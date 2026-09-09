# Tracker.CommentService

Comment thread API for tasks.

- Local URL: `http://localhost:5005`
- Database: `tracker_comments`
- Project: `Tracker.CommentService.csproj`

## Responsibilities

- Create and retrieve comments attached to tasks.
- Validate bearer tokens through AuthService.
- Persist comment data with EF Core and MariaDB.

## Run

From `app/`:

```powershell
dotnet run --project Backend/Tracker.CommentService
```

See [Backend README](../README.md) for shared configuration and database setup.
