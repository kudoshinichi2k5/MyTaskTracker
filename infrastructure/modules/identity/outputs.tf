output "client_id" {
  value = azurerm_user_assigned_identity.github_ci.client_id
}

output "principal_id" {
  value = azurerm_user_assigned_identity.github_ci.principal_id
}

output "identity_id" {
  value = azurerm_user_assigned_identity.github_ci.id
}