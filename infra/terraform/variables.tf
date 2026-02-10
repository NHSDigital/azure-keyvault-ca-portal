variable "resource_group_name" {
  description = "Name of the Resource Group"
  type        = string
  default     = "rg-camanager-dev"
}

variable "location" {
  description = "Azure Region"
  type        = string
  default     = "uksouth"
}

variable "github_repo" {
  description = "GitHub Repository (org/repo)"
  type        = string
  default     = "celloza/azure-keyvault-ca-portal"
}

variable "subscription_id" {
  description = "Azure Subscription ID"
  type        = string
}

variable "app_title" {
  description = "Title of the Application"
  type        = string
  default     = "CAManager"
}

variable "vnet_address_space" {
  description = "Address space for the Virtual Network"
  type        = list(string)
  default     = ["172.120.0.0/26"]
}

variable "subnet_prefixes" {
  description = "Address prefixes for subnets"
  type        = map(string)
  default = {
    app = "172.120.0.0/27"
    pe  = "172.120.0.32/27"
  }
}

variable "storage_public_access_enabled" {
  description = "Enable public access to Storage Account (required for local Terraform execution)"
  type        = bool
  default     = false
}
