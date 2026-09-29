variable "location" {
  description = "Azure Region"
  type        = string
}

variable "environment" {
  description = "Environment name (dev, staging, prod)"
  type        = string
}

variable "project_name" {
  description = "Tên gốc của dự án dùng để sinh tên cho các tài nguyên"
  type        = string
}

# --- CẤU HÌNH ACR ---
variable "acr_sku" {
  description = "SKU của Azure Container Registry"
  type        = string
}

variable "acr_admin_enabled" {
  description = "Bật/Tắt Admin account cho ACR"
  type        = bool
}

# --- CẤU HÌNH OIDC & GITHUB ---
variable "oidc_audience" {
  description = "OIDC audience accepted by Azure"
  type        = list(string)
}

variable "oidc_issuer" {
  description = "OIDC issuer URL"
  type        = string
}

variable "github_repository" {
  description = "Tên repo GitHub (vd: kudoshinichi2k5/MyTaskTracker)"
  type        = string
}

variable "github_ref" {
  description = "Nhánh GitHub dùng cho luồng Apply"
  type        = string
}

variable "github_token" {
  description = "GitHub Token để gọi API tránh rate limit"
  type        = string
  sensitive   = true
  default     = ""
}