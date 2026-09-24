output "generated_db_passwords" {
  value     = { for k, v in random_password.db_passwords : k => v.result }
  sensitive = true
}

output "workload_identity_client_ids" {
  description = "Danh sách Client ID của các backend service để điền vào Helm values.yaml"
  value       = {
    for k, v in module.backend_workload_identities : k => v.client_id
  }
}