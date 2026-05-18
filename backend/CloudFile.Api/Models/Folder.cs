namespace CloudFile.Api.Models;

public class Folder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;

    public Guid? ParentFolderId { get; set; }
    public Folder? ParentFolder { get; set; }
    public ICollection<Folder> SubFolders { get; set; } = [];

    public Guid OwnerId { get; set; }
    public User Owner { get; set; } = null!;

    public ICollection<FileItem> Files { get; set; } = [];
    public ICollection<ShareLink> ShareLinks { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
