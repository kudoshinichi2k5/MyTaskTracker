# Terraform Environments

Each environment is an independent Terraform root with its own state key and values:

- `dev`
- `staging`
- `prod`

Run Terraform from the selected environment directory. Copy `backend.hcl.example` to the ignored `backend.hcl`, fill in the Azure Storage backend values, then run:

```powershell
terraform init -backend-config=backend.hcl
terraform validate
terraform plan -var-file=terraform.tfvars
```

Do not commit `backend.hcl`, state files, or credentials. See [infrastructure README](../README.md) for the backend workflow.
