
data "azurerm_virtual_hub" "hub" {
  name                = var.vwan_hub_name
  resource_group_name = var.vwan_resource_group_name
}

resource "azurerm_virtual_hub_connection" "connection" {
  name                      = "conn-camanager-to-hub"
  virtual_hub_id            = data.azurerm_virtual_hub.hub.id
  remote_virtual_network_id = azurerm_virtual_network.vnet.id
}
