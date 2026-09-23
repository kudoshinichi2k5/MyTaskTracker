# Tự động sinh tên chuẩn cho toàn bộ project
locals {
  # Tiền tố chung (vd: tasktracker-dev)
  app_prefix    = "${var.project_name}-${var.environment}"
  # Tiền tố cho shared (vd: tasktracker-shared)
  shared_prefix = "${var.project_name}-shared"

  app_rg_name    = "rg-${local.app_prefix}"
  shared_rg_name = "rg-${local.shared_prefix}"
  
  # ACR chỉ nhận chữ thường và số
  acr_name       = "acr${var.project_name}${var.environment}4459"
  
  vnet_name      = "vnet-${local.app_prefix}"
  aks_name       = "aks-${local.app_prefix}"
  dns_prefix     = "aks-${local.app_prefix}-dns"
  identity_name  = "id-github-actions-${var.environment}"
  keyvault_name  = "kv-${local.app_prefix}-888"
}

# Resource Group cho môi trường Dev
resource "azurerm_resource_group" "app_rg" {
  name     = local.app_rg_name
  location = var.location
}

# Resource Group cho các dịch vụ dùng chung (Shared Services)
resource "azurerm_resource_group" "shared_rg" {
  name     = local.shared_rg_name
  location = var.location
}

# Gọi module ACR - Đặt vào Shared RG
module "acr" {
  source              = "../../modules/acr"
  name                = local.acr_name
  resource_group_name = azurerm_resource_group.shared_rg.name 
  location            = azurerm_resource_group.shared_rg.location
  sku                 = var.acr_sku
  admin_enabled       = var.acr_admin_enabled
}

module "networking" {
  source                    = "../../modules/networking"
  vnet_name                 = local.vnet_name
  resource_group_name       = azurerm_resource_group.app_rg.name 
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
  cluster_name              = local.aks_name
  dns_prefix                = local.dns_prefix
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
  identity_name       = local.identity_name
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
  scope                = module.acr.acr_id 
}

# Gọi Module Key Vault
module "keyvault" {
  source              = "../../modules/keyvault"
  keyvault_name       = local.keyvault_name 
  resource_group_name = azurerm_resource_group.shared_rg.name
  location            = azurerm_resource_group.shared_rg.location
}

locals {
  backend_services = var.backend_services_list
}

# Tạo Workload Identity cho TẤT CẢ backend services
module "backend_workload_identities" {
  source   = "../../modules/identity"
  for_each = toset(local.backend_services)

  identity_name       = "id-${each.key}-${var.environment}"
  resource_group_name = azurerm_resource_group.app_rg.name
  location            = azurerm_resource_group.app_rg.location
  
  is_workload_identity     = true
  aks_oidc_issuer_url      = module.aks.oidc_issuer_url
  k8s_namespace            = var.environment
  
  k8s_service_account_name = each.key 
}

# Cấp quyền ĐỌC Secret cho từng Pod Identity trên Key Vault đó
resource "azurerm_role_assignment" "backend_kv_reader" {
  for_each             = toset(local.backend_services)
  principal_id         = module.backend_workload_identities[each.key].principal_id
  role_definition_name = "Key Vault Secrets User"
  scope                = module.keyvault.kv_id
}

# Sinh Mật khẩu ngẫu nhiên cho từng dịch vụ Backend
resource "random_password" "db_passwords" {
  for_each         = toset(local.backend_services)
  length           = 16
  special          = true
  override_special = "!#$%&*()-_=+[]{}<>:?"
}

# Cấp quyền cho chính tài khoản Terraform (của bạn) được ghi Secret vào Key Vault
# (Mặc định khi tạo Key Vault với RBAC, người tạo không tự có quyền Data Plane, phải tự cấp quyền)
data "azurerm_client_config" "current" {}

resource "azurerm_role_assignment" "terraform_kv_admin" {
  principal_id         = data.azurerm_client_config.current.object_id
  role_definition_name = "Key Vault Administrator"
  scope                = module.keyvault.kv_id
}

# Tạo Connection String và lưu vào Azure Key Vault
resource "azurerm_key_vault_secret" "db_connection_strings" {
  for_each     = toset(local.backend_services)
  # Đặt tên Secret chuẩn hóa, VD: AuthDbConnectionString
  name         = "${title(split("-", each.key)[0])}DbConnectionString" 
  
  # Cấu trúc Connection String (MariaDB)
  # Tên user và database sẽ được cắt từ tên service (auth-service -> auth_service / tracker_auth)
  value        = "Server=tracker-mariadb.dev.svc.cluster.local;Port=3306;Database=tracker_${split("-", each.key)[0]};User=${replace(each.key, "-", "_")};Password=${random_password.db_passwords[each.key].result};"
  
  key_vault_id = module.keyvault.kv_id

  # Bắt buộc phải đợi cấp quyền Admin xong mới được phép ghi Secret
  depends_on   = [azurerm_role_assignment.terraform_kv_admin]
}