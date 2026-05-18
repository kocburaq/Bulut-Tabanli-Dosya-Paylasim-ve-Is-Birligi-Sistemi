using CloudFile.Api.Data;
using CloudFile.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CloudFile.Api.Services;

// Dosya yükleme, indirme ve silme işlemleri
// Fiziksel dosyalar: uploads/{userId}/{guid}.{ext} şeklinde saklanıyor
public class FileStorageService
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _config;

    public FileStorageService(AppDbContext db, IWebHostEnvironment env, IConfiguration config)
    {
        _db = db;
        _env = env;
        _config = config;
    }

    // appsettings'ten yükleme klasörünü al, yoksa proje dizinine göre bul
    private string GetUploadRoot()
    {
        var path = _config["Storage:UploadPath"] ?? "uploads";
        if (!Path.IsPathRooted(path))
            path = Path.Combine(_env.ContentRootPath, "..", "..", "..", "..", "uploads");
        Directory.CreateDirectory(path);
        return path;
    }

    public async Task<(bool Success, string Message, FileItem? File)> UploadAsync(
        IFormFile file, Guid? folderId, Guid ownerId, string? versionComment = null)
    {
        var user = await _db.Users.FindAsync(ownerId);
        if (user is null) return (false, "Kullanıcı bulunamadı.", null);

        // kota kontrolü - kullanıcının yeterli alanı var mı
        if (user.StorageUsed + file.Length > user.StorageQuota)
            return (false, "Depolama kotanız doldu.", null);

        var uploadRoot = GetUploadRoot();
        var userFolder = Path.Combine(uploadRoot, ownerId.ToString());
        Directory.CreateDirectory(userFolder);

        // dosya adını uuid ile değiştiriyoruz ki çakışma olmasın
        var safeFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var storagePath = Path.Combine(userFolder, safeFileName);

        await using (var stream = File.Create(storagePath))
            await file.CopyToAsync(stream);

        // Aynı klasörde aynı isimde dosya var mı? (versiyon kontrolü)
        var mevcutDosya = await _db.Files.FirstOrDefaultAsync(f =>
            f.OwnerId == ownerId &&
            f.FolderId == folderId &&
            f.OriginalFileName == file.FileName &&
            !f.IsDeleted);

        if (mevcutDosya is not null)
        {
            // varsa yeni versiyon ekle
            var yeniVersiyon = new FileVersion
            {
                FileItemId = mevcutDosya.Id,
                VersionNumber = mevcutDosya.CurrentVersion + 1,
                StoragePath = storagePath,
                Size = file.Length,
                Comment = versionComment ?? string.Empty,
                CreatedByUserId = ownerId
            };

            mevcutDosya.CurrentVersion++;
            mevcutDosya.StoragePath = storagePath;
            mevcutDosya.Size = file.Length;
            mevcutDosya.UpdatedAt = DateTime.UtcNow;

            _db.FileVersions.Add(yeniVersiyon);
            user.StorageUsed += file.Length;
            await _db.SaveChangesAsync();
            return (true, "Dosya güncellendi (yeni versiyon).", mevcutDosya);
        }

        // yeni dosya
        var fileItem = new FileItem
        {
            FileName = safeFileName,
            OriginalFileName = file.FileName,
            ContentType = file.ContentType,
            Size = file.Length,
            StoragePath = storagePath,
            FolderId = folderId,
            OwnerId = ownerId
        };

        // ilk yükleme için otomatik versiyon 1 oluştur
        var ilkVersiyon = new FileVersion
        {
            FileItemId = fileItem.Id,
            VersionNumber = 1,
            StoragePath = storagePath,
            Size = file.Length,
            Comment = versionComment ?? "İlk yükleme",
            CreatedByUserId = ownerId
        };

        user.StorageUsed += file.Length;

        _db.Files.Add(fileItem);
        _db.FileVersions.Add(ilkVersiyon);
        await _db.SaveChangesAsync();

        return (true, "Dosya yüklendi.", fileItem);
    }

    public async Task<(bool Success, string Message, string? FilePath, string? ContentType, string? FileName)>
        GetFileAsync(Guid fileId, Guid requestingUserId)
    {
        var file = await _db.Files.FindAsync(fileId);
        if (file is null || file.IsDeleted)
            return (false, "Dosya bulunamadı.", null, null, null);

        // başka kullanıcının dosyasına erişim engelleniyor
        if (file.OwnerId != requestingUserId)
            return (false, "Bu dosyaya erişim yetkiniz yok.", null, null, null);

        if (!File.Exists(file.StoragePath))
            return (false, "Dosya fiziksel olarak bulunamadı.", null, null, null);

        return (true, "OK", file.StoragePath, file.ContentType, file.OriginalFileName);
    }

    public async Task<(bool Success, string Message)> DeleteAsync(Guid fileId, Guid requestingUserId)
    {
        var file = await _db.Files.FindAsync(fileId);
        if (file is null || file.IsDeleted)
            return (false, "Dosya bulunamadı.");

        if (file.OwnerId != requestingUserId)
            return (false, "Bu dosyayı silme yetkiniz yok.");

        // soft delete - dosyayı diskten silmiyoruz, sadece IsDeleted=true yapıyoruz
        file.IsDeleted = true;
        file.UpdatedAt = DateTime.UtcNow;

        var user = await _db.Users.FindAsync(requestingUserId);
        if (user is not null) user.StorageUsed -= file.Size;

        await _db.SaveChangesAsync();
        return (true, "Dosya silindi.");
    }

    public async Task<List<FileVersion>> GetVersionsAsync(Guid fileId, Guid requestingUserId)
    {
        var file = await _db.Files.FindAsync(fileId);
        if (file is null || file.OwnerId != requestingUserId) return [];

        return await _db.FileVersions
            .Where(v => v.FileItemId == fileId)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync();
    }
}
