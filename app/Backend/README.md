# Backend

The backend is a .NET 8 microservice system. Services run independently, expose HTTP APIs, and own separate MariaDB databases.

| Service | Port | Database | Responsibility |
| --- | ---: | --- | --- |
| [Tracker.AuthService](Tracker.AuthService/README.md) | 5001 | `tracker_auth` | Registration, login, token and session validation |
| [Tracker.TaskService](Tracker.TaskService/README.md) | 5002 | `tracker_tasks` | User tasks and admin task/user operations |
| [Tracker.NotificationService](Tracker.NotificationService/README.md) | 5003 | `tracker_notifications` | User notifications |
| [Tracker.ProjectService](Tracker.ProjectService/README.md) | 5004 | `tracker_projects` | Project grouping for tasks |
| [Tracker.CommentService](Tracker.CommentService/README.md) | 5005 | `tracker_comments` | Task comment threads |

## Common layout

Each service keeps its API endpoints, models, EF Core data access, authentication adapter, and service logic in its own project. `bin/` and `obj/` are generated and should not be edited or committed.

## Run a service

From the `app` directory:

```powershell
dotnet run --project Backend/Tracker.AuthService
dotnet run --project Backend/Tracker.TaskService
dotnet run --project Backend/Tracker.NotificationService
dotnet run --project Backend/Tracker.ProjectService
dotnet run --project Backend/Tracker.CommentService
```

Start MariaDB first and configure local user secrets as described in [CONTRIBUTING.md](../CONTRIBUTING.md). Each service exposes `/health`; protected services use AuthService's `/verify` endpoint.

## Build and migrations

```powershell
dotnet build Backend/Tracker.AuthService/Tracker.AuthService.csproj
dotnet ef database update --project Backend/Tracker.AuthService
```

Repeat the build or migration command with the target service project when changing another service. See [app/README.md](../README.md) for the complete database and local development workflow.
