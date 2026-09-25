# Bổ sung Provider Kubernetes vào đầu file (cần cấu hình kết nối tới AKS)
provider "kubernetes" {
  host                   = module.aks.host
  client_certificate     = base64decode(module.aks.client_certificate)
  client_key             = base64decode(module.aks.client_key)
  cluster_ca_certificate = base64decode(module.aks.cluster_ca_certificate)
}

# Cấp quyền cho chính tài khoản Terraform (của bạn) được ghi Secret vào Key Vault
# (Mặc định khi tạo Key Vault với RBAC, người tạo không tự có quyền Data Plane, phải tự cấp quyền)
data "azurerm_client_config" "current" {}

# Tự động sinh tên chuẩn cho toàn bộ project
locals {
  # Tiền tố chung (vd: tasktracker-dev)
  app_prefix = "${var.project_name}-${var.environment}"
  # Tiền tố cho shared (vd: tasktracker-shared)
  shared_prefix = "${var.project_name}-shared"

  app_rg_name    = "rg-${local.app_prefix}"
  shared_rg_name = "rg-${local.shared_prefix}"

  # ACR chỉ nhận chữ thường và số
  acr_name = "acr${var.project_name}${var.environment}4459"

  vnet_name     = "vnet-${local.app_prefix}"
  aks_name      = "aks-${local.app_prefix}"
  dns_prefix    = "aks-${local.app_prefix}-dns"
  identity_name = "id-github-actions-${var.environment}"
  keyvault_name = "kv-${local.app_prefix}-999"
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
  request_headers = {
    Authorization = "Bearer ${var.github_token}"
  }
}

data "http" "github_repo" {
  url = "https://api.github.com/repos/${var.github_repository}"
  request_headers = {
    Authorization = "Bearer ${var.github_token}"
  }
}

locals {
  owner_id = jsondecode(data.http.github_user.response_body).id
  repo_id  = jsondecode(data.http.github_repo.response_body).id

  # Vé cho nhánh main
  oidc_subject_main = "repo:${local.github_owner}@${local.owner_id}/${local.github_repository}@${local.repo_id}:ref:refs/heads/${var.github_ref}"
  
  # Vé cho Pull Request (THÊM MỚI)
  oidc_subject_pr   = "repo:${local.github_owner}@${local.owner_id}/${local.github_repository}@${local.repo_id}:pull_request"
}

module "identity" {
  source              = "../../modules/identity"
  identity_name       = local.identity_name
  resource_group_name = azurerm_resource_group.shared_rg.name
  location            = azurerm_resource_group.shared_rg.location

  oidc_subjects        = [local.oidc_subject_main, local.oidc_subject_pr]
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

