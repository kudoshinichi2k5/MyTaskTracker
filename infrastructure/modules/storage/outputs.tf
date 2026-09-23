output "storage_account_id" {
  description = "ID của Storage Account"
  value       = azurerm_storage_account.sa.id
}

output "storage_account_name" {
  description = "Tên của Storage Account"
  value       = azurerm_storage_account.sa.name
}

output "backup_container_name" {
  description = "Tên của Storage Container chứa backup"
  value       = azurerm_storage_container.backup.name
}