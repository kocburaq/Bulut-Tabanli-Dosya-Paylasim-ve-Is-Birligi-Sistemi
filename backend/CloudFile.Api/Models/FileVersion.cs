namespace CloudFile.Api.Models;

public class FileVersion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int VersionNumber { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public long Size { get; set; }
    public string Comment { get; set; } = string.Empty;

    public Guid FileItemId { get; set; }
    public FileItem FileItem { get; set; } = null!;

    public Guid CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
