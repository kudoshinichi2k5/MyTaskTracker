# MyTaskTracker application

`app/` contains the runnable application stack: two Angular 18 SPAs, five ASP.NET Core 8 APIs, MariaDB initialization, Valkey, EF Core migrations, and Docker Compose orchestration.

## Components

| Path | Responsibility | Local address |
| --- | --- | --- |
| `Frontend/CustomerApp` | End-user task, project, comment, and notification interface. | `http://localhost:4200` |
| `Frontend/AdminPortal` | Administration interface for users, projects, tasks, and comments. | `http://localhost:4300` |
| `Backend/Tracker.AuthService` | OAuth authorization, registration, opaque token verification, refresh, and revocation. | `http://localhost:5001` |
| `Backend/Tracker.TaskService` | Task APIs and administration-oriented task summaries. | `http://localhost:5002` |
| `Backend/Tracker.NotificationService` | User notification APIs. | `http://localhost:5003` |
| `Backend/Tracker.ProjectService` | Project APIs and task-to-project membership. | `http://localhost:5004` |
| `Backend/Tracker.CommentService` | Task comment APIs. | `http://localhost:5005` |
| `infra/mariadb/init` | Creates the five databases and least-privilege database users. | -- |
| `docker-compose.yml` | Local dependencies, migrations, services, and frontend containers. | -- |

The architecture diagram shows how the two frontends use AuthService and the four resource APIs, with per-service MariaDB databases and Valkey for OAuth state.

![Application architecture](../reports/Figures/application-architecture.png)

## Authentication and authorization

The frontends are public OAuth clients. They use Authorization Code with PKCE (S256), not password grant. AuthService hosts the login page, stores authorization requests and one-time codes in Valkey, and issues random opaque access and refresh tokens.

1. The SPA redirects to `GET /connect/authorize` with `state` and an S256 PKCE challenge.
2. After login, AuthService redirects to the registered callback with a one-time `code` and the original `state`.
3. The callback exchanges `code` and `code_verifier` at `POST /connect/token`.
4. Protected API requests use `Authorization: Bearer <opaque-access-token>`.
5. Each resource service calls AuthService `GET /verify`; it does not parse JWT claims or access Valkey directly.
6. Refresh uses `grant_type=refresh_token` and rotates the refresh session. Logout uses `POST /connect/revocation`.

![OAuth Authorization Code with PKCE and opaque-token validation](../reports/Figures/authentication-flow.png)

## Data ownership

MariaDB is shared as a server in local development, but service tables are not shared. Each service uses its own database and database user:

| Service | Database | Database user |
| --- | --- | --- |
| AuthService | `tracker_auth` | `auth_service` |
| TaskService | `tracker_tasks` | `task_service` |
| NotificationService | `tracker_notifications` | `notification_service` |
| ProjectService | `tracker_projects` | `project_service` |
| CommentService | `tracker_comments` | `comment_service` |

Valkey is not business-data storage. It retains OAuth transaction state, authorization codes, access-token hashes, refresh-session hashes, and short-lived login protection state.

## Run locally

### Prerequisites

- .NET SDK 8
- Node.js 22 and npm
- Docker Desktop with Docker Compose

### Full stack

```powershell
docker compose up -d
```

This is the normal local startup command. On a fresh clone, first create `.env` from `.env.example`. Use `docker compose up -d --build` only after a Docker image needs rebuilding. The compose stack starts MariaDB and Valkey, runs migration containers before their APIs, and exposes the two frontends and five APIs at the addresses in the component table. Never commit `.env`.

When `tracker_auth` is empty, the AuthService migration currently seeds `admin` / `123456` (admin role) and `alice` / `employee123`. This runs from both Docker Compose and the Helm migration Job; it is not limited to local development. Do not expose a deployment that uses these default credentials. Before production, remove this seeding or make it explicitly development-only and provision users securely. This warning documents the current behavior; it does not prevent the seeding.

### Reset local data

```powershell
docker compose down -v
docker compose up -d
```

This removes the local MariaDB and Valkey volumes. It is destructive and should only be used when local data and sessions can be discarded.

## Build and test

```powershell
dotnet test Backend/Tests/Tracker.AuthService.Tests/Tracker.AuthService.Tests.csproj

Push-Location Frontend/CustomerApp
npm ci
npm.cmd run build:dev
npm.cmd test
Pop-Location
```

Run the equivalent frontend commands from `Frontend/AdminPortal`. CI performs these checks for changed services and applications.

## UI screenshots

The CustomerApp screenshot shows an authenticated user view; it must not contain real credentials, bearer tokens, or private personal data.

![CustomerApp](../reports/Figures/customer-app.png)

The AdminPortal screenshot shows an authenticated administration view; it must not contain real credentials, bearer tokens, or private personal data.

![AdminPortal](../reports/Figures/admin-portal.png)

The canonical image-to-README mapping and replacement instructions are in [../reports/Figures/README.md](../reports/Figures/README.md).
