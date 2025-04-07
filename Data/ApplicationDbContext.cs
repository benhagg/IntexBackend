using IntexBackend.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace IntexBackend.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // New tables
        public DbSet<MovieTitle> MovieTitles { get; set; }
        public DbSet<MovieRating> MovieRatings { get; set; }
        public DbSet<MovieUser> MovieUsers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Configure MovieRating primary key
            modelBuilder.Entity<MovieRating>()
                .HasKey(mr => new { mr.UserId, mr.ShowId });
        }
    }
}
