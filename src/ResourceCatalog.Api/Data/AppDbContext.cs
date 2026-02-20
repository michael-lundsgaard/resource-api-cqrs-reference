using Microsoft.EntityFrameworkCore;
using ResourceCatalog.Api.Entities;

namespace ResourceCatalog.Api.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<Resource> Resources => Set<Resource>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Resource>(e =>
            {
                e.HasKey(r => r.Id);
                e.Property(r => r.Name).HasMaxLength(200).IsRequired();
            });
        }
    }
}