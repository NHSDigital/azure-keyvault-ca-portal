param location string

resource id 'Microsoft.ManagedIdentity/userAssignedIdentities@2018-11-30' = {
  name: 'id-camanager'
  location: location
}

output id string = id.id
output principalId string = id.properties.principalId
output clientId string = id.properties.clientId
