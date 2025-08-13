using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PhotoGallery.Web.Models;

namespace PhotoGallery.Web.Data{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
        {
        }

        public DbSet<Gallery> Galleries => Set<Gallery>();
        public DbSet<Photo> Photos => Set<Photo>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<Gallery>()
               .HasOne(g => g.Owner)
               .WithMany()
               .HasForeignKey(g => g.OwnerId)
               .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Photo>()
                .HasOne(p => p.Gallery)
                .WithMany()
                .HasForeignKey(p => p.GalleryId)
                .OnDelete(DeleteBehavior.Cascade);
            
             builder.Entity<Gallery>()
                .HasIndex(g => new { g.OwnerId, g.CreatedUtc });

            builder.Entity<Photo>()
                .HasIndex(p => new { p.GalleryId, p.CreatedUtc });
        }

    }

    
}



