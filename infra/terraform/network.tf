resource "azurerm_virtual_network" "vnet" {
  name                = "vnet-camanager"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  address_space       = var.vnet_address_space
}

resource "azurerm_subnet" "snet_app" {
  name                 = "snet-app"
  resource_group_name  = azurerm_resource_group.rg.name
  virtual_network_name = azurerm_virtual_network.vnet.name
  address_prefixes     = [var.subnet_prefixes["app"]]

  delegation {
    name = "delegation"
    service_delegation {
      name    = "Microsoft.Web/serverFarms"
      actions = ["Microsoft.Network/virtualNetworks/subnets/action"]
    }
  }
}

resource "azurerm_subnet" "snet_pe" {
  name                 = "snet-pe"
  resource_group_name  = azurerm_resource_group.rg.name
  virtual_network_name = azurerm_virtual_network.vnet.name
  address_prefixes     = [var.subnet_prefixes["pe"]]
}

resource "azurerm_private_dns_zone" "pdns_vault" {
  name                = "privatelink.vaultcore.azure.net"
  resource_group_name = azurerm_resource_group.rg.name
}

resource "azurerm_private_dns_zone" "pdns_blob" {
  name                = "privatelink.blob.core.windows.net"
  resource_group_name = azurerm_resource_group.rg.name
}

resource "azurerm_private_dns_zone_virtual_network_link" "link_vault" {
  name                  = "link-vault"
  resource_group_name   = azurerm_resource_group.rg.name
  private_dns_zone_name = azurerm_private_dns_zone.pdns_vault.name
  virtual_network_id    = azurerm_virtual_network.vnet.id
}

resource "azurerm_private_dns_zone_virtual_network_link" "link_blob" {
  name                  = "link-blob"
  resource_group_name   = azurerm_resource_group.rg.name
  private_dns_zone_name = azurerm_private_dns_zone.pdns_blob.name
  virtual_network_id    = azurerm_virtual_network.vnet.id
}

resource "azurerm_private_dns_zone" "pdns_app" {
  name                = "privatelink.azurewebsites.net"
  resource_group_name = azurerm_resource_group.rg.name
}

resource "azurerm_private_dns_zone_virtual_network_link" "link_app" {
  name                  = "link-app"
  resource_group_name   = azurerm_resource_group.rg.name
  private_dns_zone_name = azurerm_private_dns_zone.pdns_app.name
  virtual_network_id    = azurerm_virtual_network.vnet.id
}


