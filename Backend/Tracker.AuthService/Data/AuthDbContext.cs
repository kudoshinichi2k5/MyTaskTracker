using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;
using Tracker.AuthService.Models;

namespace Tracker.AuthService.Data;

public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");

            entity.HasKey(u => u.Id);

            entity.Property(u => u.Id)
                .HasMaxLength(36);

            entity.Property(u => u.Username)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(u => u.Email)
                .HasMaxLength(320)
                .IsRequired();

            entity.Property(u => u.PasswordHash)
                .HasMaxLength(512)
                .IsRequired();

            entity.HasIndex(u => u.Username)
                .IsUnique();

            entity.HasIndex(u => u.Email)
                .IsUnique();

            entity.Property(u => u.Roles)
                .HasConversion(
                    roles => JsonSerializer.Serialize(
                        roles,
                        (JsonSerializerOptions?)null),

                    json => JsonSerializer.Deserialize<List<string>>(
                        json,
                        (JsonSerializerOptions?)null)
                        ?? new List<string>())
                .Metadata.SetValueComparer(
                    new ValueComparer<List<string>>(
                        (a, b) =>
                            (a ?? new()).SequenceEqual(b ?? new()),

                        v => v.Aggregate(
                            0,
                            (hash, s) =>
                                HashCode.Combine(
                                    hash,
                                    s.GetHashCode())),

                        v => v.ToList()));
        });
    }
}