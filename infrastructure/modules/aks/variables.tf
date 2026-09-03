variable "cluster_name" {
  description = "Tên của AKS cluster"
  type        = string
}

variable "dns_prefix" {
  description = "DNS prefix cho cụm AKS"
  type        = string
}

variable "resource_group_name" {
  description = "Tên Resource Group chứa AKS"
  type        = string
}

variable "location" {
  description = "Azure Region"
  type        = string
}

variable "aks_subnet_id" {
  description = "ID của Subnet dành cho AKS"
  type        = string
}