
using System;
using BookStoreManagement.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BookStoreManagement.Data
{
    public partial class BookStoreAuthDbContext : DbContext
    {
        // Constructor không tham số
        public BookStoreAuthDbContext()
        {
        }

        // Constructor nhận DbContextOptions
        public BookStoreAuthDbContext(
            DbContextOptions<BookStoreAuthDbContext> options)
            : base(options)
        {
        }

        // Bảng AppUsers
        public virtual DbSet<AppUser> AppUsers { get; set; }

        // Đọc connection string từ appsettings.json
        protected override void OnConfiguring(
            DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile(
                        "appsettings.json",
                        optional: false,
                        reloadOnChange: true)
                    .Build();

                string connectionString =
                    config.GetConnectionString("DefaultConnection")
                    ?? throw new InvalidOperationException(
                        "DefaultConnection was not found.");

                optionsBuilder.UseSqlServer(connectionString);
            }
        }

        // Mapping bảng AppUsers
        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AppUser>(entity =>
            {
                entity.HasKey(e => e.Id)
                    .HasName("PK__AppUsers__3214EC07CBA809EE");

                entity.HasIndex(
                    e => e.Username,
                    "UQ__AppUsers__536C85E42606F54A")
                    .IsUnique();

                entity.Property(e => e.PasswordHash)
                    .HasMaxLength(500);

                entity.Property(e => e.Role)
                    .HasMaxLength(20);

                entity.Property(e => e.Username)
                    .HasMaxLength(50);
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(
            ModelBuilder modelBuilder);
    }
}
