variable "identity_name" {
  description = "Tên của User Assigned Identity"
  type        = string
}

variable "resource_group_name" {
  description = "Tên Resource Group chứa User Assigned Identity"
  type        = string
}

variable "location" {
  description = "Azure Region"
  type        = string
}

variable "oidc_subject" {
  description = "Chuỗi subject claim OIDC động được truyền từ môi trường"
  type        = string
}

variable "oidc_audience" {
  description = "OIDC audience accepted by Azure"
  type        = list(string)
}

variable "oidc_issuer" {
  description = "OIDC issuer URL"
  type        = string
}