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

variable "oidc_issuer_enabled" {
  description = "Enable the AKS OIDC issuer"
  type        = bool
}

variable "node_pool_name" {
  description = "Default AKS node pool name"
  type        = string
}

variable "node_vm_size" {
  description = "Default AKS node VM size"
  type        = string
}

variable "node_auto_scaling_enabled" {
  description = "Enable autoscaling for the default node pool"
  type        = bool
}

variable "node_min_count" {
  description = "Minimum node count"
  type        = number
}

variable "node_max_count" {
  description = "Maximum node count"
  type        = number
}

variable "node_rotation_name" {
  description = "Temporary node pool name used during rotation"
  type        = string
}

variable "network_plugin" {
  description = "AKS network plugin"
  type        = string
}

variable "load_balancer_sku" {
  description = "AKS load balancer SKU"
  type        = string
}

variable "service_cidr" {
  description = "Kubernetes service CIDR"
  type        = string
}

variable "dns_service_ip" {
  description = "Kubernetes DNS service IP"
  type        = string
}