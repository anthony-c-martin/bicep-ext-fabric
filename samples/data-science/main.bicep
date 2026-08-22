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
  description: 'Data science workspace managed with Bicep'
}

resource lakehouse 'Lakehouse' = {
  workspaceId: workspace.id!
  displayName: '${workspaceName}-features'
  description: 'Curated features and training data'
  configuration: {
    enableSchemas: true
  }
}

resource environment 'Environment' = {
  workspaceId: workspace.id!
  displayName: '${workspaceName}-python'
  description: 'Shared Python environment for experimentation'
}

resource trainingJob 'SparkJobDefinition' = {
  workspaceId: workspace.id!
  displayName: '${workspaceName}-training-job'
  description: 'Scheduled Spark job that trains the forecasting model'
  dependsOn: [
    lakehouse
  ]
}

resource experiment 'MLExperiment' = {
  workspaceId: workspace.id!
  displayName: '${workspaceName}-forecasting'
  description: 'Tracks forecasting experiments'
  dependsOn: [
    lakehouse
    environment
  ]
}

resource model 'MLModel' = {
  workspaceId: workspace.id!
  displayName: '${workspaceName}-forecast-model'
  description: 'Registered forecasting model'
  dependsOn: [
    experiment
  ]
}

output workspaceId string? = workspace.id
