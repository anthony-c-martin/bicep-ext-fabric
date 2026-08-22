targetScope = 'local'

@secure()
@description('Microsoft Entra access token for https://api.fabric.microsoft.com.')
param fabricToken string

@description('Display name of the Fabric workspace.')
param workspaceName string

@description('Azure DevOps organization that hosts the repository.')
param organizationName string

@description('Azure DevOps project that hosts the repository.')
param projectName string

@description('Name of the Git repository to connect the workspace to.')
param repositoryName string

@description('Branch that the workspace is connected to.')
param branchName string

@description('Directory within the repository that holds the workspace items. Must start with a forward slash.')
param directoryName string

@description('ID of an existing Fabric Connection that authenticates to the Git provider.')
param gitConnectionId string

extension fabric with {
  accessToken: fabricToken
}

resource workspace 'Workspace' = {
  displayName: workspaceName
  description: 'Workspace synchronised with Git'
  // Provisioning a workspace identity lets the workspace authenticate to Azure resources directly.
  identity: {
    type: 'SystemAssigned'
  }
}

// Connecting the workspace also initializes it. 'PreferRemote' makes the repository the source of
// truth, so existing workspace items are overwritten by their Git counterparts.
resource workspaceGit 'WorkspaceGit' = {
  workspaceId: workspace.id!
  gitProviderDetails: {
    gitProviderType: 'AzureDevOps'
    organizationName: organizationName
    projectName: projectName
    repositoryName: repositoryName
    branchName: branchName
    directoryName: directoryName
  }
  gitCredentials: {
    source: 'ConfiguredConnection'
    connectionId: gitConnectionId
  }
  initializationStrategy: 'PreferRemote'
  options: {
    allowOverrideItems: true
  }
}

output workspaceId string? = workspace.id
output workspaceIdentityApplicationId string? = workspace.identity!.applicationId
output oneLakeDfsEndpoint string? = workspace.oneLakeEndpoints!.dfsEndpoint
output gitConnectionState string? = workspaceGit.gitConnectionState
output gitHead string? = workspaceGit.gitSyncDetails!.head
