using Microsoft.EntityFrameworkCore;
using Confuguration.Dbcontext;

namespace Confuguration.Dbcontext;

    public class TemplateDbContext : DbContext
    {
        public TemplateDbContext(DbContextOptions<TemplateDbContext> options) : base(options) { }

        public DbSet<MessageTemplate> MessageTemplates { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MessageTemplate>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Category);
            });
        }
        
    }
