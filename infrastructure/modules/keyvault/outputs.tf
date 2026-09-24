output "kv_id" {
  description = "ID của Key Vault"
  value       = azurerm_key_vault.kv.id
}

output "kv_uri" {
  description = "URI của Key Vault"
  value       = azurerm_key_vault.kv.vault_uri
}