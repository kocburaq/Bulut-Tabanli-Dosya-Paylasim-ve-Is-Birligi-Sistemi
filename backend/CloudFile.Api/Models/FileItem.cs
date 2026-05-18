namespace CloudFile.Api.Models;

public class FileItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public int CurrentVersion { get; set; } = 1;
    public bool IsDeleted { get; set; } = false;

    public Guid? FolderId { get; set; }
    public Folder? Folder { get; set; }

    public Guid OwnerId { get; set; }
    public User Owner { get; set; } = null!;

    public ICollection<FileVersion> Versions { get; set; } = [];
    public ICollection<ShareLink> ShareLinks { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
