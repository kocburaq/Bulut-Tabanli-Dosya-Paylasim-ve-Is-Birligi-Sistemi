using System.ComponentModel.DataAnnotations;

namespace CloudFile.Api.DTOs;

public class FileUploadDto
{
    [Required]
    public IFormFile File { get; set; } = null!;

    public Guid? FolderId { get; set; }

    public string? VersionComment { get; set; }
}
