param location string
param suffix string
param snetPeId string
param pdnsVaultId string
param appIdentityPrincipalId string
param tenantId string

resource kv 'Microsoft.KeyVault/vaults@2021-06-01-preview' = {
  name: 'kv-camanager-${suffix}'
  location: location
  properties: {
    tenantId: tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    enableRbacAuthorization: true
    publicNetworkAccess: 'Disabled'
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
  }
}

// Private Endpoint
resource peKv 'Microsoft.Network/privateEndpoints@2021-02-01' = {
  name: 'pe-kv-camanager'
  location: location
  properties: {
    subnet: {
      id: snetPeId
    }
    privateLinkServiceConnections: [
      {
        name: 'psc-kv'
        properties: {
          privateLinkServiceId: kv.id
          groupIds: [
            'vault'
          ]
        }
      }
    ]
  }
}

resource peKvDnsGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2021-02-01' = {
  parent: peKv
  name: 'pdns-kv-group'
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'privatelink-vaultcore-azure-net'
        properties: {
          privateDnsZoneId: pdnsVaultId
        }
      }
    ]
  }
}

// RBAC
var roleKeyVaultAdministrator = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  '00482a5a-887f-4fb3-b363-3b7fe8e74483'
)

resource roleAssignmentKvAdmin 'Microsoft.Authorization/roleAssignments@2020-04-01-preview' = {
  scope: kv
  name: guid(kv.id, appIdentityPrincipalId, roleKeyVaultAdministrator)
  properties: {
    roleDefinitionId: roleKeyVaultAdministrator
    principalId: appIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

output vaultUri string = kv.properties.vaultUri
