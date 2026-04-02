output "resource_group_name" {
  description = "Primary resource group for the dev environment."
  value       = module.resource_group.name
}

output "container_registry_login_server" {
  description = "Login server for Azure Container Registry."
  value       = module.container_registry.login_server
}

output "container_app_environment_name" {
  description = "Container Apps managed environment name."
  value       = module.container_apps_environment.name
}

output "api_url" {
  description = "Public URL for the API Container App when workloads are deployed."
  value       = try(module.api_container_app[0].url, null)
}

output "postgres_fqdn" {
  description = "FQDN of the PostgreSQL Flexible Server."
  value       = module.postgresql.fqdn
}

output "database_name" {
  description = "Primary application database name."
  value       = module.postgresql.database_name
}

output "storage_blob_endpoint" {
  description = "Blob service endpoint used by the workloads."
  value       = module.storage.primary_blob_endpoint
}

output "key_vault_uri" {
  description = "URI of the Key Vault."
  value       = module.key_vault.vault_uri
}

output "postgres_connection_string" {
  description = "Primary application connection string."
  value       = module.postgresql.connection_string
  sensitive   = true
}
