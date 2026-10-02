output "keyvault_name" { value = module.keyvault.kv_name }
output "tenant_id" { value = data.azurerm_client_config.current.tenant_id }
output "backend_client_ids" {
  value = { for k, v in module.backend_workload_identities : k => v.client_id }
}

output "backup_client_id" {
  value = module.backup_workload_identity.client_id
}
output "backup_storage_account_name" {
  value = local.backup_sa_name
}

output "cert_manager_client_id" { value = module.cert_manager_identity.client_id }
output "dns_zone_name" { value = local.dns_zone_name }
output "shared_rg_name" { value = local.shared_rg_name }
output "subscription_id" { value = local.subscription_id }