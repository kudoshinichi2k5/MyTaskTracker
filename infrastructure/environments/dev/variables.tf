variable "location" {
  description = "Azure Region"
  type        = string
  default     = "eastasia"
}

variable "environment" {
  description = "Environment name"
  type        = string
  default     = "dev"
}

variable "app_rg_name" {
  description = "Tên Resource Group cho ứng dụng Dev"
  type        = string
}

variable "shared_rg_name" {
  description = "Tên Resource Group chứa các dịch vụ dùng chung (ACR, DNS...)"
  type        = string
}

variable "acr_name" {
  description = "Tên ACR (phải duy nhất toàn cầu)"
  type        = string
}

variable "vnet_name" { type = string }
variable "vnet_address_space" { type = list(string) }
variable "aks_subnet_address_prefix" { type = list(string) }
variable "db_subnet_address_prefix" { type = list(string) }

variable "aks_cluster_name" {
  description = "Tên của AKS Cluster trong môi trường dev"
  type        = string
}

variable "aks_dns_prefix" {
  description = "DNS prefix cho AKS dev"
  type        = string
}