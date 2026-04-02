output "id" {
  value = azurerm_storage_account.this.id
}

output "name" {
  value = azurerm_storage_account.this.name
}

output "primary_blob_endpoint" {
  value = azurerm_storage_account.this.primary_blob_endpoint
}

output "staging_container_name" {
  value = azurerm_storage_container.staging.name
}

output "evidence_container_name" {
  value = azurerm_storage_container.evidence.name
}

output "exports_container_name" {
  value = azurerm_storage_container.exports.name
}

