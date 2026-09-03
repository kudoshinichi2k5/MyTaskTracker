output "aks_id" {
  value = azurerm_kubernetes_cluster.aks.id
}

# Output này cực kỳ quan trọng cho Task 6 (Cấp quyền kéo image từ ACR)
output "kubelet_identity_object_id" {
  value = azurerm_kubernetes_cluster.aks.kubelet_identity[0].object_id
}