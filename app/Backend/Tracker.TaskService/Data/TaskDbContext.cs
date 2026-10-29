using Microsoft.EntityFrameworkCore;
using Tracker.TaskService.Models;

namespace Tracker.TaskService.Data;

public class TaskDbContext : DbContext
{
    public TaskDbContext(DbContextOptions<TaskDbContext> options)
        : base(options)
    {

    }

    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.ToTable("tasks");
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Id).ValueGeneratedOnAdd();
            entity.Property(t => t.UserId).HasMaxLength(64).IsRequired();
            entity.Property(t => t.Title).HasMaxLength(500).IsRequired();
            entity.Property(t => t.ProjectId).HasColumnName("project_id").HasMaxLength(36);
            // Truy vän chinh là "lấy tất cả task của 1 user" -> index theo UserId.
            entity.HasIndex(t => t.UserId).HasDatabaseName("ix_tasks_user_id");
            // Index theo ProjectId cho việc lấy task theo project.
            entity.HasIndex(t => t.ProjectId).HasDatabaseName("ix_tasks_project_id");
        });
    }
}