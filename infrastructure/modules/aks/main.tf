resource "azurerm_kubernetes_cluster" "aks" {
  name                = var.cluster_name
  location            = var.location
  resource_group_name = var.resource_group_name
  dns_prefix          = var.dns_prefix

  default_node_pool {
    name                = "default"
    vm_size             = "standard_b2pls_v2"
    enable_auto_scaling = true
    min_count           = 1
    max_count           = 2
    vnet_subnet_id      = var.aks_subnet_id
  }

  identity {
    type = "SystemAssigned"
  }

  network_profile {
    network_plugin    = "azure"
    load_balancer_sku = "standard"
    service_cidr      = "192.168.0.0/16"
    dns_service_ip    = "192.168.0.10"
  }
}