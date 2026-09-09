# Tracker.AuthService

Authentication and identity API for TaskTracker.

- Local URL: `http://localhost:5001`
- Database: `tracker_auth`
- Project: `Tracker.AuthService.csproj`

## Responsibilities

- Register users and authenticate credentials.
- Issue and refresh opaque access tokens.
- Validate access tokens through `GET /verify`.
- Invalidate refresh sessions through logout.
- Expose health and OpenAPI endpoints.

## Run

From `app/`:

```powershell
dotnet run --project Backend/Tracker.AuthService
```

Start MariaDB and configure the local user secrets before running. See [Backend README](../README.md) and [app README](../../README.md).
