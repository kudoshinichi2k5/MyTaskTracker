# Frontend

The frontend contains two Angular 18 applications. Both use the environment files under `src/environments/` to select API origins at build time.

| Application | Local port | Purpose | Documentation |
| --- | ---: | --- | --- |
| CustomerApp | 4200 | Customer task, project, comment, and notification workflow | [CustomerApp README](CustomerApp/README.md) |
| AdminPortal | 4300 | Administrative task, user, project, and comment views | [AdminPortal README](AdminPortal/README.md) |

## Install and run

Run each application from its own directory:

```powershell
Push-Location CustomerApp
npm.cmd ci
npm.cmd start
Pop-Location
```

Use the same commands from `AdminPortal` in a second terminal. If PowerShell blocks `npm.ps1`, use `npm.cmd` as shown.

## Build configurations

Both applications provide `development`, `testing`, `staging`, and `production` configurations:

```powershell
npm.cmd run build:testing
npm.cmd run build:staging
npm.cmd run build:production
```

Production and staging files still contain example domain placeholders. Replace them with the deployed HTTPS API origins before building a deployment bundle.
