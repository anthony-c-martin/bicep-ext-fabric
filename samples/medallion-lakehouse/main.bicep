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
  description: 'Medallion data engineering workspace managed with Bicep'
}

resource bronze 'Lakehouse' = {
  workspaceId: workspace.id!
  displayName: '${workspaceName}-bronze'
  description: 'Bronze medallion layer'
  configuration: {
    enableSchemas: true
  }
}

resource silver 'Lakehouse' = {
  workspaceId: workspace.id!
  displayName: '${workspaceName}-silver'
  description: 'Silver medallion layer'
  configuration: {
    enableSchemas: true
  }
}

resource gold 'Lakehouse' = {
  workspaceId: workspace.id!
  displayName: '${workspaceName}-gold'
  description: 'Gold medallion layer'
  configuration: {
    enableSchemas: true
  }
}

resource ingestionPipeline 'DataPipeline' = {
  workspaceId: workspace.id!
  displayName: '${workspaceName}-ingestion'
  description: 'Ingests source data into the bronze lakehouse'
  dependsOn: [
    bronze
    silver
    gold
  ]
}

resource warehouse 'Warehouse' = {
  workspaceId: workspace.id!
  displayName: '${workspaceName}-serving'
  description: 'Serving layer for curated gold data'
  configuration: {
    collationType: 'Latin1_General_100_BIN2_UTF8'
  }
  dependsOn: [
    gold
  ]
}

output workspaceId string? = workspace.id
