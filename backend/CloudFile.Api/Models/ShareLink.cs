namespace CloudFile.Api.Models;

public class ShareLink
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Token { get; set; } = Guid.NewGuid().ToString("N");
    public bool AllowDownload { get; set; } = true;
    public bool AllowUpload { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public DateTime? ExpiresAt { get; set; }

    public Guid? FileItemId { get; set; }
    public FileItem? FileItem { get; set; }

    public Guid? FolderId { get; set; }
    public Folder? Folder { get; set; }

    public Guid CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
