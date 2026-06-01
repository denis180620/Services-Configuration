using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Confuguration.Dbcontext;

// ✅ Наследуемся от IdentityDbContext<User, Role, Guid> вместо DbContext
public class UserDbContext : IdentityDbContext<User, Role, Guid>
{
    public UserDbContext(DbContextOptions<UserDbContext> options) : base(options) { }

    // Identity уже имеет DbSet<User>, поэтому отдельный DbSet<User> не нужен
    public DbSet<UserTamplate> UserTemplates { get; set; }
    public DbSet<SentMessage> SentMessages { get; set; }
    public DbSet<UserSession> UserSessions { get; set; }
    public DbSet<Contact> Contacts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ✅ ВАЖНО: сначала вызываем base.OnModelCreating для настройки Identity
        base.OnModelCreating(modelBuilder);

        // Переименовываем таблицы Identity (опционально)
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserName).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();

            // Настройка связей
            entity.HasMany(u => u.Tamplates)
                  .WithOne(t => t.User)
                  .HasForeignKey(t => t.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(u => u.SentMessages)
                  .WithOne(m => m.User)
                  .HasForeignKey(m => m.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(u => u.Contacts)
                  .WithOne(c => c.User)
                  .HasForeignKey(c => c.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(u => u.UserSessions)
                  .WithOne(s => s.User)
                  .HasForeignKey(s => s.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");
        });

        modelBuilder.Entity<UserTamplate>(entity =>
        {
            entity.ToTable("UserTemplates");
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<SentMessage>(entity =>
        {
            entity.ToTable("SentMessages");
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<Contact>(entity =>
        {
            entity.ToTable("Contacts");
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.ToTable("UserSessions");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.RefreshToken).IsUnique();
            entity.HasIndex(e => new { e.UserId, e.IsActive });
        });
    }
}