  is_workload_identity = true
  aks_oidc_issuer_url  = module.aks.oidc_issuer_url
  k8s_namespace        = var.kubernetes_namespace

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

resource "azurerm_role_assignment" "terraform_kv_admin" {
  principal_id         = data.azurerm_client_config.current.object_id
  role_definition_name = "Key Vault Administrator"
  scope                = module.keyvault.kv_id
}

# Tạo Connection String và lưu vào Azure Key Vault
resource "azurerm_key_vault_secret" "db_connection_strings" {
  for_each = toset(local.backend_services)
  # Sử dụng chính tên service (vd: auth-service-connection-string)
  # KeyVault không phân biệt hoa thường và chấp nhận dấu gạch ngang
  name = "${each.key}-connection-string"

  # Cấu trúc Connection String (MariaDB)
  value = "Server=tracker-mariadb.${var.kubernetes_namespace}.svc.cluster.local;Port=3306;Database=tracker_${split("-", each.key)[0]};User=${replace(each.key, "-", "_")};Password=${random_password.db_passwords[each.key].result};"

  key_vault_id = module.keyvault.kv_id

  depends_on = [azurerm_role_assignment.terraform_kv_admin]
}

# Sinh Root Password cho MariaDB
resource "random_password" "mariadb_root" {
  length           = 20
  special          = true
  override_special = "!#%&*()-_=+[]{}<>:?"
}

# Lưu Root Pass vào Key Vault
resource "azurerm_key_vault_secret" "root_pass" {
  name         = "mariadb-root-password"
  value        = random_password.mariadb_root.result
  key_vault_id = module.keyvault.kv_id
  depends_on   = [azurerm_role_assignment.terraform_kv_admin]
}

# Tạo namespace trước các resource Kubernetes mà Terraform quản lý.
resource "kubernetes_namespace_v1" "environment" {
  metadata {
    name = var.kubernetes_namespace
  }
}

# Tạo Kubernetes Secret cho MariaDB.
resource "kubernetes_secret_v1" "mariadb_init_secret" {
  metadata {
    name      = "mariadb-init-secret"
    namespace = kubernetes_namespace_v1.environment.metadata[0].name
  }

  data = {
    # Truyền Root Pass cho Bitnami
    "mariadb-root-password" = random_password.mariadb_root.result
    # Truyền 5 pass của 5 backend cho Init Script
    "AUTH_DB_PASSWORD"         = random_password.db_passwords["auth-service"].result
    "TASK_DB_PASSWORD"         = random_password.db_passwords["task-service"].result
    "NOTIFICATION_DB_PASSWORD" = random_password.db_passwords["notification-service"].result
    "PROJECT_DB_PASSWORD"      = random_password.db_passwords["project-service"].result
    "COMMENT_DB_PASSWORD"      = random_password.db_passwords["comment-service"].result
  }

  # Đảm bảo AKS được tạo trước khi thả Secret vào
  depends_on = [module.aks]
}

# Tạo Identity riêng cho Terraform CI
module "terraform_ci_identity" {
  source              = "../../modules/identity"
  identity_name       = "id-terraform-ci-${var.environment}"
  resource_group_name = azurerm_resource_group.shared_rg.name
  location            = azurerm_resource_group.shared_rg.location

  # Cấu hình OIDC cho GitHub Actions
  is_workload_identity = false
  oidc_subjects        = [local.oidc_subject_main, local.oidc_subject_pr]
  oidc_audience        = var.oidc_audience
  oidc_issuer          = var.oidc_issuer
}

# Gán quyền Contributor trên 2 Resource Group
resource "azurerm_role_assignment" "tf_ci_contributor_app" {
  principal_id         = module.terraform_ci_identity.principal_id
  role_definition_name = "Contributor"
  scope                = azurerm_resource_group.app_rg.id
}

resource "azurerm_role_assignment" "tf_ci_contributor_shared" {
  principal_id         = module.terraform_ci_identity.principal_id
  role_definition_name = "Contributor"
  scope                = azurerm_resource_group.shared_rg.id
}

# Gán quyền User Access Administrator để Terraform có thể cấp role cho các Identity khác
resource "azurerm_role_assignment" "tf_ci_rbac_admin_app" {
  principal_id         = module.terraform_ci_identity.principal_id
  role_definition_name = "User Access Administrator"
  scope                = azurerm_resource_group.app_rg.id
}

resource "azurerm_role_assignment" "tf_ci_rbac_admin_shared" {
  principal_id         = module.terraform_ci_identity.principal_id
  role_definition_name = "User Access Administrator"
  scope                = azurerm_resource_group.shared_rg.id
}

# Gán quyền Storage Blob Data Contributor vào storage account chứa tfstate
# (Lấy hardcode theo context bạn cung cấp ở backend.tf)
data "azurerm_storage_account" "tfstate" {
  name                = "tfstate4459"
  resource_group_name = "TaskTrackerRG"
}

resource "azurerm_role_assignment" "tf_ci_state_access" {
  principal_id         = module.terraform_ci_identity.principal_id
  role_definition_name = "Storage Blob Data Contributor"
  scope                = data.azurerm_storage_account.tfstate.id
}

# Gán quyền Reader (Control Plane) vào storage account chứa tfstate để terraform init có thể đọc metadata
resource "azurerm_role_assignment" "tf_ci_state_reader" {
  principal_id         = module.terraform_ci_identity.principal_id
  role_definition_name = "Reader"
  scope                = data.azurerm_storage_account.tfstate.id
}