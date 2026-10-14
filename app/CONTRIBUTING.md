# Contributing

## Local configuration

Do not commit credentials. `appsettings.Development.json` and `.env` are ignored intentionally. Start MariaDB, then configure the local .NET user secrets:

```powershell
docker compose up -d mariadb
Push-Location Backend; .\setup-local-secrets.ps1; Pop-Location
```

The script uses the development database accounts created by `infra/mariadb/init/01-init-databases.sql`. If your MariaDB password differs, supply it explicitly:

```powershell
Push-Location Backend; .\setup-local-secrets.ps1 -Password '<your-password>'; Pop-Location
```

Use Azure Key Vault-backed secrets for the cloud dev deployment. Do not copy local `.env` values or user secrets into CI or cloud configuration.

## Git conventions

- Branches: `feature/<short-description>`, `fix/<short-description>`, `chore/<short-description>`.
- Commit messages follow Conventional Commits, for example: `fix(notification): configure local database secret`.
- Keep each commit focused and include migrations with the code that requires them.
- Before opening a pull request, run the affected `dotnet build` command and do not commit generated files, `.env`, user secrets, or `appsettings.Development.json`.
