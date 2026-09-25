resource "azurerm_user_assigned_identity" "identity" {
  name                = var.identity_name
  resource_group_name = var.resource_group_name
  location            = var.location
}

# --- Cấu hình 1: Cho GitHub Actions (Hỗ trợ nhiều Subject) ---
resource "azurerm_federated_identity_credential" "github_oidc" {
  # Lặp qua danh sách oidc_subjects nếu KHÔNG phải workload identity
  for_each            = var.is_workload_identity ? toset([]) : toset(var.oidc_subjects)
  
  # Đặt tên credential (Thay thế ký tự đặc biệt như ':' và '/' để hợp lệ với Azure)
  name                = "${var.identity_name}-fed-${replace(each.value, "/[^a-zA-Z0-9-]/", "-")}"
  resource_group_name = var.resource_group_name
  audience            = var.oidc_audience
  issuer              = var.oidc_issuer
  parent_id           = azurerm_user_assigned_identity.identity.id
  
  # Truyền giá trị subject từ vòng lặp
  subject             = each.value
}

// Workload Identity (AKS)
resource "azurerm_federated_identity_credential" "aks_oidc" {
  count               = var.is_workload_identity ? 1 : 0
  name                = "${var.identity_name}-aks-cred"
  resource_group_name = var.resource_group_name
  # Audience chuẩn của K8s Workload Identity
  audience            = ["api://AzureADTokenExchange"]
  # Lấy OIDC Issuer từ cụm AKS
  issuer              = var.aks_oidc_issuer_url
  parent_id           = azurerm_user_assigned_identity.identity.id
  # Subject chuẩn nối namespace và service account
  subject             = "system:serviceaccount:${var.k8s_namespace}:${var.k8s_service_account_name}"
}