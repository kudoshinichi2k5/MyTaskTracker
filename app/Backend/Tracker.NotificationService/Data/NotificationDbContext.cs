using Microsoft.EntityFrameworkCore;
using Tracker.NotificationService.Models;

namespace Tracker.NotificationService.Data;

public class NotificationDbContext : DbContext
{
    public NotificationDbContext(
        DbContextOptions<NotificationDbContext> options)
        : base(options)
    {
    }

    public DbSet<NotificationItem> Notifications =>
        Set<NotificationItem>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NotificationItem>(
            entity =>
            {
                entity.ToTable("notifications");

                entity.HasKey(n => n.Id);

                entity.Property(n => n.Id)
                    .ValueGeneratedOnAdd();

                entity.Property(n => n.UserId)
                    .HasMaxLength(64)
                    .IsRequired();

                entity.Property(n => n.Message)
                    .HasMaxLength(1000)
                    .IsRequired();

                entity.Property(n => n.IsRead)
                    .IsRequired();

                entity.HasIndex(n => n.UserId)
                    .HasDatabaseName(
                        "ix_notifications_user_id");
            });
    }
}