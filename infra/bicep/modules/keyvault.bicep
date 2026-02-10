param location string
param suffix string
param snetPeId string
param pdnsVaultId string
param appIdentityPrincipalId string
param tenantId string
param keyVaultAdminRoleDefinitionId string = '00482a5a-887f-4fb3-b363-3b7fe8e74483' // Standard Azure ID for Key Vault Administrator

resource kv 'Microsoft.KeyVault/vaults@2023-02-01' = {
  name: 'kv-cam-${suffix}'
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
resource peKv 'Microsoft.Network/privateEndpoints@2023-04-01' = {
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

resource peKvDnsGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2023-04-01' = {
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
resource roleKeyVaultAdministrator 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  scope: subscription()
  name: keyVaultAdminRoleDefinitionId
}

resource roleAssignmentKvAdmin 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: kv
  name: guid(kv.id, appIdentityPrincipalId, roleKeyVaultAdministrator.id)
  properties: {
    roleDefinitionId: roleKeyVaultAdministrator.id
    principalId: appIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

output vaultUri string = kv.properties.vaultUri
