locals {
  normalized_base_name = lower(replace(var.base_name, "-", ""))
  common_tags = merge(var.tags, {
    environment = var.environment
    managed-by  = "terraform"
    stack       = "bootstrap"
    system      = "argus-evidence-platform"
  })
}

resource "random_string" "suffix" {
  length  = 5
  upper   = false
  special = false
}

resource "azurerm_resource_group" "this" {
  name     = "rg-${var.base_name}-${var.environment}-tfstate"
  location = var.location
  tags     = local.common_tags
}

resource "azurerm_storage_account" "this" {
  name                     = substr("st${local.normalized_base_name}${var.environment}${random_string.suffix.result}", 0, 24)
  resource_group_name      = azurerm_resource_group.this.name
  location                 = azurerm_resource_group.this.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
  account_kind             = "StorageV2"
  min_tls_version          = "TLS1_2"

  allow_nested_items_to_be_public = false
  https_traffic_only_enabled      = true
  shared_access_key_enabled       = true

  blob_properties {
    versioning_enabled = true
  }

  tags = local.common_tags
}

resource "azurerm_storage_container" "this" {
  name                  = "tfstate"
  storage_account_id    = azurerm_storage_account.this.id
  container_access_type = "private"
}

