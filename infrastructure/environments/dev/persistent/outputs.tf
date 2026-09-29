output "app_rg_name" { value = azurerm_resource_group.app_rg.name }
output "app_rg_location" { value = azurerm_resource_group.app_rg.location }
output "app_rg_id" { value = azurerm_resource_group.app_rg.id }

output "shared_rg_name" { value = azurerm_resource_group.shared_rg.name }
output "shared_rg_location" { value = azurerm_resource_group.shared_rg.location }
output "shared_rg_id" { value = azurerm_resource_group.shared_rg.id }

output "acr_id" { value = module.acr.acr_id }
output "acr_login_server" { value = module.acr.login_server }

output "tf_ci_principal_id" { value = module.terraform_ci_identity.principal_id }

output "backup_storage_account_name" {
  description = "Tên Storage Account dùng để lưu backup database"
  value       = azurerm_storage_account.backup.name
}   