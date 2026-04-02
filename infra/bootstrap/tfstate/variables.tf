variable "subscription_id" {
  description = "Azure subscription ID for the bootstrap resources."
  type        = string
}

variable "location" {
  description = "Azure region for the bootstrap resources."
  type        = string
  default     = "westeurope"
}

variable "base_name" {
  description = "Base name used to derive resource names."
  type        = string
  default     = "argusep"
}

variable "environment" {
  description = "Environment identifier used in names."
  type        = string
  default     = "shared"
}

variable "tags" {
  description = "Tags applied to bootstrap resources."
  type        = map(string)
  default     = {}
}

