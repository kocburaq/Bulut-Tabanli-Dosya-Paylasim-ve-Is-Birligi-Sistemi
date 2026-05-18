using System.ComponentModel.DataAnnotations;
namespace CloudFile.Api.DTOs;
public class ForgotPasswordDto
{
    [Required][EmailAddress]
    public string Email { get; set; } = string.Empty;
}
