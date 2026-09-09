# Infrastructure

Each environment keeps its deployable resource values in its own `terraform.tfvars` file. Terraform backend settings are separate from Terraform input variables and must be supplied during `terraform init`.

Initialize an environment with:

```powershell
terraform init `
	-backend-config="resource_group_name=<state-resource-group>" `
	-backend-config="storage_account_name=<state-storage-account>" `
	-backend-config="container_name=tfstate" `
	-backend-config="key=dev.terraform.tfstate"
terraform validate
terraform plan -var-file=terraform.tfvars
```

Replace the placeholder values with deployment-specific Azure Storage settings. Use the matching state key for `staging` or `prod`. Backend values cannot be loaded from `terraform.tfvars`; Terraform initializes the backend before it loads that file.
