namespace CloudFile.Api.Models;

public enum PermissionType
{
    Read,
    Write,
    Delete,
    FullControl
}

public class FilePermission
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? FileItemId { get; set; }
    public FileItem? FileItem { get; set; }

    public Guid? FolderId { get; set; }
    public Folder? Folder { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid GrantedByUserId { get; set; }
    public User GrantedByUser { get; set; } = null!;

    public PermissionType Permission { get; set; } = PermissionType.Read;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
