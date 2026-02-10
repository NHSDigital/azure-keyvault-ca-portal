param vwanHubName string
param vnetId string

resource hub 'Microsoft.Network/virtualHubs@2023-04-01' existing = {
  name: vwanHubName
}

resource connection 'Microsoft.Network/virtualHubs/hubVirtualNetworkConnections@2023-04-01' = {
  parent: hub
  name: 'conn-camanager-to-hub'
  properties: {
    remoteVirtualNetwork: {
      id: vnetId
    }
    allowHubToRemoteVnetTransit: true
    allowRemoteVnetToUseHubVnetGateways: true
  }
}
