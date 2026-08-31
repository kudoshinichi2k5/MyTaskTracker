using Microsoft.EntityFrameworkCore;
using Tracker.CommentService.Models;

namespace Tracker.CommentService.Data;

public class CommentDbContext : DbContext
{
    public CommentDbContext(
        DbContextOptions<CommentDbContext> options)
        : base(options)
    {
    }

    public DbSet<CommentItem> Comments =>
        Set<CommentItem>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CommentItem>(
            entity =>
            {
                entity.ToTable("comments");

                entity.HasKey(c => c.Id);

                entity.Property(c => c.Id)
                    .HasColumnType("char(36)")
                    .IsRequired();

                entity.Property(c => c.TaskId)
                    .IsRequired();

                entity.Property(c => c.AuthorUserId)
                    .HasMaxLength(64)
                    .IsRequired();

                entity.Property(c => c.Body)
                    .HasMaxLength(4000)
                    .IsRequired();

                entity.Property(c => c.CreatedAt)
                    .IsRequired();

                entity.Property(c => c.EditedAt);

                entity.HasIndex(c => c.TaskId)
                    .HasDatabaseName(
                        "ix_comments_task_id");

                entity.HasIndex(c => c.AuthorUserId)
                    .HasDatabaseName(
                        "ix_comments_author_user_id");
            });
    }
}