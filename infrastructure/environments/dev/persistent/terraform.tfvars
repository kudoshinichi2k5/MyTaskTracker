location          = "eastasia"
environment       = "dev"
project_name      = "tasktracker"
acr_sku           = "Basic"
acr_admin_enabled = false

oidc_audience     = ["api://AzureADTokenExchange"]
oidc_issuer       = "https://token.actions.githubusercontent.com"
github_repository = "kudoshinichi2k5/MyTaskTracker"
github_ref        = "main"