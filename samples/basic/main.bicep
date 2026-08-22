targetScope = 'local'

@secure()
@description('Microsoft Entra access token for https://api.fabric.microsoft.com.')
param fabricToken string

@description('Display name of the Fabric workspace.')
param workspaceName string

@description('Display name of the lakehouse.')
param lakehouseName string

extension fabric with {
  accessToken: fabricToken
}

resource workspace 'Workspace' = {
  displayName: workspaceName
  description: 'Managed with the Microsoft Fabric Bicep extension'
}

resource lakehouse 'Lakehouse' = {
  workspaceId: workspace.id!
  displayName: lakehouseName
  description: 'Lakehouse managed with Bicep'
  configuration: {
    enableSchemas: true
  }
}

resource database 'SQLDatabase' = {
  workspaceId: workspace.id!
  displayName: '${lakehouseName}-db'
  description: 'SQL database managed with Bicep'
  configuration: {
    creationMode: 'New'
  }
}

output workspaceId string? = workspace.id
output lakehouseId string? = lakehouse.id
