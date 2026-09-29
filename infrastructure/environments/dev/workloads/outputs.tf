output "keyvault_name" { value = module.keyvault.kv_name }
output "tenant_id" { value = data.azurerm_client_config.current.tenant_id }
output "backend_client_ids" {
  value = { for k, v in module.backend_workload_identities : k => v.client_id }
}