output "generated_db_passwords" {
  value     = { for k, v in random_password.db_passwords : k => v.result }
  sensitive = true
}