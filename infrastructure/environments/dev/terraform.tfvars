location       = "eastasia"
environment    = "dev"
app_rg_name    = "rg-tasktracker-dev"
shared_rg_name = "rg-tasktracker-shared"
acr_name       = "acrtasktrackerdev4459"

# Networking configs
vnet_name                 = "vnet-tasktracker-dev"
vnet_address_space        = ["10.0.0.0/16"]
aks_subnet_address_prefix = ["10.0.1.0/24"]
db_subnet_address_prefix  = ["10.0.2.0/24"]

# AKS configs
aks_cluster_name = "aks-tasktracker-dev"
aks_dns_prefix   = "aks-tasktracker-dev-dns"