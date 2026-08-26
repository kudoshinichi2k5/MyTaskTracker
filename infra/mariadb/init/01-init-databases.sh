#!/usr/bin/env bash
# Runs inside the mariadb container on first init only (empty data volume).
# Unlike a .sql file, a .sh file dropped in docker-entrypoint-initdb.d is
# sourced by the MariaDB entrypoint, so it can see the same environment
# variables docker-compose.yml passes to this container - that's the only
# way to get the real per-service passwords out of .env and into the
# CREATE USER statements instead of a hardcoded literal.
set -euo pipefail

: "${MARIADB_ROOT_PASSWORD:?MARIADB_ROOT_PASSWORD is required}"
: "${AUTH_DB_PASSWORD:?AUTH_DB_PASSWORD is required}"
: "${TASK_DB_PASSWORD:?TASK_DB_PASSWORD is required}"
: "${NOTIFICATION_DB_PASSWORD:?NOTIFICATION_DB_PASSWORD is required}"
: "${PROJECT_DB_PASSWORD:?PROJECT_DB_PASSWORD is required}"
: "${COMMENT_DB_PASSWORD:?COMMENT_DB_PASSWORD is required}"

mysql -u root -p"${MARIADB_ROOT_PASSWORD}" <<-EOSQL
CREATE DATABASE IF NOT EXISTS tracker_auth CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE DATABASE IF NOT EXISTS tracker_tasks CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE DATABASE IF NOT EXISTS tracker_notifications CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE DATABASE IF NOT EXISTS tracker_projects CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE DATABASE IF NOT EXISTS tracker_comments CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

CREATE USER IF NOT EXISTS 'auth_service'@'%' IDENTIFIED BY '${AUTH_DB_PASSWORD}';
GRANT ALL PRIVILEGES ON tracker_auth.* TO 'auth_service'@'%';

CREATE USER IF NOT EXISTS 'task_service'@'%' IDENTIFIED BY '${TASK_DB_PASSWORD}';
GRANT ALL PRIVILEGES ON tracker_tasks.* TO 'task_service'@'%';

CREATE USER IF NOT EXISTS 'notification_service'@'%' IDENTIFIED BY '${NOTIFICATION_DB_PASSWORD}';
GRANT ALL PRIVILEGES ON tracker_notifications.* TO 'notification_service'@'%';

CREATE USER IF NOT EXISTS 'project_service'@'%' IDENTIFIED BY '${PROJECT_DB_PASSWORD}';
GRANT ALL PRIVILEGES ON tracker_projects.* TO 'project_service'@'%';

CREATE USER IF NOT EXISTS 'comment_service'@'%' IDENTIFIED BY '${COMMENT_DB_PASSWORD}';
GRANT ALL PRIVILEGES ON tracker_comments.* TO 'comment_service'@'%';

FLUSH PRIVILEGES;
EOSQL