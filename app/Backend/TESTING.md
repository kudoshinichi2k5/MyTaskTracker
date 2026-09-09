# Backend tests

`Tracker.AuthService` and `Tracker.TaskService` each have a sibling xUnit test project (`Tracker.AuthService.Tests`, `Tracker.TaskService.Tests`). `Tracker.NotificationService`, `Tracker.ProjectService`, and `Tracker.CommentService` do not have test projects yet.

## Running the tests

No database, Docker, or `.env` setup is required. Each suite runs against an isolated EF Core InMemory database and does not touch MariaDB.

```powershell
dotnet test app/Backend/Tracker.AuthService.Tests/Tracker.AuthService.Tests.csproj
dotnet test app/Backend/Tracker.TaskService.Tests/Tracker.TaskService.Tests.csproj
```

## Coverage layers

- Integration tests boot the real minimal API pipeline with `WebApplicationFactory<Program>`, including routing, model binding, authentication, authorization, and EF Core.
- Unit tests exercise `EfUserStore` and `EfTaskStore` directly against fresh InMemory contexts.
- TaskService fakes only the outbound AuthService `/verify` HTTP boundary, so its real opaque-token authentication handler remains under test.

## Test seam

Both services use two behavior-preserving test hooks:

1. `public partial class Program { }` exposes the top-level program to `WebApplicationFactory`.
2. The `Testing` environment calls `EnsureCreated()` instead of relational `Migrate()` because EF Core InMemory does not support migrations. Development, staging, and production keep the normal migration path.

## Extending coverage

The remaining resource services follow the same EF Core and opaque-token pattern. Add a sibling test project, a `Testing` startup seam, an API factory, a local AuthService `/verify` test double, endpoint tests, and store unit tests for each service.
