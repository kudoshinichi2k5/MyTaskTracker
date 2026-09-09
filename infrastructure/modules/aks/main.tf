resource "azurerm_kubernetes_cluster" "aks" {
  name                = var.cluster_name
  location            = var.location
  resource_group_name = var.resource_group_name
  dns_prefix          = var.dns_prefix

  # Thêm dòng này để đồng bộ với state thực tế của Azure
  oidc_issuer_enabled = var.oidc_issuer_enabled

  default_node_pool {
    name                        = var.node_pool_name
    vm_size                     = var.node_vm_size
    enable_auto_scaling         = var.node_auto_scaling_enabled
    min_count                   = var.node_min_count
    max_count                   = var.node_max_count
    vnet_subnet_id              = var.aks_subnet_id
    temporary_name_for_rotation = var.node_rotation_name
  }

  identity {
    type = "SystemAssigned"
  }

  network_profile {
    network_plugin    = var.network_plugin
    load_balancer_sku = var.load_balancer_sku
    service_cidr      = var.service_cidr
    dns_service_ip    = var.dns_service_ip
  }
}