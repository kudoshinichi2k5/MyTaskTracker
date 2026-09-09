# Terraform Modules

Reusable AzureRM modules used by the environment roots:

| Module | Responsibility |
| --- | --- |
| `acr` | Azure Container Registry |
| `aks` | Azure Kubernetes Service cluster and node pool |
| `identity` | User-assigned identity and GitHub Actions federated credential |
| `networking` | Virtual network, subnets, and database NSG |
| `database` | Reserved database module surface for future Azure database resources |

Modules receive deployment-specific values through variables. Environment values belong under `infrastructure/environments/<name>/terraform.tfvars`.
