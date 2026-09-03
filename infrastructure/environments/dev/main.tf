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

module "networking" {
  source                    = "../../modules/networking"
  vnet_name                 = var.vnet_name
  resource_group_name       = azurerm_resource_group.app_rg.name # Đặt trong App RG
  location                  = azurerm_resource_group.app_rg.location
  vnet_address_space        = var.vnet_address_space
  aks_subnet_address_prefix = var.aks_subnet_address_prefix
  db_subnet_address_prefix  = var.db_subnet_address_prefix
}

module "aks" {
  source              = "../../modules/aks"
  cluster_name        = var.aks_cluster_name
  dns_prefix          = var.aks_dns_prefix
  resource_group_name = azurerm_resource_group.app_rg.name
  location            = azurerm_resource_group.app_rg.location
  aks_subnet_id       = module.networking.aks_subnet_id
}

# Cấp quyền AcrPull cho AKS Managed Identity để tự động kéo image từ ACR
resource "azurerm_role_assignment" "aks_acrpull" {
  principal_id                     = module.aks.kubelet_identity_object_id
  role_definition_name             = "AcrPull"
  scope                            = module.acr.acr_id
  skip_service_principal_aad_check = true
}