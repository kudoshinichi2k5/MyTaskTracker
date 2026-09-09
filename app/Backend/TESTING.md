# Backend tests

All five backend services now have test projects under `Backend/Tests/`, separate from production service folders. AuthService and TaskService use integration plus store unit tests. NotificationService has integration coverage for authenticated notification workflows. ProjectService and CommentService currently use store unit tests; their TestServer hosts returned an environment-specific `400 Invalid Hostname` before routing, so endpoint integration coverage remains a follow-up.

## Running the tests

No database, Docker, or `.env` setup is required. Each suite runs against an isolated EF Core InMemory database and does not touch MariaDB.

```powershell
dotnet test app/Backend/Tests/Tracker.AuthService.Tests/Tracker.AuthService.Tests.csproj
dotnet test app/Backend/Tests/Tracker.TaskService.Tests/Tracker.TaskService.Tests.csproj
dotnet test app/Backend/Tests/Tracker.NotificationService.Tests/Tracker.NotificationService.Tests.csproj
dotnet test app/Backend/Tests/Tracker.ProjectService.Tests/Tracker.ProjectService.Tests.csproj
dotnet test app/Backend/Tests/Tracker.CommentService.Tests/Tracker.CommentService.Tests.csproj
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

The remaining resource services follow the same EF Core and opaque-token pattern. Add endpoint integration coverage after the TestServer host issue is resolved.

## Frontend test proposal

Both Angular applications already use Jasmine/Karma and contain starter specs. The next frontend test group should be:

1. Service HTTP contract tests with `HttpClientTestingModule`/`HttpTestingController` for auth, task, project, comment, notification, report, and admin-user services.
2. Auth interceptor tests for bearer header injection, missing token behavior, and `401` recovery.
3. Guard tests for anonymous, authenticated, and admin-only navigation.
4. Focused component tests for login/register validation, task/project forms, comment editing, notification unread state, and admin dashboard summaries.
5. One build/test job per Angular app in GitHub Actions, using `npm ci`, `npm test -- --watch=false --browsers=ChromeHeadless`, and the matching production build.

Keep frontend tests independent of live APIs by mocking HTTP requests and environment-specific API origins.
