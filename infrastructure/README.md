# Infrastructure

Each environment keeps its deployable resource values in its own `terraform.tfvars` file. The `dev` environment includes its Azure remote backend in `backend.tf`; Terraform backend settings are separate from Terraform input variables.

Initialize an environment with:

```powershell
terraform init
terraform validate
terraform plan -var-file=terraform.tfvars
```

For `staging` and `prod`, add the environment-specific Azure backend settings before initializing them. Backend values cannot be loaded from `terraform.tfvars`; Terraform initializes the backend before it loads that file.
