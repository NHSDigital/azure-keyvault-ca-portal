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

// Private DNS Zones
resource pdnsVault 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  name: 'privatelink.vaultcore.azure.net'
  location: 'global'
}

resource pdnsBlob 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  name: 'privatelink.blob.${environment().suffixes.storage}'
  location: 'global'
}

resource pdnsApp 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  name: 'privatelink.azurewebsites.net'
  location: 'global'
}

// Links
resource linkVault 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = {
  parent: pdnsVault
  name: 'link-vault'
  location: 'global'
  properties: {
    registrationEnabled: false
    virtualNetwork: {
      id: vnet.id
    }
  }
}

resource linkBlob 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = {
  parent: pdnsBlob
  name: 'link-blob'
  location: 'global'
  properties: {
    registrationEnabled: false
    virtualNetwork: {
      id: vnet.id
    }
  }
}

resource linkApp 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = {
  parent: pdnsApp
  name: 'link-app'
  location: 'global'
  properties: {
    registrationEnabled: false
    virtualNetwork: {
      id: vnet.id
    }
  }
}

output vnetId string = vnet.id
output snetAppId string = resourceId('Microsoft.Network/virtualNetworks/subnets', vnetName, 'snet-app')
output snetPeId string = resourceId('Microsoft.Network/virtualNetworks/subnets', vnetName, 'snet-pe')
output pdnsVaultId string = pdnsVault.id
output pdnsBlobId string = pdnsBlob.id
output pdnsAppId string = pdnsApp.id
