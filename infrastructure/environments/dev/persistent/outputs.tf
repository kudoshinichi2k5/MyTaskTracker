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

output "dns_zone_name_servers" {
  description = "Danh sách 4 NameServers để cấu hình DNS Delegation trên Registrar"
  value       = azurerm_dns_zone.main.name_servers
}

output "dns_zone_name" {
  description = "Tên DNS Zone"
  value       = azurerm_dns_zone.main.name
}

output "ingress_public_ip" {
  description = "Địa chỉ IPv4 tĩnh của Ingress"
  value       = azurerm_public_ip.ingress_ip.ip_address
}

output "ingress_public_ip_id" {
  description = "Resource ID của Static Public IP"
  value       = azurerm_public_ip.ingress_ip.id
}

output "dns_zone_id" {
  description = "Resource ID của DNS Zone để giới hạn Role Assignment"
  value       = azurerm_dns_zone.main.id
}
output "subscription_id" {
  value = data.azurerm_client_config.current.subscription_id
}