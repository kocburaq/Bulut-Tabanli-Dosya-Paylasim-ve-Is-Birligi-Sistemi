namespace CloudFile.Api.DTOs;

public class ShareLinkCreateDto
{
    public Guid? FileItemId { get; set; }
    public Guid? FolderId { get; set; }
    public bool AllowDownload { get; set; } = true;
    public bool AllowUpload { get; set; } = false;
    public DateTime? ExpiresAt { get; set; }
}
