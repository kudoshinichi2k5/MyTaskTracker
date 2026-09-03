variable "vnet_name" { type = string }
variable "resource_group_name" { type = string }
variable "location" { type = string }
variable "vnet_address_space" { type = list(string) }
variable "aks_subnet_address_prefix" { type = list(string) }
variable "db_subnet_address_prefix" { type = list(string) }