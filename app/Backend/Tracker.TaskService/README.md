# Tracker.TaskService

Task and administrative task API.

- Local URL: `http://localhost:5002`
- Database: `tracker_tasks`
- Project: `Tracker.TaskService.csproj`

## Responsibilities

- Create, update, list, and delete user tasks.
- Provide administrative task and user operations.
- Validate bearer tokens through AuthService.
- Persist task data with EF Core and MariaDB.

## Run

From `app/`:

```powershell
dotnet run --project Backend/Tracker.TaskService
```

The service uses `AuthService__BaseUrl` and `AuthService__VerifyPath` to verify tokens. See [Backend README](../README.md).
