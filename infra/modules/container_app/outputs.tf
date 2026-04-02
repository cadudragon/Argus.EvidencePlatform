output "id" {
  value = azurerm_container_app.this.id
}

output "name" {
  value = azurerm_container_app.this.name
}

output "url" {
  value = try("https://${azurerm_container_app.this.latest_revision_fqdn}", null)
}

