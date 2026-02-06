variable "resource_group_name" {
  description = "Name of the Resource Group"
  type        = string
  default     = "rg-camanager-dev"
}

variable "location" {
  description = "Azure Region"
  type        = string
  default     = "UK South"
}

variable "github_repo" {
  description = "GitHub Repository (org/repo)"
  type        = string
  default     = "celloza/azure-keyvault-ca-portal"
}
