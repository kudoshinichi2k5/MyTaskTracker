variable "location" { type = string }
variable "resource_group_name" { type = string }
variable "environment" { type = string }
variable "db_nsg_name" { type = string }
variable "aks_subnet_address_prefix" { type = list(string) }
variable "db_subnet_id" { type = string }
