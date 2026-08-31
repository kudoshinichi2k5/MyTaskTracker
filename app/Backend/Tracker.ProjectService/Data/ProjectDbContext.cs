using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;
using Tracker.ProjectService.Models;

namespace Tracker.ProjectService.Data;

public class ProjectDbContext : DbContext
{
    public ProjectDbContext(
        DbContextOptions<ProjectDbContext> options)
        : base(options)
    {
    }

    public DbSet<ProjectItem> Projects =>
        Set<ProjectItem>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProjectItem>(
            entity =>
            {
                entity.ToTable("projects");

                entity.HasKey(p => p.Id);

                entity.Property(p => p.Id)
                    .HasColumnType("char(36)")
                    .IsRequired();

                entity.Property(p => p.Name)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(p => p.Description)
                    .HasMaxLength(2000);

                entity.Property(p => p.OwnerUserId)
                    .HasMaxLength(64)
                    .IsRequired();

                entity.Property(p => p.CreatedAt)
                    .IsRequired();

                entity.Property(p => p.UpdatedAt)
                    .IsRequired();

                entity.HasIndex(p => p.OwnerUserId)
                    .HasDatabaseName(
                        "ix_projects_owner_user_id");

                entity.Property(p => p.TaskIds)
                    .HasConversion(
                        ids => JsonSerializer.Serialize(
                            ids,
                            (JsonSerializerOptions?)null),

                        json =>
                            JsonSerializer.Deserialize<
                                List<int>>(
                                json,
                                (JsonSerializerOptions?)null)
                            ?? new List<int>())
                    .Metadata.SetValueComparer(
                        new ValueComparer<List<int>>(
                            (a, b) =>
                                (a ?? new())
                                    .SequenceEqual(
                                        b ?? new()),

                            v => v.Aggregate(
                                0,
                                (hash, id) =>
                                    HashCode.Combine(
                                        hash,
                                        id)),

                            v => v.ToList()));
            });
    }
}