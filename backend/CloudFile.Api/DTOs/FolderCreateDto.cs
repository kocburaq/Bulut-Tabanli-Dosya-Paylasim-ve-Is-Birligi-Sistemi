using System.ComponentModel.DataAnnotations;

namespace CloudFile.Api.DTOs;

public class FolderCreateDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public Guid? ParentFolderId { get; set; }
}
