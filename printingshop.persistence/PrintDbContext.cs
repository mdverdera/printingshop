using Microsoft.EntityFrameworkCore;
using printingshop.domain.Entities;

namespace printingshop.persistence
{
    public class PrintDbContext : DbContext
    {
        public PrintDbContext(DbContextOptions<PrintDbContext> options) 
            : base(options)
        {
        }

        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<Attachment> Attachments { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Order configuration
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CustomerEmail).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Subject).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Status).IsRequired();
                entity.Property(e => e.PrintAttempts).HasDefaultValue(0);
                entity.Property(e => e.GmailMessageId).HasMaxLength(255);
                entity.Property(e => e.GmailThreadId).HasMaxLength(255);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt);
                
                entity.HasIndex(e => e.GmailMessageId).IsUnique();
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.CreatedAt);

                entity.HasMany(e => e.Attachments)
                    .WithOne(a => a.Order)
                    .HasForeignKey(a => a.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Attachment configuration
            modelBuilder.Entity<Attachment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.OrderId).IsRequired();
                entity.Property(e => e.FileName).IsRequired().HasMaxLength(500);
                entity.Property(e => e.GoogleDriveFileId).IsRequired().HasMaxLength(255);
                entity.Property(e => e.PageCount).HasDefaultValue(0);
                entity.Property(e => e.SubTotalCost).HasPrecision(10, 2);
                entity.Property(e => e.CreatedAt).IsRequired();

                entity.HasIndex(e => e.OrderId);
                entity.HasIndex(e => e.GoogleDriveFileId);
            });
        }
    }
}
