# Azure Key Vault CA Manager

A lightweight, secure ASP.NET Core web application for managing Azure Key Vault Certificates and performing remote CSR signing. This project allows you to operate a private Certificate Authority (CA) backed by the hardware security of Azure Key Vault.

## Features

- **Root CA Management**: Create and manage Root CAs directly within Azure Key Vault.
- **CSR Inspection**: Upload and inspect Certificate Signing Requests (CSRs) before signing.
- **Remote Signing**: Sign CSRs using keys stored in Azure Key Vault without the private key ever leaving the vault.
- **Secure Infrastructure**: 
  - Fully integrated with Azure Managed Identity.
  - VNet Integration for App Service.
  - Private Endpoints for Key Vault and Storage.
  - RBAC-based access control.

## Architecture

- **Frontend/API**: ASP.NET Core 8 MVC Web App.
- **Identity**: Microsoft Entra ID (stats-less authentication).
- **Storage**: Azure Key Vault (Keys/Certs) & Azure Blob Storage (Internal state if needed).
- **Infrastructure**: Terraform-managed Azure resources.

## Prerequisites

- Azure Subscription
- Azure CLI
- Terraform
- .NET 8 SDK

## Getting Started

### 1. Infrastructure Deployment

This project uses Terraform to provision all necessary Azure resources.

```bash
cd infra
terraform init
terraform apply -var="resource_group_name=my-rg" -var="location=UK South"
```

> **Note**: This will output the `app_service_name` and configure your Key Vault with Private Endpoints.

### 2. Local Development

1.  Add your user to the "Key Vault Administrator" role on the deployed Key Vault.
2.  Update `appsettings.json` (or use User Secrets) with your `AzureAd` config and `KeyVault:Url`.
3.  Run the application:
    ```bash
    dotnet run
    ```

### 3. CI/CD Pipeline

The project includes a GitHub Action (`.github/workflows/deploy.yml`) that:
1.  Builds and Tests the application.
2.  Uploads the build artifact (`latest.zip`) to the GitHub Release.
3.  **Manual Step**: You must restart the Azure App Service to pick up the new package from the Release URL.

**Secrets Required in GitHub:**
- `AZURE_CREDENTIALS`: Service Principal JSON for Azure login.
- `AZURE_WEBAPP_NAME`: Name of the App Service (from Terraform output).
- `AZURE_RESOURCE_GROUP`: Resource Group name.

## Deployment Model

The App Service is configured with `WEBSITE_RUN_FROM_PACKAGE` pointing to the GitHub Release URL. This ensures atomic deployments and easy rollbacks.
