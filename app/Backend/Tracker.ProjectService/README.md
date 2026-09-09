# Tracker.ProjectService

Project grouping API used by task workflows.

- Local URL: `http://localhost:5004`
- Database: `tracker_projects`
- Project: `Tracker.ProjectService.csproj`

## Responsibilities

- Create, update, list, and delete projects.
- Group task work under projects.
- Validate bearer tokens through AuthService.
- Persist project data with EF Core and MariaDB.

## Run

From `app/`:

```powershell
dotnet run --project Backend/Tracker.ProjectService
```

See [Backend README](../README.md) for shared configuration and database setup.
