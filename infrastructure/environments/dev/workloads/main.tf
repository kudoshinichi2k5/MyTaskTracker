data "azurerm_client_config" "current" {}

# ĐỌC OUTPUT TỪ PERSISTENT STATE
data "terraform_remote_state" "persistent" {
  backend = "azurerm"
  config = {
    resource_group_name  = "TaskTrackerRG"
    storage_account_name = "tfstate4459"
    container_name       = "tfstate"
    key                  = "persistent.dev.terraform.tfstate"

    use_oidc             = true
    use_azuread_auth     = true

    client_id            = var.arm_client_id
    tenant_id            = var.arm_tenant_id
    subscription_id      = var.arm_subscription_id
  }
}

locals {
  app_prefix    = "${var.project_name}-${var.environment}"
  vnet_name     = "vnet-${local.app_prefix}"
  aks_name      = "aks-${local.app_prefix}"
  dns_prefix    = "aks-${local.app_prefix}-dns"
  keyvault_name = "kv-${local.app_prefix}-999"

  # Trích xuất giá trị từ remote state
  app_rg_name        = data.terraform_remote_state.persistent.outputs.app_rg_name
  app_rg_location    = data.terraform_remote_state.persistent.outputs.app_rg_location
  shared_rg_name     = data.terraform_remote_state.persistent.outputs.shared_rg_name
  shared_rg_location = data.terraform_remote_state.persistent.outputs.shared_rg_location
  acr_id             = data.terraform_remote_state.persistent.outputs.acr_id
  tf_ci_principal_id = data.terraform_remote_state.persistent.outputs.tf_ci_principal_id
}

# 1. Networking (Workload)
module "networking" {
  source                    = "../../../modules/networking"
  vnet_name                 = local.vnet_name
  resource_group_name       = local.app_rg_name
  location                  = local.app_rg_location
  vnet_address_space        = var.vnet_address_space
  aks_subnet_address_prefix = var.aks_subnet_address_prefix
  db_subnet_address_prefix  = var.db_subnet_address_prefix
  aks_subnet_name           = var.aks_subnet_name
  db_subnet_name            = var.db_subnet_name
  db_nsg_name               = var.db_nsg_name
}

# 2. AKS (Workload)
module "aks" {
  source                    = "../../../modules/aks"
  cluster_name              = local.aks_name
  dns_prefix                = local.dns_prefix
  resource_group_name       = local.app_rg_name
  location                  = local.app_rg_location
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

resource "azurerm_role_assignment" "aks_acrpull" {
  principal_id                     = module.aks.kubelet_identity_object_id
  role_definition_name             = "AcrPull"
  scope                            = local.acr_id
  skip_service_principal_aad_check = true
}

# 3. Key Vault (Workload - Ephemeral)
module "keyvault" {
  source              = "../../../modules/keyvault"
  keyvault_name       = local.keyvault_name
  resource_group_name = local.shared_rg_name
  location            = local.shared_rg_location
}

# Workload Identities cho Backend Services
module "backend_workload_identities" {
  source                   = "../../../modules/identity"
  for_each                 = toset(var.backend_services_list)
  identity_name            = "id-${each.key}-${var.environment}"
  resource_group_name      = local.app_rg_name
  location                 = local.app_rg_location
  is_workload_identity     = true
  aks_oidc_issuer_url      = module.aks.oidc_issuer_url
  k8s_namespace            = var.kubernetes_namespace
  k8s_service_account_name = each.key
}

resource "azurerm_role_assignment" "backend_kv_reader" {
  for_each             = toset(var.backend_services_list)
  principal_id         = module.backend_workload_identities[each.key].principal_id
  role_definition_name = "Key Vault Secrets User"
  scope                = module.keyvault.kv_id
}

# Cấp quyền admin KeyVault tĩnh và động
resource "azurerm_role_assignment" "terraform_kv_admin_local" {
  count                = var.admin_object_id != "" ? 1 : 0
  principal_id         = var.admin_object_id
  role_definition_name = "Key Vault Administrator"
  scope                = module.keyvault.kv_id
}

resource "azurerm_role_assignment" "tf_ci_kv_admin_explicit" {
  principal_id         = local.tf_ci_principal_id
  role_definition_name = "Key Vault Administrator"
  scope                = module.keyvault.kv_id
}

# Sinh Password & K8s Secrets (Giữ nguyên như cũ, nhớ đổi `depends_on` cho Secret)
resource "random_password" "db_passwords" {
  for_each         = toset(var.backend_services_list)
  length           = 16
  special          = true
  override_special = "!#$%&*()-_=+[]{}<>:?"
}
resource "random_password" "mariadb_root" {
  length           = 20
  special          = true
  override_special = "!#%&*()-_=+[]{}<>:?"
}

resource "azurerm_key_vault_secret" "db_connection_strings" {
  for_each     = toset(var.backend_services_list)
  name         = "${each.key}-connection-string"
  value        = "Server=tracker-mariadb.${var.kubernetes_namespace}.svc.cluster.local;Port=3306;Database=tracker_${split("-", each.key)[0]};User=${replace(each.key, "-", "_")};Password=${random_password.db_passwords[each.key].result};"
  key_vault_id = module.keyvault.kv_id
  depends_on   = [azurerm_role_assignment.terraform_kv_admin_local, azurerm_role_assignment.tf_ci_kv_admin_explicit]
}

resource "azurerm_key_vault_secret" "root_pass" {
  name         = "mariadb-root-password"
  value        = random_password.mariadb_root.result
  key_vault_id = module.keyvault.kv_id
  depends_on   = [azurerm_role_assignment.terraform_kv_admin_local, azurerm_role_assignment.tf_ci_kv_admin_explicit]
}

resource "kubernetes_namespace_v1" "environment" {
  metadata { name = var.kubernetes_namespace }
}

resource "kubernetes_secret_v1" "mariadb_init_secret" {
  metadata {
    name      = "mariadb-init-secret"
    namespace = kubernetes_namespace_v1.environment.metadata[0].name
  }
  data = {
    "mariadb-root-password"    = random_password.mariadb_root.result
    "AUTH_DB_PASSWORD"         = random_password.db_passwords["auth-service"].result
    "TASK_DB_PASSWORD"         = random_password.db_passwords["task-service"].result
    "NOTIFICATION_DB_PASSWORD" = random_password.db_passwords["notification-service"].result
    "PROJECT_DB_PASSWORD"      = random_password.db_passwords["project-service"].result
    "COMMENT_DB_PASSWORD"      = random_password.db_passwords["comment-service"].result
  }
  depends_on = [module.aks]
}