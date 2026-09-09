# Application Infrastructure

This folder contains local infrastructure used by the application. It is separate from the Azure Terraform configuration under the repository-level `infrastructure/` folder.

- [MariaDB initialization](mariadb/README.md) creates the five service databases and users.
- `docker-compose.yml` mounts the initialization script and persists data in the `mariadb_data` volume.

Start the local database from `app/`:

```powershell
docker compose up -d mariadb
```

Database credentials come from `app/.env`. Never commit the real `.env` file.
