# AdminPortal

Administrative Angular 18 application for task summaries, user access, projects, and comments.

- Local URL: `http://localhost:4300`
- Package name: `admin-portal`
- API configuration: `src/environments/`

## Run locally

From this directory:

```powershell
npm.cmd ci
npm.cmd start -- --port 4300
```

The development environment targets the local backend APIs on ports `5001` through `5005`. Start MariaDB and the backend services first; see [app README](../../README.md).

## Build and test

```powershell
npm.cmd run build
npm.cmd run build:testing
npm.cmd run build:staging
npm.cmd run build:production
npm.cmd test
```

The staging and production environment files contain example HTTPS domains. Replace those values with the deployed API origins before creating a deployment bundle.
