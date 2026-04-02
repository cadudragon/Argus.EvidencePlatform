data "azurerm_client_config" "current" {}

resource "random_string" "suffix" {
  length  = 5
  upper   = false
  special = false
}

locals {
  short_location       = "weu"
  normalized_base_name = lower(replace(var.base_name, "-", ""))

  common_tags = merge(var.tags, {
    environment = var.environment
    managed-by  = "terraform"
    system      = "argus-evidence-platform"
  })

  resource_group_name            = "rg-${var.base_name}-${var.environment}-${local.short_location}"
  virtual_network_name           = "vnet-${var.base_name}-${var.environment}-${local.short_location}"
  log_analytics_name             = "log-${var.base_name}-${var.environment}-${local.short_location}"
  application_insights_name      = "appi-${var.base_name}-${var.environment}-${local.short_location}"
  container_app_environment_name = "acae-${var.base_name}-${var.environment}-${local.short_location}"
  api_identity_name              = "uai-${var.base_name}-${var.environment}-api"
  worker_identity_name           = "uai-${var.base_name}-${var.environment}-worker"
  api_container_app_name         = "ca-${var.base_name}-${var.environment}-api"
  worker_container_app_name      = "ca-${var.base_name}-${var.environment}-worker"
  key_vault_name                 = substr("kv-${local.normalized_base_name}-${var.environment}-${random_string.suffix.result}", 0, 24)
  acr_name                       = substr("acr${local.normalized_base_name}${var.environment}${random_string.suffix.result}", 0, 50)
  storage_account_name           = substr("st${local.normalized_base_name}${var.environment}${random_string.suffix.result}", 0, 24)
  postgres_server_name           = substr("psql-${local.normalized_base_name}-${var.environment}-${random_string.suffix.result}", 0, 63)
}

module "resource_group" {
  source = "../../modules/resource_group"

  name     = local.resource_group_name
  location = var.location
  tags     = local.common_tags
}

module "network" {
  source = "../../modules/network"

  name                          = local.virtual_network_name
  resource_group_name           = module.resource_group.name
  location                      = module.resource_group.location
  address_space                 = var.address_space
  container_apps_subnet_cidr    = var.container_apps_subnet_cidr
  postgres_subnet_cidr          = var.postgres_subnet_cidr
  private_endpoints_subnet_cidr = var.private_endpoints_subnet_cidr
  tags                          = local.common_tags
}

module "monitoring" {
  source = "../../modules/monitoring"

  log_analytics_workspace_name = local.log_analytics_name
  application_insights_name    = local.application_insights_name
  resource_group_name          = module.resource_group.name
  location                     = module.resource_group.location
  tags                         = local.common_tags
}

module "container_registry" {
  source = "../../modules/container_registry"

  name                          = local.acr_name
  resource_group_name           = module.resource_group.name
  location                      = module.resource_group.location
  sku                           = var.acr_sku
  public_network_access_enabled = var.acr_public_network_access_enabled
  tags                          = local.common_tags
}

module "key_vault" {
  source = "../../modules/key_vault"

  name                       = local.key_vault_name
  resource_group_name        = module.resource_group.name
  location                   = module.resource_group.location
  tenant_id                  = data.azurerm_client_config.current.tenant_id
  private_endpoint_subnet_id = module.network.private_endpoints_subnet_id
  private_dns_zone_id        = module.network.key_vault_private_dns_zone_id
  tags                       = local.common_tags
}

module "storage" {
  source = "../../modules/storage"

  name                       = local.storage_account_name
  resource_group_name        = module.resource_group.name
  location                   = module.resource_group.location
  account_replication_type   = var.storage_account_replication_type
  private_endpoint_subnet_id = module.network.private_endpoints_subnet_id
  blob_private_dns_zone_id   = module.network.blob_private_dns_zone_id
  tags                       = local.common_tags
}

module "postgresql" {
  source = "../../modules/postgresql_flexible_server"

  name                  = local.postgres_server_name
  resource_group_name   = module.resource_group.name
  location              = module.resource_group.location
  postgres_version      = var.postgres_version
  administrator_login   = var.postgres_admin_login
  sku_name              = var.postgres_sku_name
  storage_mb            = var.postgres_storage_mb
  backup_retention_days = var.postgres_backup_retention_days
  delegated_subnet_id   = module.network.postgres_subnet_id
  private_dns_zone_id   = module.network.postgres_private_dns_zone_id
  database_name         = var.database_name
  availability_zone     = var.postgres_availability_zone
  tags                  = local.common_tags
}

resource "azurerm_user_assigned_identity" "api" {
  name                = local.api_identity_name
  resource_group_name = module.resource_group.name
  location            = module.resource_group.location
  tags                = local.common_tags
}

