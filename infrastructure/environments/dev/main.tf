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
  sku                 = var.acr_sku
  admin_enabled       = var.acr_admin_enabled
}

module "networking" {
  source                    = "../../modules/networking"
  vnet_name                 = var.vnet_name
  resource_group_name       = azurerm_resource_group.app_rg.name # Đặt trong App RG
  location                  = azurerm_resource_group.app_rg.location
  vnet_address_space        = var.vnet_address_space
  aks_subnet_address_prefix = var.aks_subnet_address_prefix
  db_subnet_address_prefix  = var.db_subnet_address_prefix
  aks_subnet_name           = var.aks_subnet_name
  db_subnet_name            = var.db_subnet_name
  db_nsg_name               = var.db_nsg_name
}

module "aks" {
  source                    = "../../modules/aks"
  cluster_name              = var.aks_cluster_name
  dns_prefix                = var.aks_dns_prefix
  resource_group_name       = azurerm_resource_group.app_rg.name
  location                  = azurerm_resource_group.app_rg.location
  aks_subnet_id             = module.networking.aks_subnet_id
  oidc_issuer_enabled       = var.aks_oidc_issuer_enabled
  node_pool_name            = var.aks_node_pool_name
  node_vm_size              = var.aks_node_vm_size
  node_auto_scaling_enabled = var.aks_node_auto_scaling_enabled
  node_min_count            = var.aks_node_min_count
  node_max_count            = var.aks_node_max_count
  node_rotation_name        = var.aks_node_rotation_name
  network_plugin            = var.aks_network_plugin
  load_balancer_sku         = var.aks_load_balancer_sku
  service_cidr              = var.aks_service_cidr
  dns_service_ip            = var.aks_dns_service_ip
}

# Resolve GitHub owner and repository IDs at plan time for the federated identity subject.
locals {
  github_owner      = split("/", var.github_repository)[0]
  github_repository = split("/", var.github_repository)[1]
}

data "http" "github_user" {
  url = "https://api.github.com/users/${local.github_owner}"
}

data "http" "github_repo" {
  url = "https://api.github.com/repos/${var.github_repository}"
}

locals {
  owner_id = jsondecode(data.http.github_user.response_body).id
  repo_id  = jsondecode(data.http.github_repo.response_body).id

  oidc_dynamic_subject = "repo:${local.github_owner}@${local.owner_id}/${local.github_repository}@${local.repo_id}:ref:refs/heads/${var.github_ref}"
}

module "identity" {
  source              = "../../modules/identity"
  identity_name       = var.identity_name
  resource_group_name = azurerm_resource_group.shared_rg.name
  location            = azurerm_resource_group.shared_rg.location

  oidc_subject  = local.oidc_dynamic_subject
  oidc_audience = var.oidc_audience
  oidc_issuer   = var.oidc_issuer
}

# Cấp quyền AcrPull cho AKS Managed Identity để tự động kéo image từ ACR
resource "azurerm_role_assignment" "aks_acrpull" {
  principal_id                     = module.aks.kubelet_identity_object_id
  role_definition_name             = "AcrPull"
  scope                            = module.acr.acr_id
  skip_service_principal_aad_check = true
}

# Cấp quyền Contributor cho GitHub Actions Identity trên App RG
resource "azurerm_role_assignment" "ci_app_rg_contributor" {
  principal_id         = module.identity.principal_id
  role_definition_name = "Reader"
  scope                = azurerm_resource_group.app_rg.id
}

# Cấp quyền AcrPush cho GitHub Actions Identity (Giới hạn scope CHỈ TRÊN ACR)
resource "azurerm_role_assignment" "ci_acr_push" {
  principal_id         = module.identity.principal_id
  role_definition_name = "AcrPush"
  # Bạn đã suy đoán cực kỳ chính xác: truyền ID của ACR thay vì ID của Resource Group
  scope                = module.acr.acr_id 
}