resource "azurerm_service_plan" "plan" {
  name                = "plan-camanager"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  os_type             = "Linux"
  sku_name            = "S1"
}

resource "azurerm_linux_web_app" "app" {
  name                = "app-camanager-${random_string.suffix.result}"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  service_plan_id     = azurerm_service_plan.plan.id

  site_config {
    vnet_route_all_enabled = true
  }

  virtual_network_subnet_id = azurerm_subnet.snet_app.id

  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.app_identity.id]
  }

  key_vault_reference_identity_id = azurerm_user_assigned_identity.app_identity.id

  app_settings = {
    "KeyVault__Url"            = azurerm_key_vault.kv.vault_uri
    "AzureAd__TenantId"        = data.azurerm_client_config.current.tenant_id
    "WEBSITE_RUN_FROM_PACKAGE" = "https://github.com/${var.github_repo}/releases/latest/download/latest.zip"
  }
}
