terraform {
  backend "azurerm" {
    resource_group_name  = "TaskTrackerRG"
    storage_account_name = "tfstate4459"
    container_name       = "tfstate"
    key                  = "dev.terraform.tfstate"

    # Ép sử dụng token OIDC từ GitHub Actions
    use_oidc             = true
    use_azuread_auth     = true
  }
}
