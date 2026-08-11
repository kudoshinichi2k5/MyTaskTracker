# IIS deployment

This sample has three independently hosted ASP.NET Core services and two Angular applications. Deploy the services as separate IIS applications (or sites) and deploy the Angular `browser` folders as IIS applications named `app1` and `app2`.

Do not expose this build to the Internet as-is: AuthService has a hard-coded demo account (`admin` / `123456`) and all tokens, refresh sessions, tasks, notifications, users, and role changes are held only in process memory. An app-pool recycle or deployment invalidates every session and loses all data.

## Prerequisites

- IIS with the URL Rewrite module (needed by both Angular `web.config` files).
- The .NET 8 Hosting Bundle installed on the IIS server; restart IIS after installation.
- HTTPS bindings and certificates for every public application/API origin.
- IIS application-pool identities with read/execute access to the deployment directories and write access to a dedicated Data Protection keys directory if cookies or protected data are added later.

## Configure production values

Before building the frontends, replace the `*.yourdomain.com` placeholder URLs in both `src/environments/environment.prod.ts` files with the final HTTPS API origins. They are compiled into the JavaScript bundle.

Set these environment variables for the TaskService and NotificationService IIS applications (or their equivalent IIS `web.config` environment variables):

```text
ASPNETCORE_ENVIRONMENT=Production
AuthService__BaseUrl=http://127.0.0.1:5001
AllowedFrontendOrigins=https://app.yourdomain.com,https://admin.yourdomain.com
```

`AuthService__BaseUrl` must be reachable from the IIS worker process. If AuthService is hosted behind an HTTPS IIS binding instead of local port 5001, use that HTTPS origin. Do not set a public wildcard for `AllowedFrontendOrigins`.

## Build and publish

Run from the repository root. In PowerShell, use `npm.cmd` if execution policy blocks `npm.ps1`.

```powershell
dotnet publish Backend/Tracker.AuthService/Tracker.AuthService.csproj -c Release -o .\artifacts\auth
dotnet publish Backend/Tracker.TaskService/Tracker.TaskService.csproj -c Release -o .\artifacts\tasks
dotnet publish Backend/Tracker.NotificationService/Tracker.NotificationService.csproj -c Release -o .\artifacts\notifications

Push-Location Frontend\CustomerApp; npm.cmd ci; npm.cmd run build:production; Pop-Location
Push-Location Frontend\AdminPortal; npm.cmd ci; npm.cmd run build:production; Pop-Location
```

Deploy `.\artifacts\auth`, `.\artifacts\tasks`, and `.\artifacts\notifications` to their respective IIS app roots. Deploy `Frontend\CustomerApp\dist\frontend\browser` to `/app1` and `Frontend\AdminPortal\dist\admin-portal\browser` to `/app2`. Keep the generated `web.config` files; they enable Angular deep links.

## IIS layout and verification

Configure the ASP.NET Core apps with **No Managed Code**, using separate application pools. If hosting them on loopback ports, bind AuthService to `127.0.0.1:5001`, TaskService to `127.0.0.1:5002`, and NotificationService to `127.0.0.1:5003`; expose the APIs through HTTPS reverse-proxy routes or dedicated HTTPS sites.

After deployment, verify `/health` for all services, then sign in through both portals and confirm task, notification, and admin-summary requests. A successful login must obtain an opaque token from AuthService; both resource services validate it through AuthService's `/verify` endpoint.
