using CloudFile.Api.Data;
using CloudFile.Api.DTOs;
using CloudFile.Api.Helpers;
using CloudFile.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CloudFile.Api.Controllers;

[ApiController]
[Route("api/share")]
public class ShareController : ControllerBase
{
    private readonly AppDbContext _db;

    public ShareController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateLink([FromBody] ShareLinkCreateDto dto)
    {
        if (dto.FileItemId is null && dto.FolderId is null)
            return BadRequest(new { message = "Dosya veya klasör belirtmelisiniz." });

        var userId = JwtHelper.GetUserId(User);

        if (dto.FileItemId.HasValue)
        {
            var file = await _db.Files.FindAsync(dto.FileItemId.Value);
            if (file is null || file.OwnerId != userId || file.IsDeleted)
                return NotFound(new { message = "Dosya bulunamadı." });
        }

        if (dto.FolderId.HasValue)
        {
            var folder = await _db.Folders.FindAsync(dto.FolderId.Value);
            if (folder is null || folder.OwnerId != userId)
                return NotFound(new { message = "Klasör bulunamadı." });
        }

        var link = new ShareLink
        {
            FileItemId = dto.FileItemId,
            FolderId = dto.FolderId,
            AllowDownload = dto.AllowDownload,
            AllowUpload = dto.AllowUpload,
            ExpiresAt = dto.ExpiresAt?.ToUniversalTime(),
            CreatedByUserId = userId
        };

        _db.ShareLinks.Add(link);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            link.Id,
            link.Token,
            link.AllowDownload,
            link.AllowUpload,
            link.ExpiresAt,
            link.CreatedAt,
            ShareUrl = $"/api/share/{link.Token}"
        });
    }

    [HttpGet("{token}")]
    [AllowAnonymous]
    public async Task<IActionResult> AccessLink(string token)
    {
        var link = await _db.ShareLinks
            .Include(s => s.FileItem)
            .Include(s => s.Folder)
            .FirstOrDefaultAsync(s => s.Token == token && s.IsActive);

        if (link is null)
            return NotFound(new { message = "Link bulunamadı veya devre dışı." });

        if (link.ExpiresAt.HasValue && link.ExpiresAt.Value < DateTime.UtcNow)
            return Gone("Link süresi dolmuş.");

        if (link.FileItem is not null)
        {
            return Ok(new
            {
                Type = "file",
                link.FileItem.Id,
                link.FileItem.OriginalFileName,
                link.FileItem.ContentType,
                link.FileItem.Size,
                link.AllowDownload
            });
        }

        return Ok(new
        {
            Type = "folder",
            link.Folder!.Id,
            link.Folder.Name,
            link.AllowDownload,
            link.AllowUpload
        });
    }

    [HttpGet("{token}/download")]
    [AllowAnonymous]
    public async Task<IActionResult> DownloadViaLink(string token)
    {
        var link = await _db.ShareLinks
            .Include(s => s.FileItem)
            .FirstOrDefaultAsync(s => s.Token == token && s.IsActive);

        if (link is null || !link.AllowDownload)
            return NotFound(new { message = "İndirme yetkisi yok." });

        if (link.ExpiresAt.HasValue && link.ExpiresAt.Value < DateTime.UtcNow)
            return Gone("Link süresi dolmuş.");

        var file = link.FileItem;
        if (file is null || file.IsDeleted || !System.IO.File.Exists(file.StoragePath))
            return NotFound(new { message = "Dosya bulunamadı." });

        var bytes = await System.IO.File.ReadAllBytesAsync(file.StoragePath);
        return File(bytes, file.ContentType, file.OriginalFileName);
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeactivateLink(Guid id)
    {
        var userId = JwtHelper.GetUserId(User);
        var link = await _db.ShareLinks.FindAsync(id);

        if (link is null || link.CreatedByUserId != userId)
            return NotFound(new { message = "Link bulunamadı." });

        link.IsActive = false;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Link devre dışı bırakıldı." });
    }

    private ObjectResult Gone(string message) =>
        StatusCode(StatusCodes.Status410Gone, new { message });
}
