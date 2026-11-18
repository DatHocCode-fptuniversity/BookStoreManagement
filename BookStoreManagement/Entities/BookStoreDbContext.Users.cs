
using Microsoft.EntityFrameworkCore;

namespace BookStoreManagement.Entities
{
    public partial class BookStoreDbContext
    {
        public virtual DbSet<AppUser> AppUsers { get; set; }
            = null!;

        partial void OnModelCreatingPartial(
            ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AppUser>(entity =>
            {
                entity.ToTable("AppUsers");

                entity.HasKey(e => e.Id);

                entity.HasIndex(e => e.Username)
                    .IsUnique();

                entity.Property(e => e.Username)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(e => e.PasswordHash)
                    .HasMaxLength(500)
                    .IsRequired();

                entity.Property(e => e.Role)
                    .HasMaxLength(20)
                    .IsRequired();
            });
        }
    }
}
