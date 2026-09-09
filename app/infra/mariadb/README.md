# MariaDB Initialization

`init/01-init-databases.sh` runs automatically when MariaDB starts with an empty data directory. It creates these databases and matching service users:

- `tracker_auth` / `auth_service`
- `tracker_tasks` / `task_service`
- `tracker_notifications` / `notification_service`
- `tracker_projects` / `project_service`
- `tracker_comments` / `comment_service`

Passwords are read from `MARIADB_ROOT_PASSWORD` and the `*_DB_PASSWORD` variables supplied through `app/.env`.

The script creates database permissions, not application tables. EF Core migrations owned by each backend service create and update those tables.

To re-run initialization from scratch:

```powershell
Push-Location app
docker compose down -v
docker compose up -d mariadb
Pop-Location
```

The `-v` flag deletes the local `mariadb_data` volume and all local database data.
