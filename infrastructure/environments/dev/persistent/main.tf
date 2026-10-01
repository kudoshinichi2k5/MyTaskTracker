data "azurerm_client_config" "current" {}

locals {
  app_prefix    = "${var.project_name}-${var.environment}"
  shared_prefix = "${var.project_name}-shared"

  app_rg_name    = "rg-${local.app_prefix}"
  shared_rg_name = "rg-${local.shared_prefix}"
  acr_name       = "acr${var.project_name}${var.environment}4459"
  identity_name  = "id-github-actions-${var.environment}"
}

# TẠO CẢ 2 RESOURCE GROUP Ở ĐÂY (Để cấu hình phân quyền ngay lập tức)
resource "azurerm_resource_group" "app_rg" {
  name     = local.app_rg_name
  location = var.location
}

resource "azurerm_resource_group" "shared_rg" {
  name     = local.shared_rg_name
  location = var.location
}

# 1. ACR (Lõi)
module "acr" {
  source              = "../../../modules/acr"
  name                = local.acr_name
  resource_group_name = azurerm_resource_group.shared_rg.name
  location            = azurerm_resource_group.shared_rg.location
  sku                 = var.acr_sku
  admin_enabled       = var.acr_admin_enabled
}

# 2. Backup Storage (Lõi)
resource "azurerm_storage_account" "backup" {
  name                     = "sabackup${var.project_name}${var.environment}"
  resource_group_name      = azurerm_resource_group.shared_rg.name
  location                 = azurerm_resource_group.shared_rg.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
}
resource "azurerm_storage_container" "mariadb_backup" {
  name                  = "mariadb-backups"
  storage_account_name  = azurerm_storage_account.backup.name
  container_access_type = "private"
}

# Quản lý vòng đời lưu trữ (Lifecycle Management)
resource "azurerm_storage_management_policy" "backup_lifecycle" {
  storage_account_id = azurerm_storage_account.backup.id

  rule {
    name    = "mariadb-backup-retention"
    enabled = true
    filters {
      # Chỉ áp dụng cho các file nằm trong container mariadb-backups
      prefix_match = ["mariadb-backups/"]
      blob_types   = ["blockBlob"]
    }
    actions {
      base_blob {
        # Sau 7 ngày kể từ lúc upload -> Chuyển sang Tier Cool (Giảm tiền lưu trữ)
        tier_to_cool_after_days_since_modification_greater_than    = 7
        
        # Sau 30 ngày kể từ lúc upload -> Xóa luôn
        delete_after_days_since_modification_greater_than          = 30
      }
    }
  }
}

# 3. GitHub Data & OIDC Subjects
locals {
  github_owner      = split("/", var.github_repository)[0]
  github_repository = split("/", var.github_repository)[1]
}

data "http" "github_user" {
  url             = "https://api.github.com/users/${local.github_owner}"
  request_headers = var.github_token != "" ? { Authorization = "Bearer ${var.github_token}" } : {}
}

data "http" "github_repo" {
  url             = "https://api.github.com/repos/${var.github_repository}"
  request_headers = var.github_token != "" ? { Authorization = "Bearer ${var.github_token}" } : {}
}

locals {
  owner_id = jsondecode(data.http.github_user.response_body).id
  repo_id  = jsondecode(data.http.github_repo.response_body).id

  oidc_subject_main = "repo:${local.github_owner}@${local.owner_id}/${local.github_repository}@${local.repo_id}:ref:refs/heads/${var.github_ref}"
  oidc_subject_pr   = "repo:${local.github_owner}@${local.owner_id}/${local.github_repository}@${local.repo_id}:pull_request"
}

# 4. Identity cho APP CI (Week 6)
module "identity" {
  source               = "../../../modules/identity"
  identity_name        = local.identity_name
  resource_group_name  = azurerm_resource_group.shared_rg.name
  location             = azurerm_resource_group.shared_rg.location
  is_workload_identity = false
  oidc_subjects        = [local.oidc_subject_main, local.oidc_subject_pr]
  oidc_audience        = var.oidc_audience
  oidc_issuer          = var.oidc_issuer
}

resource "azurerm_role_assignment" "ci_acr_push" {
  principal_id         = module.identity.principal_id
  role_definition_name = "AcrPush"
  scope                = module.acr.acr_id
}

# 5. Identity cho TERRAFORM CI (Quản lý Workloads)
module "terraform_ci_identity" {
  source               = "../../../modules/identity"
  identity_name        = "id-terraform-ci-${var.environment}"
  resource_group_name  = azurerm_resource_group.shared_rg.name
  location             = azurerm_resource_group.shared_rg.location
  is_workload_identity = false
  oidc_subjects        = [local.oidc_subject_main, local.oidc_subject_pr]
  oidc_audience        = var.oidc_audience
  oidc_issuer          = var.oidc_issuer
}

# Gán quyền Quản trị (Contributor + Admin) trên CẢ 2 RG cho Bot TF
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

data "azurerm_storage_account" "tfstate" {
  name                = "tfstate4459"
  resource_group_name = "TaskTrackerRG"
}
resource "azurerm_role_assignment" "tf_ci_state_access" {
  principal_id         = module.terraform_ci_identity.principal_id
  role_definition_name = "Storage Blob Data Contributor"
  scope                = data.azurerm_storage_account.tfstate.id
}
resource "azurerm_role_assignment" "tf_ci_state_reader" {
  principal_id         = module.terraform_ci_identity.principal_id
  role_definition_name = "Reader"
  scope                = data.azurerm_storage_account.tfstate.id
}

# --- WEEK 9: ROUTING & INGRESS ---

# 1. Tạo Azure DNS Zone (Nơi quản lý các bản ghi DNS tự động sau này)
resource "azurerm_dns_zone" "main" {
  name                = var.domain_name
  resource_group_name = azurerm_resource_group.shared_rg.name
  
  # DNS Zone là dịch vụ toàn cầu (Global), nên không cần location cụ thể, 
  # nhưng Azure bắt buộc nó nằm trong một RG.
}

# 2. Tạo Static Public IP (Standard SKU)
resource "azurerm_public_ip" "ingress_ip" {
  name                = "pip-ingress-${var.project_name}-${var.environment}"
  resource_group_name = azurerm_resource_group.shared_rg.name
  location            = azurerm_resource_group.shared_rg.location
  
  # Bắt buộc phải là Static để IP không đổi khi deploy lại
  allocation_method   = "Static"
  
  # Bắt buộc là Standard để tương thích với AKS Standard Load Balancer
  sku                 = "Standard"
  
  # Tùy chọn (Nice to have): Gắn domain name label cho IP này (VD: dev-ingress.eastasia.cloudapp.azure.com)
  domain_name_label   = "${var.project_name}-ingress-${var.environment}"
}