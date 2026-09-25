output "generated_db_passwords" {
  value     = { for k, v in random_password.db_passwords : k => v.result }
  sensitive = true
}

output "workload_identity_client_ids" {
  description = "Danh sách Client ID của các backend service để điền vào Helm values.yaml"
  value = {
    for k, v in module.backend_workload_identities : k => v.client_id
  }
}

output "acr_login_server" {
  description = "ACR login server dùng để pull image"
  value       = module.acr.login_server
}

output "aks_id" {
  description = "ID của AKS cluster"
  value       = module.aks.aks_id
}

output "aks_oidc_issuer_url" {
  description = "OIDC issuer URL của AKS cluster"
  value       = module.aks.oidc_issuer_url
}

output "key_vault_uri" {
  description = "URI của Key Vault dùng để lưu secrets"
  value       = module.keyvault.kv_uri
}

output "app_resource_group_name" {
  description = "Resource group của môi trường ứng dụng"
  value       = azurerm_resource_group.app_rg.name
}

output "shared_resource_group_name" {
  description = "Resource group dùng chung"
  value       = azurerm_resource_group.shared_rg.name
}

output "keyvault_name" {
  value = module.keyvault.kv_name
}

output "tenant_id" {
  value = data.azurerm_client_config.current.tenant_id
}

output "backend_client_ids" {
  description = "Map of service name to its workload identity client ID"
  value = {
    for k, v in module.backend_workload_identities : k => v.client_id
  }
}