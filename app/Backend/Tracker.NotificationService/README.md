# Tracker.NotificationService

Notification API for user-facing task activity.

- Local URL: `http://localhost:5003`
- Database: `tracker_notifications`
- Project: `Tracker.NotificationService.csproj`

## Responsibilities

- Read and manage notifications for the authenticated user.
- Persist notifications with EF Core and MariaDB.
- Validate bearer tokens through AuthService.

## Run

From `app/`:

```powershell
dotnet run --project Backend/Tracker.NotificationService
```

The service uses `AuthService__BaseUrl` and `AuthService__VerifyPath` for token verification. See [Backend README](../README.md).
