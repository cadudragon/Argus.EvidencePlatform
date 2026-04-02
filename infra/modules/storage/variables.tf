variable "name" {
  type = string
}

variable "resource_group_name" {
  type = string
}

variable "location" {
  type = string
}

variable "account_replication_type" {
  type = string
}

variable "private_endpoint_subnet_id" {
  type = string
}

variable "blob_private_dns_zone_id" {
  type = string
}

variable "staging_container_name" {
  type    = string
  default = "staging"
}

variable "evidence_container_name" {
  type    = string
  default = "evidence"
}

variable "exports_container_name" {
  type    = string
  default = "exports"
}

variable "blob_delete_retention_days" {
  type    = number
  default = 30
}

variable "container_delete_retention_days" {
  type    = number
  default = 30
}

variable "tags" {
  type    = map(string)
  default = {}
}

