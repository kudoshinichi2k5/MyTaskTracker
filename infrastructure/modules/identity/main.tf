resource "azurerm_user_assigned_identity" "identity" {
  name                = var.identity_name
  resource_group_name = var.resource_group_name
  location            = var.location
}

// Github Action
resource "azurerm_federated_identity_credential" "github_oidc" {
  count               = var.is_workload_identity ? 0 : 1
  name                = "${var.identity_name}-fed-cred"
  resource_group_name = var.resource_group_name
  audience            = var.oidc_audience
  issuer              = var.oidc_issuer
  parent_id           = azurerm_user_assigned_identity.identity.id

  # Thay thế hardcode bằng biến
  subject = var.oidc_subject
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