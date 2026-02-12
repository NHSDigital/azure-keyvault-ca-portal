param location string
param vnetName string = 'vnet-camanager'
param vnetAddressSpace array = ['172.120.0.0/26']
param subnetPrefixes object = {
  app: '172.120.0.0/27'
  pe: '172.120.0.32/27'
}

resource vnet 'Microsoft.Network/virtualNetworks@2023-04-01' = {
  name: vnetName
  location: location
  properties: {
    addressSpace: {
      addressPrefixes: vnetAddressSpace
    }
    subnets: [
      {
        name: 'snet-app'
        properties: {
          addressPrefix: subnetPrefixes.app
          delegations: [
            {
              name: 'delegation'
              properties: {
                serviceName: 'Microsoft.Web/serverFarms'
              }
            }
          ]
        }
      }
      {
        name: 'snet-pe'
        properties: {
          addressPrefix: subnetPrefixes.pe
        }
      }
    ]
  }
}

output vnetId string = vnet.id
output snetAppId string = resourceId('Microsoft.Network/virtualNetworks/subnets', vnetName, 'snet-app')
output snetPeId string = resourceId('Microsoft.Network/virtualNetworks/subnets', vnetName, 'snet-pe')
