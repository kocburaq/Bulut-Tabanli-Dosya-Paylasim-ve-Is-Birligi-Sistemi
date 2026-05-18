namespace CloudFile.Api.Models;

public enum UserRole
{
    User,
    Manager,
    Admin
}

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.User;
    public long StorageQuota { get; set; } = 1_073_741_824; // 1 GB default
    public long StorageUsed { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Folder> Folders { get; set; } = [];
    public ICollection<FileItem> Files { get; set; } = [];
    public ICollection<ShareLink> ShareLinks { get; set; } = [];
}
