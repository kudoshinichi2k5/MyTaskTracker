# TaskTracker

TaskTracker is a .NET 8 microservices sample with two Angular 18 applications:

- **CustomerApp** (`http://localhost:4200`) manages a signed-in user's tasks and notifications.
- **AdminPortal** (`http://localhost:4300`) provides task summaries and role administration.

Authentication preserves an OWIN-style password-grant contract while using ASP.NET Core middleware. The system utilizes a centralized authentication model where tokens are simply random, opaque strings. These strings are stored in a database/memory and must be explicitly looked up and validated on every single request, completely bypassing JWTs.

## Architecture

```mermaid
flowchart LR
    Customer[CustomerApp :4200] --> Auth[Auth Service :5001]
    Customer --> Tasks[Task Service :5002]
    Customer --> Notifications[Notification Service :5003]
    Admin[AdminPortal :4300] --> Auth
    Admin --> Tasks
    Tasks --> Auth
    Notifications --> Auth
```

Both resource services validate bearer tokens against AuthService. NotificationService and TaskService therefore use the same opaque-token authentication model; neither accepts JWTs.

## Projects

| Project | Responsibility | Local URL |
| --- | --- | --- |
| `Backend/Tracker.AuthService` | Login, refresh, logout, and token verification | `http://localhost:5001` |
| `Backend/Tracker.TaskService` | User tasks and admin task/user endpoints | `http://localhost:5002` |
| `Backend/Tracker.NotificationService` | User notifications | `http://localhost:5003` |
| `Frontend/CustomerApp` | Customer-facing Angular application | `http://localhost:4200` |
| `Frontend/AdminPortal` | Admin Angular application | `http://localhost:4300` |

## Authentication flow

1. An Angular app sends form-encoded credentials to `POST /token` on AuthService.
2. AuthService returns a camelCase response containing `accessToken`, `refreshToken`, `expiresIn`, `username`, and `roles`.
3. The app stores its session locally and adds `Authorization: Bearer <token>` to API requests.
4. TaskService and NotificationService call AuthService's `GET /verify?token=...` endpoint to validate the opaque token and create user claims.
5. `POST /api/v1/auth/refresh` renews the access token; `POST /api/v1/auth/logout` invalidates the refresh session.

Example local sign-in request:

```text
POST http://localhost:5001/token
Content-Type: application/x-www-form-urlencoded

grant_type=password&username=admin&password=123456
```

## Run locally

Prerequisites: .NET 8 SDK, Node.js, and npm.

Start each backend service in a separate terminal:

```powershell
dotnet run --project Backend/Tracker.AuthService
dotnet run --project Backend/Tracker.TaskService
dotnet run --project Backend/Tracker.NotificationService
```

Start each frontend in a separate terminal:

```powershell
Push-Location Frontend/CustomerApp; npm.cmd install; npm.cmd start
Push-Location Frontend/AdminPortal; npm.cmd install; npm.cmd start
```

Use `npm.cmd` in PowerShell if local execution policy blocks `npm.ps1`.

## Build

```powershell
dotnet build Backend/Tracker.AuthService/Tracker.AuthService.csproj
dotnet build Backend/Tracker.TaskService/Tracker.TaskService.csproj
dotnet build Backend/Tracker.NotificationService/Tracker.NotificationService.csproj

Push-Location Frontend/CustomerApp; npm.cmd run build:production; Pop-Location
Push-Location Frontend/AdminPortal; npm.cmd run build:production; Pop-Location
```

The production Angular builds use their respective `environment.prod.ts` files and do not require build-time Google Fonts access.

## IIS deployment

See [DEPLOYMENT.md](DEPLOYMENT.md) for prerequisites, production configuration, publish commands, IIS application-pool settings, frontend deployment paths, and post-deployment verification.

Before any production build, replace the `*.yourdomain.com` placeholders in both frontend `environment.prod.ts` files with the final HTTPS API origins. They are compiled into the bundles.

## Important limitations

This repository is a demonstration, not production-ready identity or data infrastructure:

- AuthService currently contains the demo account `admin` / `123456`.
- Tokens, refresh sessions, tasks, notifications, users, and roles are in memory only.
- Restarting a service or recycling an IIS application pool invalidates sessions and loses data.
- Replace the demo credential lookup and in-memory stores with persistent, securely managed implementations before exposing the application publicly.
