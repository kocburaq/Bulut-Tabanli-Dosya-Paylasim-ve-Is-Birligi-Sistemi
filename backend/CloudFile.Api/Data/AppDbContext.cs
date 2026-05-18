using CloudFile.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CloudFile.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Folder> Folders => Set<Folder>();
    public DbSet<FileItem> Files => Set<FileItem>();
    public DbSet<FileVersion> FileVersions => Set<FileVersion>();
    public DbSet<ShareLink> ShareLinks => Set<ShareLink>();
    public DbSet<FilePermission> FilePermissions => Set<FilePermission>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Role).HasConversion<string>();
            e.Property(u => u.StorageQuota).HasDefaultValue(1_073_741_824L);
        });

        // Folder (self-referencing)
        modelBuilder.Entity<Folder>(e =>
        {
            e.HasKey(f => f.Id);
            e.HasOne(f => f.ParentFolder)
             .WithMany(f => f.SubFolders)
             .HasForeignKey(f => f.ParentFolderId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(f => f.Owner)
             .WithMany(u => u.Folders)
             .HasForeignKey(f => f.OwnerId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // FileItem
        modelBuilder.Entity<FileItem>(e =>
        {
            e.HasKey(f => f.Id);
            e.HasOne(f => f.Owner)
             .WithMany(u => u.Files)
             .HasForeignKey(f => f.OwnerId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(f => f.Folder)
             .WithMany(folder => folder.Files)
             .HasForeignKey(f => f.FolderId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // FileVersion
        modelBuilder.Entity<FileVersion>(e =>
        {
            e.HasKey(v => v.Id);
            e.HasOne(v => v.FileItem)
             .WithMany(f => f.Versions)
             .HasForeignKey(v => v.FileItemId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(v => v.CreatedByUser)
             .WithMany()
             .HasForeignKey(v => v.CreatedByUserId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ShareLink
        modelBuilder.Entity<ShareLink>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasIndex(s => s.Token).IsUnique();

            e.HasOne(s => s.FileItem)
             .WithMany(f => f.ShareLinks)
             .HasForeignKey(s => s.FileItemId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(s => s.Folder)
             .WithMany(f => f.ShareLinks)
             .HasForeignKey(s => s.FolderId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(s => s.CreatedByUser)
             .WithMany(u => u.ShareLinks)
             .HasForeignKey(s => s.CreatedByUserId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // FilePermission
        modelBuilder.Entity<FilePermission>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Permission).HasConversion<string>();

            e.HasOne(p => p.FileItem)
             .WithMany()
             .HasForeignKey(p => p.FileItemId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(p => p.Folder)
             .WithMany()
             .HasForeignKey(p => p.FolderId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(p => p.User)
             .WithMany()
             .HasForeignKey(p => p.UserId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(p => p.GrantedByUser)
             .WithMany()
             .HasForeignKey(p => p.GrantedByUserId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // PasswordResetToken
        modelBuilder.Entity<PasswordResetToken>(e =>
        {
            e.HasKey(t => t.Id);
            e.HasIndex(t => t.Token).IsUnique();
            e.HasOne(t => t.User)
             .WithMany()
             .HasForeignKey(t => t.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
