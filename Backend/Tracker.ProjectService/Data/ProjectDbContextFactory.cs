using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Tracker.ProjectService.Data;

public class ProjectDbContextFactory
    : IDesignTimeDbContextFactory<
        ProjectDbContext>
{
    public ProjectDbContext CreateDbContext(
        string[] args)
    {
        var configuration =
            new ConfigurationBuilder()
                .SetBasePath(
                    Directory.GetCurrentDirectory())
                .AddJsonFile(
                    "appsettings.json",
                    optional: true)
                .AddJsonFile(
                    "appsettings.Development.json",
                    optional: true)
                .AddUserSecrets<
                    ProjectDbContextFactory>(
                    optional: true)
                .AddEnvironmentVariables()
                .Build();

        var connectionString =
            configuration.GetConnectionString(
                "ProjectDb")
            ?? "Server=localhost;" +
               "Port=3306;" +
               "Database=tracker_projects;" +
               "User=project_service;" +
               "Password=change_me_project;";

        var connectionString =
            configuration.GetConnectionString("ProjectDb");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = "Server=localhost;Port=3306;Database=tracker_projects;User=project_service;Password=change_me_project;";
        }

        var optionsBuilder =
            new DbContextOptionsBuilder<
                ProjectDbContext>();

        optionsBuilder.UseMySql(
            connectionString,
            ServerVersion.AutoDetect(
                connectionString));

        return new ProjectDbContext(
            optionsBuilder.Options);
    }
}