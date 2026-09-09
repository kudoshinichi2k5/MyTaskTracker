# Infrastructure

Each environment keeps its deployable values in its own `terraform.tfvars` file. Backend storage settings are supplied locally through an ignored `backend.hcl` file.

Initialize an environment with:

```powershell
Copy-Item backend.hcl.example backend.hcl
terraform init -backend-config=backend.hcl
terraform validate
terraform plan -var-file=terraform.tfvars
```

Replace the placeholder values in `backend.hcl` before initialization. Run the commands from the selected environment directory.
