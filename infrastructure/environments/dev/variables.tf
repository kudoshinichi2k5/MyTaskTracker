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