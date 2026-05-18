using CloudFile.Api.Data;
using CloudFile.Api.DTOs;
using CloudFile.Api.Helpers;
using CloudFile.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CloudFile.Api.Services;

// Kayıt, giriş ve şifre sıfırlama işlemlerini yönetir
public class AuthService
{
    private readonly AppDbContext _db;
    private readonly JwtHelper _jwt;

    public AuthService(AppDbContext db, JwtHelper jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    public async Task<(bool Success, string Message, string? Token)> RegisterAsync(RegisterDto dto)
    {
        // aynı mail zaten kayıtlı mı kontrol et
        var exists = await _db.Users.AnyAsync(u => u.Email == dto.Email.ToLower());
        if (exists)
            return (false, "Bu e-posta adresi zaten kayıtlı.", null);

        var user = new User
        {
            Email = dto.Email.ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Role = UserRole.User
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var token = _jwt.GenerateToken(user);
        return (true, "Kayıt başarılı.", token);
    }

    public async Task<(bool Success, string Message, string? Token)> LoginAsync(LoginDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email.ToLower());

        // kullanıcı yok veya hesap pasif
        if (user is null || !user.IsActive)
            return (false, "E-posta veya şifre hatalı.", null);

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return (false, "E-posta veya şifre hatalı.", null);

        var token = _jwt.GenerateToken(user);
        return (true, "Giriş başarılı.", token);
    }

    public async Task<User?> GetUserByIdAsync(Guid id)
    {
        return await _db.Users.FindAsync(id);
    }

    // Şifre sıfırlama token'ı oluşturur
    // TODO: ileride buradan e-posta da gönderilebilir (smtp vs.)
    public async Task<(bool Success, string Message, string? ResetToken)> ForgotPasswordAsync(ForgotPasswordDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email.ToLower());

        // güvenlik açısından kullanıcı bulunamasa da aynı mesajı veriyoruz
        // yoksa "bu mail kayıtlı değil" diye enumerate edilebilir
        if (user is null || !user.IsActive)
            return (true, "Eğer bu e-posta kayıtlıysa sıfırlama token'ı oluşturuldu.", null);

        // önceki kullanılmamış tokenları geçersiz yap
        var eskiTokenlar = await _db.PasswordResetTokens
            .Where(t => t.UserId == user.Id && !t.IsUsed)
            .ToListAsync();
        eskiTokenlar.ForEach(t => t.IsUsed = true);

        var resetToken = new PasswordResetToken { UserId = user.Id };
        _db.PasswordResetTokens.Add(resetToken);
        await _db.SaveChangesAsync();

        return (true, "Token oluşturuldu.", resetToken.Token);
    }

    public async Task<(bool Success, string Message)> ResetPasswordAsync(ResetPasswordDto dto)
    {
        var tokenKaydi = await _db.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == dto.Token);

        if (tokenKaydi is null || tokenKaydi.IsUsed)
            return (false, "Geçersiz veya kullanılmış token.");

        if (tokenKaydi.ExpiresAt < DateTime.UtcNow)
            return (false, "Token süresi dolmuş.");

        tokenKaydi.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        tokenKaydi.IsUsed = true;
        await _db.SaveChangesAsync();

        return (true, "Şifreniz başarıyla sıfırlandı.");
    }
}
