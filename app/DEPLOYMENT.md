# Development deployment

This repository currently has a local development stack and an Azure dev
deployment path. It does not have a configured production cloud environment.

## Local Docker Compose

Run from the `app` directory. Create `.env` from `.env.example` if it does not
exist, then replace the example passwords with local development values:

```powershell
if (-not (Test-Path .env)) { Copy-Item .env.example .env }
docker compose up --build
```

Do not overwrite an existing `.env`; it may contain your local values. The
Compose stack builds the `runtime` Docker targets and serves the apps at
`http://localhost:4200` and `http://localhost:4300`. The frontends use
`environment.ts`, which points to the backend ports exposed on localhost.
MariaDB and each backend use the matching service password from `.env`.

To stop the stack, run `docker compose down` from `app`. Add `-v` only when you
intend to delete the local MariaDB volume and all local data.

## Azure dev deployment

Pushes to `main` run the CI checks. When app files change, CI publishes backend
and frontend build artifacts, packages those artifacts with the Docker
`runtime-artifact` targets, pushes one ARM64 image per changed app/service to
ACR, and updates the image tags consumed by the Argo CD applications in
`deploy/argocd-apps/applicationset-services.yaml`.

The cloud frontend artifact uses the Angular `dev` configuration and calls:

- `https://api.mytasktracker.work.gd/auth`
- `https://api.mytasktracker.work.gd/tasks`
- `https://api.mytasktracker.work.gd/notifications`
- `https://api.mytasktracker.work.gd/projects`
- `https://api.mytasktracker.work.gd/comments`

The API ingress strips each path prefix before forwarding requests to its
service. The API services allow requests from the dev frontend domains in
`deploy/values/base/`.

The domain currently resolves to `65.52.163.27`, but HTTPS to the website and
API did not respond during the latest check. DNS resolution alone does not
confirm that the dev ingress, TLS certificate, or applications are available.

## Scope and safety

The Azure/Argo CD values in this repository target the `dev` namespace. Do not
use the current image pipeline or dev domain as a production deployment path.
The application still contains demo credentials and in-memory authentication
state; it is not suitable for public production use as-is.
