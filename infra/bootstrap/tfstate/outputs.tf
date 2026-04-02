output "resource_group_name" {
  description = "Resource group that holds the Terraform remote state storage account."
  value       = azurerm_resource_group.this.name
}

output "storage_account_name" {
  description = "Storage account name for the Terraform backend."
  value       = azurerm_storage_account.this.name
}

output "container_name" {
  description = "Blob container used for Terraform state."
  value       = azurerm_storage_container.this.name
}

output "backend_hcl_snippet" {
  description = "Copy these values into infra/environments/dev/backend.hcl."
  value       = <<-EOT
resource_group_name  = "${azurerm_resource_group.this.name}"
storage_account_name = "${azurerm_storage_account.this.name}"
container_name       = "${azurerm_storage_container.this.name}"
key                  = "dev.terraform.tfstate"
  EOT
}
