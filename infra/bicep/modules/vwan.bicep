param vwanHubName string
param vnetId string

resource hub 'Microsoft.Network/virtualHubs@2021-02-01' existing = {
  name: vwanHubName
}

resource connection 'Microsoft.Network/virtualHubs/hubVirtualNetworkConnections@2021-02-01' = {
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
