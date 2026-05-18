using CloudFile.Api.Data;
using CloudFile.Api.Helpers;
using CloudFile.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CloudFile.Api.Controllers;

[ApiController]
[Route("api/folders")]
[Authorize]
public class FoldersController : ControllerBase
{
    private readonly AppDbContext _db;

    public FoldersController(AppDbContext db)
    {
        _db = db;
    }

    // klasörleri listele - parentId null ise ana dizin
    [HttpGet]
    public async Task<IActionResult> GetFolders([FromQuery] Guid? parentId)
    {
        var userId = JwtHelper.GetUserId(User);

        var folders = await _db.Folders
            .Where(f => f.OwnerId == userId && f.ParentFolderId == parentId)
            .Select(f => new
            {
                f.Id,
                f.Name,
                f.CreatedAt,
                // alt klasördeki dosya sayısını da gönder
                fileCount = _db.Files.Count(fi => fi.FolderId == f.Id && !fi.IsDeleted)
            })
            .OrderBy(f => f.Name)
            .ToListAsync();

        return Ok(folders);
    }

    // yeni klasör oluştur
    [HttpPost]
    public async Task<IActionResult> CreateFolder([FromBody] CreateFolderDto dto)
    {
        var userId = JwtHelper.GetUserId(User);

        // parentId verilmişse o klasörün sahibi bu kullanıcı mı kontrol et
        if (dto.ParentFolderId.HasValue)
        {
            var parent = await _db.Folders.FindAsync(dto.ParentFolderId.Value);
            if (parent is null || parent.OwnerId != userId)
                return BadRequest(new { message = "Üst klasör bulunamadı." });
        }

        var folder = new Folder
        {
            Name = dto.Name,
            OwnerId = userId,
            ParentFolderId = dto.ParentFolderId
        };

        _db.Folders.Add(folder);
        await _db.SaveChangesAsync();

        return Ok(new { folder.Id, folder.Name, folder.CreatedAt });
    }

    // klasörü sil (içindeki dosyalarla birlikte)
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteFolder(Guid id)
    {
        var userId = JwtHelper.GetUserId(User);
        var folder = await _db.Folders.FindAsync(id);

        if (folder is null || folder.OwnerId != userId)
            return NotFound(new { message = "Klasör bulunamadı." });

        _db.Folders.Remove(folder);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Klasör silindi." });
    }

    // klasör adını güncelle
    [HttpPut("{id}")]
    public async Task<IActionResult> RenameFolder(Guid id, [FromBody] CreateFolderDto dto)
    {
        var userId = JwtHelper.GetUserId(User);
        var folder = await _db.Folders.FindAsync(id);

        if (folder is null || folder.OwnerId != userId)
            return NotFound(new { message = "Klasör bulunamadı." });

        folder.Name = dto.Name;
        await _db.SaveChangesAsync();

        return Ok(new { folder.Id, folder.Name });
    }
}

public class CreateFolderDto
{
    public string Name { get; set; } = "";
    public Guid? ParentFolderId { get; set; }
}
