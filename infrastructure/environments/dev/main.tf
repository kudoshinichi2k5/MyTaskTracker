# Resource Group cho môi trường Dev
resource "azurerm_resource_group" "app_rg" {
  name     = var.app_rg_name
  location = var.location
}

# Resource Group cho các dịch vụ dùng chung (Shared Services)
resource "azurerm_resource_group" "shared_rg" {
  name     = var.shared_rg_name
  location = var.location
}

# Gọi module ACR - Đặt vào Shared RG
module "acr" {
  source              = "../../modules/acr"
  name                = var.acr_name
  resource_group_name = azurerm_resource_group.shared_rg.name # Trỏ về shared_rg
  location            = azurerm_resource_group.shared_rg.location
}