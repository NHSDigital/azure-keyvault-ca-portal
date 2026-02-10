param location string
param suffix string

resource log 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: 'log-camanager-${suffix}'
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: 'appi-camanager-${suffix}'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: log.id
  }
}

output appInsightsConnectionString string = appInsights.properties.ConnectionString
