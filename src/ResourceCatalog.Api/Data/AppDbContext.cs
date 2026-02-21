using Microsoft.EntityFrameworkCore;
using ResourceCatalog.Api.Entities;

namespace ResourceCatalog.Api.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<Resource> Resources => Set<Resource>();
        public DbSet<Tag> Tags => Set<Tag>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Resource>(e =>
            {
                e.HasKey(r => r.Id);
                e.Property(r => r.Name).HasMaxLength(200).IsRequired();

                // Many-to-many relationship
                e.HasMany(r => r.Tags)
                 .WithMany(t => t.Resources);
            });

            modelBuilder.Entity<Tag>(e =>
            {
                e.HasKey(t => t.Id);
                e.Property(t => t.Label).HasMaxLength(50).IsRequired();
                e.HasIndex(t => t.Label).IsUnique();
            });
        }
    }
}