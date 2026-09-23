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

variable "is_workload_identity" {
  description = "Nếu true, sẽ tạo Federated Credential cho Kubernetes Pod thay vì GitHub Actions"
  type        = bool
  default     = false
}

variable "aks_oidc_issuer_url" {
  description = "URL OIDC của AKS Cluster (bắt buộc nếu is_workload_identity = true)"
  type        = string
  default     = ""
}

variable "k8s_namespace" {
  description = "Namespace của Kubernetes chứa Pod (bắt buộc nếu is_workload_identity = true)"
  type        = string
  default     = ""
}

variable "k8s_service_account_name" {
  description = "Tên ServiceAccount của Pod (bắt buộc nếu is_workload_identity = true)"
  type        = string
  default     = ""
}

variable "oidc_subject" {
  description = "Chuỗi subject claim OIDC động được truyền từ môi trường"
  type        = string
  default     = "" # Thêm dòng này
}

variable "oidc_audience" {
  description = "OIDC audience accepted by Azure"
  type        = list(string)
  default     = [] # Thêm dòng này (dùng [] vì type là list)
}

variable "oidc_issuer" {
  description = "OIDC issuer URL"
  type        = string
  default     = "" # Thêm dòng này
}