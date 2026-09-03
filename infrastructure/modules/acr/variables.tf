variable "name" {
  description = "Tên của Azure Container Registry"
  type        = string
}

variable "resource_group_name" {
  description = "Tên Resource Group chứa ACR"
  type        = string
}

variable "location" {
  description = "Azure Region"
  type        = string
}

variable "sku" {
  description = "SKU của ACR (Basic, Standard, Premium)"
  type        = string
  default     = "Basic"
}

variable "admin_enabled" {
  description = "Bật admin user cho ACR"
  type        = bool
  default     = true
}