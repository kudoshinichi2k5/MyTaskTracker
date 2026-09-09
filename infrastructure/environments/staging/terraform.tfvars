location          = "eastasia"
environment       = "staging"
app_rg_name       = "rg-tasktracker-staging"
shared_rg_name    = "rg-tasktracker-shared"
acr_name          = "acrtasktrackerstaging4459"
acr_sku           = "Basic"
acr_admin_enabled = false

vnet_name                 = "vnet-tasktracker-staging"
vnet_address_space        = ["10.10.0.0/16"]
aks_subnet_address_prefix = ["10.10.1.0/24"]
db_subnet_address_prefix  = ["10.10.2.0/24"]
aks_subnet_name           = "aks-subnet"
db_subnet_name            = "db-subnet"
db_nsg_name               = "db-nsg"

aks_cluster_name  = "aks-tasktracker-staging"
aks_dns_prefix    = "aks-tasktracker-staging-dns"
identity_name     = "id-github-actions-staging"
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
aks_service_cidr              = "192.168.10.0/16"
aks_dns_service_ip            = "192.168.10.10"
