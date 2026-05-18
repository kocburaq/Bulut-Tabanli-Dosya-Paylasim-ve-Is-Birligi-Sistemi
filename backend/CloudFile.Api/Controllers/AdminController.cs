using CloudFile.Api.Data;
using CloudFile.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CloudFile.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminController(AppDbContext db)
    {
        _db = db;
    }

    // Tüm kullanıcıları listele
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] string? search)
    {
        var query = _db.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(u =>
                u.Email.Contains(search) ||
                u.FirstName.Contains(search) ||
                u.LastName.Contains(search));

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.FirstName,
                u.LastName,
                u.Role,
                u.IsActive,
                u.StorageQuota,
                u.StorageUsed,
                u.CreatedAt,
                FileCount = u.Files.Count(f => !f.IsDeleted),
                FolderCount = u.Folders.Count
            })
            .ToListAsync();

        return Ok(users);
    }

    // Kullanıcı rolünü güncelle
    [HttpPut("users/{id}/role")]
    public async Task<IActionResult> UpdateRole(Guid id, [FromBody] UpdateRoleDto dto)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is null) return NotFound(new { message = "Kullanıcı bulunamadı." });

        if (!Enum.TryParse<UserRole>(dto.Role, true, out var role))
            return BadRequest(new { message = "Geçersiz rol. (Admin, Manager, User)" });

        user.Role = role;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { message = $"Rol güncellendi: {role}", user.Id, user.Email, Role = role.ToString() });
    }

    // Kullanıcı kotasını güncelle
    [HttpPut("users/{id}/quota")]
    public async Task<IActionResult> UpdateQuota(Guid id, [FromBody] UpdateQuotaDto dto)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is null) return NotFound(new { message = "Kullanıcı bulunamadı." });

        if (dto.QuotaBytes < 1_048_576) // minimum 1 MB
            return BadRequest(new { message = "Kota en az 1 MB olmalıdır." });

        user.StorageQuota = dto.QuotaBytes;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Kota güncellendi.",
            user.Id,
            user.Email,
            QuotaBytes = user.StorageQuota,
            QuotaMB = user.StorageQuota / 1_048_576
        });
    }

    // Kullanıcıyı aktif/pasif yap
    [HttpPut("users/{id}/toggle-active")]
    public async Task<IActionResult> ToggleActive(Guid id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is null) return NotFound(new { message = "Kullanıcı bulunamadı." });

        user.IsActive = !user.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = user.IsActive ? "Kullanıcı aktif edildi." : "Kullanıcı devre dışı bırakıldı.",
            user.Id,
            user.IsActive
        });
    }

    // Sistem istatistikleri
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var totalUsers = await _db.Users.CountAsync();
        var activeUsers = await _db.Users.CountAsync(u => u.IsActive);
        var totalFiles = await _db.Files.CountAsync(f => !f.IsDeleted);
        var totalFolders = await _db.Folders.CountAsync();
        var totalShareLinks = await _db.ShareLinks.CountAsync(s => s.IsActive);
        var totalStorageUsed = await _db.Users.SumAsync(u => u.StorageUsed);

        var roleStats = await _db.Users
            .GroupBy(u => u.Role)
            .Select(g => new { Role = g.Key.ToString(), Count = g.Count() })
            .ToListAsync();

        var recentUsers = await _db.Users
            .OrderByDescending(u => u.CreatedAt)
            .Take(5)
            .Select(u => new { u.Id, u.Email, u.FirstName, u.LastName, u.Role, u.CreatedAt })
            .ToListAsync();

        return Ok(new
        {
            TotalUsers = totalUsers,
            ActiveUsers = activeUsers,
            TotalFiles = totalFiles,
            TotalFolders = totalFolders,
            ActiveShareLinks = totalShareLinks,
            TotalStorageUsedBytes = totalStorageUsed,
            TotalStorageUsedMB = totalStorageUsed / 1_048_576,
            RoleDistribution = roleStats,
            RecentUsers = recentUsers
        });
    }
}

public class UpdateRoleDto
{
    public string Role { get; set; } = string.Empty;
}

public class UpdateQuotaDto
{
    public long QuotaBytes { get; set; }
}
