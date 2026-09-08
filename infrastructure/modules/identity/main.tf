resource "azurerm_user_assigned_identity" "github_ci" {
  name                = var.identity_name
  resource_group_name = var.resource_group_name
  location            = var.location
}

resource "azurerm_federated_identity_credential" "github_oidc" {
  name                = "${var.identity_name}-fed-cred"
  resource_group_name = var.resource_group_name
  audience            = ["api://AzureADTokenExchange"]
  issuer              = "https://token.actions.githubusercontent.com"
  parent_id           = azurerm_user_assigned_identity.github_ci.id
  
  # Thay thế hardcode bằng biến
  subject             = var.oidc_subject
}