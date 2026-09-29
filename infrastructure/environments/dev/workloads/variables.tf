variable "environment" {
  description = "Environment name (dev, staging, prod)"
  type        = string
}

variable "project_name" {
  description = "Tên gốc của dự án dùng để sinh tên cho các tài nguyên"
  type        = string
}

# --- CẤU HÌNH NETWORKING ---
variable "vnet_address_space" { type = list(string) }
variable "aks_subnet_address_prefix" { type = list(string) }
variable "db_subnet_address_prefix" { type = list(string) }
variable "aks_subnet_name" { type = string }
variable "db_subnet_name" { type = string }
variable "db_nsg_name" { type = string }

# --- CẤU HÌNH AKS ---
variable "aks_oidc_issuer_enabled" { type = bool }
variable "aks_node_pool_name" { type = string }
variable "aks_node_vm_size" { type = string }
variable "aks_node_auto_scaling_enabled" { type = bool }
variable "aks_node_min_count" { type = number }
variable "aks_node_max_count" { type = number }
variable "aks_node_rotation_name" { type = string }
variable "aks_network_plugin" { type = string }
variable "aks_load_balancer_sku" { type = string }
variable "aks_service_cidr" { type = string }
variable "aks_dns_service_ip" { type = string }

# --- CẤU HÌNH KUBERNETES WORKLOADS ---
variable "kubernetes_namespace" {
  description = "Namespace dùng cho các resource Kubernetes của môi trường"
  type        = string
  default     = "dev"
}

variable "backend_services_list" {
  description = "Danh sách các backend services cần tạo Workload Identity"
  type        = list(string)
  default = [
    "auth-service",
    "task-service",
    "notification-service",
    "project-service",
    "comment-service"
  ]

  validation {
    condition = length(setsubtract([
      "auth-service",
      "task-service",
      "notification-service",
      "project-service",
      "comment-service"
    ], var.backend_services_list)) == 0
    error_message = "backend_services_list phải chứa đủ năm backend service được MariaDB init script sử dụng."
  }
}

# --- QUẢN TRỊ VIÊN ---
variable "admin_object_id" {
  description = "Object ID của tài khoản cá nhân (dùng để bootstrap/kiểm tra ở local nếu cần)"
  type        = string
  default     = ""
}

variable "arm_client_id" {
  description = "Client ID của Service Principal (Bot Terraform CI)"
  type        = string
  default     = ""
}

variable "arm_tenant_id" {
  description = "Tenant ID của Azure Active Directory"
  type        = string
  default     = ""
}

variable "arm_subscription_id" {
  description = "Subscription ID của Azure"
  type        = string
  default     = ""
}