resource "azurerm_user_assigned_identity" "worker" {
  name                = local.worker_identity_name
  resource_group_name = module.resource_group.name
  location            = module.resource_group.location
  tags                = local.common_tags
}

resource "azurerm_role_assignment" "api_acr_pull" {
  scope                = module.container_registry.id
  role_definition_name = "AcrPull"
  principal_id         = azurerm_user_assigned_identity.api.principal_id
}

resource "azurerm_role_assignment" "worker_acr_pull" {
  scope                = module.container_registry.id
  role_definition_name = "AcrPull"
  principal_id         = azurerm_user_assigned_identity.worker.principal_id
}

resource "azurerm_role_assignment" "api_storage_blob_data_contributor" {
  scope                = module.storage.id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = azurerm_user_assigned_identity.api.principal_id
}

resource "azurerm_role_assignment" "worker_storage_blob_data_contributor" {
  scope                = module.storage.id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = azurerm_user_assigned_identity.worker.principal_id
}

module "container_apps_environment" {
  source = "../../modules/container_apps_environment"

  name                       = local.container_app_environment_name
  resource_group_name        = module.resource_group.name
  location                   = module.resource_group.location
  log_analytics_workspace_id = module.monitoring.log_analytics_workspace_id
  infrastructure_subnet_id   = module.network.container_apps_subnet_id
  tags                       = local.common_tags
}

module "api_container_app" {
  count  = var.deploy_workloads ? 1 : 0
  source = "../../modules/container_app"

  name                         = local.api_container_app_name
  resource_group_name          = module.resource_group.name
  container_app_environment_id = module.container_apps_environment.id
  identity_id                  = azurerm_user_assigned_identity.api.id
  registry_server              = module.container_registry.login_server
  container_name               = "api"
  image                        = var.api_image
  cpu                          = var.api_cpu
  memory                       = var.api_memory
  min_replicas                 = var.api_min_replicas
  max_replicas                 = var.api_max_replicas
  secrets = {
    postgres-connection-string = module.postgresql.connection_string
  }
  env = {
    APPLICATIONINSIGHTS_CONNECTION_STRING = module.monitoring.application_insights_connection_string
    ASPNETCORE_ENVIRONMENT                = var.application_environment
    ASPNETCORE_URLS                       = "http://0.0.0.0:8080"
    Authentication__Audience              = var.auth_audience
    Authentication__Authority             = var.auth_authority
    OTEL_SERVICE_NAME                     = "argus-evidence-api"
    Storage__ConnectionName               = "unused"
    Storage__EvidenceContainerName        = module.storage.evidence_container_name
    Storage__ExportsContainerName         = module.storage.exports_container_name
    Storage__ServiceUri                   = module.storage.primary_blob_endpoint
    Storage__StagingContainerName         = module.storage.staging_container_name
    Wolverine__AutoProvision              = tostring(var.wolverine_auto_provision)
  }
  secret_env = {
    ConnectionStrings__postgresdb = "postgres-connection-string"
  }
  ingress = {
    external_enabled = true
    target_port      = 8080
  }
  tags = local.common_tags

  depends_on = [
    azurerm_role_assignment.api_acr_pull,
    azurerm_role_assignment.api_storage_blob_data_contributor
  ]
}

module "worker_container_app" {
  count  = var.deploy_workloads ? 1 : 0
  source = "../../modules/container_app"

  name                         = local.worker_container_app_name
  resource_group_name          = module.resource_group.name
  container_app_environment_id = module.container_apps_environment.id
  identity_id                  = azurerm_user_assigned_identity.worker.id
  registry_server              = module.container_registry.login_server
  container_name               = "workers"
  image                        = var.worker_image
  cpu                          = var.worker_cpu
  memory                       = var.worker_memory
  min_replicas                 = var.worker_min_replicas
  max_replicas                 = var.worker_max_replicas
  secrets = {
    postgres-connection-string = module.postgresql.connection_string
  }
  env = {
    APPLICATIONINSIGHTS_CONNECTION_STRING = module.monitoring.application_insights_connection_string
    DOTNET_ENVIRONMENT                    = var.application_environment
    OTEL_SERVICE_NAME                     = "argus-evidence-workers"
    Storage__ConnectionName               = "unused"
    Storage__EvidenceContainerName        = module.storage.evidence_container_name
    Storage__ExportsContainerName         = module.storage.exports_container_name
    Storage__ServiceUri                   = module.storage.primary_blob_endpoint
    Storage__StagingContainerName         = module.storage.staging_container_name
    Wolverine__AutoProvision              = tostring(var.wolverine_auto_provision)
  }
  secret_env = {
    ConnectionStrings__postgresdb = "postgres-connection-string"
  }
  tags = local.common_tags

  depends_on = [
    azurerm_role_assignment.worker_acr_pull,
    azurerm_role_assignment.worker_storage_blob_data_contributor
  ]
}
