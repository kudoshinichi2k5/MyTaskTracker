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