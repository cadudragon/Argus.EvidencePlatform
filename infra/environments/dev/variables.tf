variable "subscription_id" {
  description = "Azure subscription ID for the dev environment."
  type        = string
}

variable "location" {
  description = "Azure region for the dev environment."
  type        = string
  default     = "westeurope"
}

variable "environment" {
  description = "Environment identifier."
  type        = string
  default     = "dev"
}

variable "base_name" {
  description = "Base name used to derive resource names."
  type        = string
  default     = "argusep"
}

variable "application_environment" {
  description = "Environment value passed to the .NET workloads."
  type        = string
  default     = "Production"
}

variable "deploy_workloads" {
  description = "Whether to deploy the API and worker Container Apps."
  type        = bool
  default     = false
}

variable "api_image" {
  description = "Container image for Argus.EvidencePlatform.Api."
  type        = string
  default     = null
  nullable    = true
}

variable "worker_image" {
  description = "Container image for Argus.EvidencePlatform.Workers."
  type        = string
  default     = null
  nullable    = true
}

variable "auth_authority" {
  description = "OIDC authority passed to the API."
  type        = string
  default     = ""
}

variable "auth_audience" {
  description = "OIDC audience passed to the API."
  type        = string
  default     = ""
}

variable "postgres_admin_login" {
  description = "Administrator login for PostgreSQL Flexible Server."
  type        = string
}

variable "postgres_version" {
  description = "PostgreSQL major version."
  type        = string
  default     = "16"
}

variable "postgres_sku_name" {
  description = "SKU for PostgreSQL Flexible Server."
  type        = string
  default     = "B_Standard_B1ms"
}

variable "postgres_storage_mb" {
  description = "Allocated PostgreSQL storage in MB."
  type        = number
  default     = 32768
}

variable "postgres_backup_retention_days" {
  description = "Backup retention window in days."
  type        = number
  default     = 7
}

variable "postgres_availability_zone" {
  description = "Availability zone for PostgreSQL. Set null to let Azure choose."
  type        = string
  default     = null
  nullable    = true
}

variable "database_name" {
  description = "Primary application database name."
  type        = string
  default     = "argus_evidence_platform"
}

variable "api_min_replicas" {
  description = "Minimum replica count for the API Container App."
  type        = number
  default     = 1
}

variable "api_max_replicas" {
  description = "Maximum replica count for the API Container App."
  type        = number
  default     = 2
}

variable "worker_min_replicas" {
  description = "Minimum replica count for the worker Container App."
  type        = number
  default     = 1
}

variable "worker_max_replicas" {
  description = "Maximum replica count for the worker Container App."
  type        = number
  default     = 2
}

variable "api_cpu" {
  description = "CPU allocation for the API Container App."
  type        = number
  default     = 0.5
}

variable "api_memory" {
  description = "Memory allocation for the API Container App."
  type        = string
  default     = "1Gi"
}

variable "worker_cpu" {
  description = "CPU allocation for the worker Container App."
  type        = number
  default     = 0.5
}

variable "worker_memory" {
  description = "Memory allocation for the worker Container App."
  type        = string
  default     = "1Gi"
}

variable "acr_sku" {
  description = "SKU for Azure Container Registry."
  type        = string
  default     = "Standard"
}

variable "acr_public_network_access_enabled" {
  description = "Whether ACR allows public network access."
  type        = bool
  default     = true
}

variable "storage_account_replication_type" {
  description = "Replication type for the workload storage account."
  type        = string
  default     = "LRS"
}

variable "wolverine_auto_provision" {
  description = "Whether Wolverine should auto-provision durable storage on startup."
  type        = bool
  default     = false
}

variable "address_space" {
  description = "VNet address space for the environment."
  type        = list(string)
  default     = ["10.42.0.0/24"]
}

variable "container_apps_subnet_cidr" {
  description = "Subnet CIDR for the Container Apps environment."
  type        = string
  default     = "10.42.0.0/27"
}

variable "postgres_subnet_cidr" {
  description = "Subnet CIDR for PostgreSQL Flexible Server private access."
  type        = string
  default     = "10.42.0.32/27"
}

variable "private_endpoints_subnet_cidr" {
  description = "Subnet CIDR for private endpoints."
  type        = string
  default     = "10.42.0.64/27"
}

variable "tags" {
  description = "Additional tags applied to all resources."
  type        = map(string)
  default     = {}
}

