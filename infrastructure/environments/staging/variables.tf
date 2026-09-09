variable "location" { type = string }
variable "environment" { type = string }
variable "app_rg_name" { type = string }
variable "shared_rg_name" { type = string }
variable "acr_name" { type = string }
variable "acr_sku" { type = string }
variable "acr_admin_enabled" { type = bool }

variable "vnet_name" { type = string }
variable "vnet_address_space" { type = list(string) }
variable "aks_subnet_address_prefix" { type = list(string) }
variable "db_subnet_address_prefix" { type = list(string) }
variable "aks_subnet_name" { type = string }
variable "db_subnet_name" { type = string }
variable "db_nsg_name" { type = string }

variable "aks_cluster_name" { type = string }
variable "aks_dns_prefix" { type = string }
variable "identity_name" { type = string }
variable "oidc_audience" { type = list(string) }
variable "oidc_issuer" { type = string }
variable "github_repository" { type = string }
variable "github_ref" { type = string }

variable "aks_oidc_issuer_enabled" { type = bool }
variable "aks_node_pool_name" { type = string }
variable "aks_node_vm_size" { type = string }
variable "aks_node_auto_scaling_enabled" { type = bool }
variable "aks_node_min_count" { type = number }
variable "aks_node_max_count" { type = number }
variable "aks_node_rotation_name" { type = string }
variable "aks_network_plugin" { type = string }
variable "aks_load_balancer_sku" { type = string }
variable "aks_service_cidr" { type = string }
variable "aks_dns_service_ip" { type = string }
