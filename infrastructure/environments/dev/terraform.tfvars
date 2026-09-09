location          = "eastasia"
environment       = "dev"
app_rg_name       = "rg-tasktracker-dev"
shared_rg_name    = "rg-tasktracker-shared"
acr_name          = "acrtasktrackerdev4459"
acr_sku           = "Basic"
acr_admin_enabled = false

# Networking configs
vnet_name                 = "vnet-tasktracker-dev"
vnet_address_space        = ["10.0.0.0/16"]
aks_subnet_address_prefix = ["10.0.1.0/24"]
db_subnet_address_prefix  = ["10.0.2.0/24"]
aks_subnet_name           = "aks-subnet"
db_subnet_name            = "db-subnet"
db_nsg_name               = "db-nsg"

# AKS configs
aks_cluster_name  = "aks-tasktracker-dev"
aks_dns_prefix    = "aks-tasktracker-dev-dns"
identity_name     = "id-github-actions-dev"
oidc_audience     = ["api://AzureADTokenExchange"]
oidc_issuer       = "https://token.actions.githubusercontent.com"
github_repository = "kudoshinichi2k5/MyTaskTracker"
github_ref        = "main"

aks_oidc_issuer_enabled       = true
aks_node_pool_name            = "default"
aks_node_vm_size              = "standard_b2ps_v2"
aks_node_auto_scaling_enabled = true
aks_node_min_count            = 1
aks_node_max_count            = 2
aks_node_rotation_name        = "tmppool"
aks_network_plugin            = "azure"
aks_load_balancer_sku         = "standard"
aks_service_cidr              = "192.168.0.0/16"
aks_dns_service_ip            = "192.168.0.10"