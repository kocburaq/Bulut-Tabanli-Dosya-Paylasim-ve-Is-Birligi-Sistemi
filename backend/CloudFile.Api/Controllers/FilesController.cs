using CloudFile.Api.Data;
using CloudFile.Api.DTOs;
using CloudFile.Api.Helpers;
using CloudFile.Api.Models;
using CloudFile.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CloudFile.Api.Controllers;

[ApiController]
[Route("api/files")]
[Authorize]
public class FilesController : ControllerBase
{
    private readonly FileStorageService _storage;
    private readonly AppDbContext _db;

    public FilesController(FileStorageService storage, AppDbContext db)
    {
        _storage = storage;
        _db = db;
    }

    // Dosyaları listele (filtreleme + arama)
    [HttpGet]
    public async Task<IActionResult> GetFiles(
        [FromQuery] Guid? folderId,
        [FromQuery] string? search,
        [FromQuery] string? contentType,
        [FromQuery] string? sortBy = "createdAt",
        [FromQuery] string? sortOrder = "desc")
    {
        var userId = JwtHelper.GetUserId(User);

        var query = _db.Files
            .Where(f => f.OwnerId == userId && !f.IsDeleted);

        // Klasör filtresi
        if (folderId.HasValue)
            query = query.Where(f => f.FolderId == folderId);

        // Arama
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(f => f.OriginalFileName.Contains(search));

        // İçerik tipi filtresi (image, video, application vb.)
        if (!string.IsNullOrWhiteSpace(contentType))
            query = query.Where(f => f.ContentType.StartsWith(contentType));

        // Sıralama
        query = (sortBy?.ToLower(), sortOrder?.ToLower()) switch
        {
            ("name", "asc")  => query.OrderBy(f => f.OriginalFileName),
            ("name", _)      => query.OrderByDescending(f => f.OriginalFileName),
            ("size", "asc")  => query.OrderBy(f => f.Size),
            ("size", _)      => query.OrderByDescending(f => f.Size),
            ("createdat", "asc") => query.OrderBy(f => f.CreatedAt),
            _                => query.OrderByDescending(f => f.CreatedAt)
        };

        var files = await query
            .Select(f => new
            {
                f.Id,
                f.OriginalFileName,
                f.ContentType,
                f.Size,
                f.CurrentVersion,
                f.FolderId,
                f.CreatedAt,
                f.UpdatedAt
            })
            .ToListAsync();

        return Ok(files);
    }

    // Dosya yükle
    [HttpPost("upload")]
    [RequestSizeLimit(104_857_600)] // 100 MB
    public async Task<IActionResult> Upload([FromForm] FileUploadDto dto)
    {
        var userId = JwtHelper.GetUserId(User);
        var (success, message, file) = await _storage.UploadAsync(
            dto.File, dto.FolderId, userId, dto.VersionComment);

        if (!success) return BadRequest(new { message });

        return Ok(new
        {
            message,
            file?.Id,
            file?.OriginalFileName,
            file?.Size,
            file?.CurrentVersion
        });
    }

    // Dosya indir
    [HttpGet("{id}/download")]
    public async Task<IActionResult> Download(Guid id)
    {
        var userId = JwtHelper.GetUserId(User);
        var (success, message, filePath, contentType, fileName) =
            await _storage.GetFileAsync(id, userId);

        if (!success) return NotFound(new { message });

        var bytes = await System.IO.File.ReadAllBytesAsync(filePath!);
        return File(bytes, contentType!, fileName);
    }

    // Versiyonları listele
    [HttpGet("{id}/versions")]
    public async Task<IActionResult> GetVersions(Guid id)
    {
        var userId = JwtHelper.GetUserId(User);
        var versions = await _storage.GetVersionsAsync(id, userId);

        return Ok(versions.Select(v => new
        {
            v.Id,
            v.VersionNumber,
            v.Size,
            v.Comment,
            v.CreatedAt
        }));
    }

    // Belirli versiyona geri dön (rollback)
    [HttpPost("{id}/rollback/{versionId}")]
    public async Task<IActionResult> Rollback(Guid id, Guid versionId)
    {
        var userId = JwtHelper.GetUserId(User);
        var file = await _db.Files.Include(f => f.Versions)
            .FirstOrDefaultAsync(f => f.Id == id && f.OwnerId == userId);

        if (file is null || file.IsDeleted)
            return NotFound(new { message = "Dosya bulunamadı." });

        var version = file.Versions.FirstOrDefault(v => v.Id == versionId);
        if (version is null)
            return NotFound(new { message = "Versiyon bulunamadı." });

        file.StoragePath = version.StoragePath;
        file.Size = version.Size;
        file.CurrentVersion = version.VersionNumber;
        file.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = $"Dosya v{version.VersionNumber} sürümüne geri alındı.",
            file.Id,
            file.CurrentVersion
        });
    }

    // Yetki ver (dosya bazlı)
    [HttpPost("{id}/permissions")]
    public async Task<IActionResult> GrantPermission(Guid id, [FromBody] GrantPermissionDto dto)
    {
        var userId = JwtHelper.GetUserId(User);
        var file = await _db.Files.FindAsync(id);

        if (file is null || file.OwnerId != userId || file.IsDeleted)
            return NotFound(new { message = "Dosya bulunamadı veya yetkiniz yok." });

        var targetUser = await _db.Users.FindAsync(dto.TargetUserId);
        if (targetUser is null)
            return NotFound(new { message = "Kullanıcı bulunamadı." });

        if (!Enum.TryParse<PermissionType>(dto.Permission, true, out var perm))
            return BadRequest(new { message = "Geçersiz yetki. (Read, Write, Delete, FullControl)" });

        var existing = await _db.FilePermissions
            .FirstOrDefaultAsync(p => p.FileItemId == id && p.UserId == dto.TargetUserId);

        if (existing is not null)
        {
            existing.Permission = perm;
        }
        else
        {
            _db.FilePermissions.Add(new FilePermission
            {
                FileItemId = id,
                UserId = dto.TargetUserId,
                GrantedByUserId = userId,
                Permission = perm
            });
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = $"{targetUser.Email} kullanıcısına {perm} yetkisi verildi." });
    }

    // Dosyayı sil (soft delete)
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = JwtHelper.GetUserId(User);
        var (success, message) = await _storage.DeleteAsync(id, userId);

        if (!success) return BadRequest(new { message });
        return Ok(new { message });
    }

    // Dosya detayı
    [HttpGet("{id}")]
    public async Task<IActionResult> GetFile(Guid id)
    {
        var userId = JwtHelper.GetUserId(User);
        var file = await _db.Files
            .Include(f => f.Folder)
            .FirstOrDefaultAsync(f => f.Id == id && f.OwnerId == userId && !f.IsDeleted);

        if (file is null) return NotFound(new { message = "Dosya bulunamadı." });

        return Ok(new
        {
            file.Id,
            file.OriginalFileName,
            file.ContentType,
            file.Size,
            file.CurrentVersion,
            file.FolderId,
            FolderName = file.Folder?.Name,
            file.CreatedAt,
            file.UpdatedAt
        });
    }
}

public class GrantPermissionDto
{
    public Guid TargetUserId { get; set; }
    public string Permission { get; set; } = "Read";
}
