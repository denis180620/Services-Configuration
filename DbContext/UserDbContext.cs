using Microsoft.EntityFrameworkCore;

namespace Confuguration.Dbcontext;

public class UserDbContext : DbContext
{
    public UserDbContext(DbContextOptions<UserDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<UserTamplate> UserTemplates { get; set; }
    public DbSet<SentMessage> SentMessages { get; set; }
    public DbSet<UserSession> UserSessions { get; set; }
    public DbSet<Contact> Contacts {get; set;}

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        modelBuilder.Entity<UserTamplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Tamplates)
                  .HasForeignKey(e => e.UserId);
        });

        modelBuilder.Entity<SentMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                  .WithMany(u => u.SentMessages)
                  .HasForeignKey(e => e.UserId);
        });
        modelBuilder.Entity<Contact>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Contacts)
                  .HasForeignKey(e => e.UserId);
        });
        
        modelBuilder.Entity<UserSession>(entity =>
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.RefreshToken).IsUnique();
        entity.HasIndex(e => new { e.UserId, e.IsActive });

        entity.HasOne(e => e.User)
              .WithMany(u => u.UserSessions) // Добавьте навигационное свойство в User
              .HasForeignKey(e => e.UserId)
              .OnDelete(DeleteBehavior.Cascade);
    });
    }
}