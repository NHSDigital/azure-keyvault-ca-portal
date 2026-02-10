param location string
param suffix string
param snetAppId string
param snetPeId string
param pdnsAppId string
param keyVaultUrl string
param tenantId string
param githubRepo string
param storageAccountName string
param appInsightsConnectionString string
param auditTableName string
param appTitle string
param identityId string
param identityClientId string

resource plan 'Microsoft.Web/serverfarms@2021-02-01' = {
  name: 'plan-camanager'
  location: location
  kind: 'linux'
  sku: {
    name: 'B3'
  }
  properties: {
    reserved: true
  }
}

resource app 'Microsoft.Web/sites@2021-02-01' = {
  name: 'app-camanager-${suffix}'
  location: location
  kind: 'app,linux'
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    publicNetworkAccess: 'Disabled'
    virtualNetworkSubnetId: snetAppId
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      vnetRouteAllEnabled: true
    }
    keyVaultReferenceIdentity: identityId
  }
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${identityId}': {}
    }
  }
}

resource appSettings 'Microsoft.Web/sites/config@2021-02-01' = {
  parent: app
  name: 'appsettings'
  properties: {
    KeyVault__Url: keyVaultUrl
    AzureAd__TenantId: tenantId
    WEBSITE_RUN_FROM_PACKAGE: 'https://github.com/${githubRepo}/releases/latest/download/latest.zip'
    AzureWebJobsStorage__accountName: storageAccountName
    AzureWebJobsStorage__credential: 'managedidentity'
    AzureWebJobsStorage__clientId: identityClientId
    AppTitle: appTitle
    ApplicationInsights__ConnectionString: appInsightsConnectionString
    Storage__AuditTableName: auditTableName
    Storage__AccountName: storageAccountName
  }
}

// Private Endpoint
resource peApp 'Microsoft.Network/privateEndpoints@2021-02-01' = {
  name: 'pe-app-camanager'
  location: location
  properties: {
    subnet: {
      id: snetPeId
    }
    privateLinkServiceConnections: [
      {
        name: 'psc-app'
        properties: {
          privateLinkServiceId: app.id
          groupIds: [
            'sites'
          ]
        }
      }
    ]
  }
}

resource peAppDnsGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2021-02-01' = {
  parent: peApp
  name: 'pdns-app-group'
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'privatelink-azurewebsites-net'
        properties: {
          privateDnsZoneId: pdnsAppId
        }
      }
    ]
  }
}

output appName string = app.name
