data "azurerm_client_config" "current" {}

resource "azurerm_key_vault" "kv" {
  name                       = var.keyvault_name
  location                   = var.location
  resource_group_name        = var.resource_group_name
  tenant_id                  = data.azurerm_client_config.current.tenant_id
  sku_name                   = "standard"
  
  # Yêu cầu bắt buộc của Enterprise
  soft_delete_retention_days = 7
  purge_protection_enabled   = false

  # Bật RBAC thay vì Access Policies cũ
  enable_rbac_authorization  = true
}

