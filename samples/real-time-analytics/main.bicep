targetScope = 'local'

@secure()
@description('Microsoft Entra access token for https://api.fabric.microsoft.com.')
param fabricToken string

@description('Display name of the Fabric workspace.')
param workspaceName string

extension fabric with {
  accessToken: fabricToken
}

resource workspace 'Workspace' = {
  displayName: workspaceName
  description: 'Real-time analytics workspace managed with Bicep'
}

resource eventhouse 'Eventhouse' = {
  workspaceId: workspace.id!
  displayName: '${workspaceName}-eventhouse'
  description: 'Event data storage and analytics'
  configuration: {
    minimumConsumptionUnits: '2.25'
  }
}

resource eventstream 'Eventstream' = {
  workspaceId: workspace.id!
  displayName: '${workspaceName}-stream'
  description: 'Streaming ingestion pipeline'
  dependsOn: [
    eventhouse
  ]
}

resource telemetryDatabase 'KQLDatabase' = {
  workspaceId: workspace.id!
  displayName: '${workspaceName}-telemetry'
  description: 'Telemetry database backed by the eventhouse'
  configuration: {
    eventhouseId: eventhouse.id!
    databaseType: 'ReadWrite'
  }
}

resource querySet 'KQLQueryset' = {
  workspaceId: workspace.id!
  displayName: '${workspaceName}-queries'
  description: 'Operational KQL queries'
  dependsOn: [
    telemetryDatabase
  ]
}

resource dashboard 'KQLDashboard' = {
  workspaceId: workspace.id!
  displayName: '${workspaceName}-operations'
  description: 'Operational real-time dashboard'
  dependsOn: [
    querySet
  ]
}

output workspaceId string? = workspace.id
output telemetryDatabaseId string? = telemetryDatabase.id
output queryServiceUri string? = telemetryDatabase.queryServiceUri
