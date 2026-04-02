output "id" {
  value = azurerm_postgresql_flexible_server.this.id
}

output "fqdn" {
  value = azurerm_postgresql_flexible_server.this.fqdn
}

output "database_name" {
  value = azurerm_postgresql_flexible_server_database.this.name
}

output "administrator_login" {
  value = azurerm_postgresql_flexible_server.this.administrator_login
}

output "administrator_password" {
  value     = random_password.administrator.result
  sensitive = true
}

output "connection_string" {
  value     = "Host=${azurerm_postgresql_flexible_server.this.fqdn};Port=5432;Database=${azurerm_postgresql_flexible_server_database.this.name};Username=${azurerm_postgresql_flexible_server.this.administrator_login};Password=${random_password.administrator.result};Ssl Mode=Require;Trust Server Certificate=false"
  sensitive = true
}
