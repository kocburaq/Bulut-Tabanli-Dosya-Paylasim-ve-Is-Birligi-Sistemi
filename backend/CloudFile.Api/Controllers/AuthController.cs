using CloudFile.Api.DTOs;
using CloudFile.Api.Helpers;
using CloudFile.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudFile.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _auth;

    public AuthController(AuthService auth)
    {
        _auth = auth;
    }

    // POST /api/auth/register
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var (success, message, token) = await _auth.RegisterAsync(dto);
        if (!success) return BadRequest(new { message });
        return Ok(new { message, token });
    }

    // POST /api/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var (success, message, token) = await _auth.LoginAsync(dto);
        if (!success) return Unauthorized(new { message });
        return Ok(new { message, token });
    }

    // oturum açık kullanıcının bilgilerini döndürür
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var userId = JwtHelper.GetUserId(User);
        var user = await _auth.GetUserByIdAsync(userId);
        if (user is null) return NotFound();

        return Ok(new
        {
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.Role,
            user.StorageQuota,
            user.StorageUsed,
            user.CreatedAt
        });
    }

    // şifre sıfırlama - token üretir (gerçek sistemde e-mail ile gönderilir)
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        var (success, message, resetToken) = await _auth.ForgotPasswordAsync(dto);
        return Ok(new { message, token = resetToken });
    }

    // token + yeni şifre ile şifre sıfırla
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        var (success, message) = await _auth.ResetPasswordAsync(dto);
        if (!success) return BadRequest(new { message });
        return Ok(new { message });
    }
}
