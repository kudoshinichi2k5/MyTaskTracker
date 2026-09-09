# GitHub Workflows

The repository currently contains `workflows/ci.yml`.

The workflow runs on pushes and pull requests targeting `main`. It checks out the repository, logs into Azure with GitHub Actions OIDC, and verifies the active Azure subscription with `az account show`.

Required repository secrets:

- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`

The workflow requests `id-token: write` and `contents: read`. Keep the federated identity subject and Azure permissions aligned with the Terraform identity configuration under `infrastructure/`.
