targetScope = 'local'

@secure()
@description('Microsoft Entra access token for https://api.fabric.microsoft.com.')
param fabricToken string

@description('Display name of the Fabric workspace.')
param workspaceName string

@description('ID of an existing Fabric Domain to assign the workspace to. Domain creation is not supported by Microsoft.Fabric.Api 2.20.0, so the domain must already exist.')
param domainId string

@description('Object ID of the Microsoft Entra user or group to grant workspace access to.')
param principalId string

extension fabric with {
  accessToken: fabricToken
}

resource workspace 'Workspace' = {
  displayName: workspaceName
  description: 'Workspace governance managed with Bicep'
}

resource reportsFolder 'Folder' = {
  workspaceId: workspace.id!
  displayName: 'Reports'
}

resource archiveFolder 'Folder' = {
  workspaceId: workspace.id!
  displayName: 'Archive'
  parentFolderId: reportsFolder.id!
}

resource domain 'Domain' = {
  id: domainId
}

resource domainAssignment 'DomainWorkspaceAssignment' = {
  domainId: domain.id!
  workspaceIds: [
    workspace.id!
  ]
}

resource governanceTag 'Tag' = {
  displayName: '${workspaceName}-governed'
  scope: {
    type: 'Domain'
    domainId: domain.id!
  }
}

// This resource owns the Contributor role on the domain: any principal not listed here is unassigned.
resource domainContributors 'DomainRoleAssignments' = {
  domainId: domain.id!
  role: 'Contributor'
  principals: [
    {
      id: principalId
      type: 'User'
    }
  ]
}

// Tags are applied by ID, so governed items reference the tag created above.
resource governedNotebook 'Notebook' = {
  workspaceId: workspace.id!
  displayName: '${workspaceName}-governed-notebook'
  description: 'Notebook tagged for governance reporting'
  folderId: reportsFolder.id!
  tags: [
    governanceTag.id!
  ]
}

resource workspaceAccess 'WorkspaceRoleAssignment' = {
  workspaceId: workspace.id!
  principal: {
    id: principalId
    type: 'User'
  }
  role: 'Member'
}

resource releasePipeline 'DeploymentPipeline' = {
  displayName: '${workspaceName}-release'
  description: 'Promotes items from development to production'
  stages: [
    {
      displayName: 'Development'
      description: 'Development stage'
      isPublic: false
      workspaceId: workspace.id!
    }
    {
      displayName: 'Production'
      description: 'Production stage'
      isPublic: true
    }
  ]
}

resource pipelineAccess 'DeploymentPipelineRoleAssignment' = {
  deploymentPipelineId: releasePipeline.id!
  principal: {
    id: principalId
    type: 'User'
  }
  role: 'Admin'
}

output workspaceId string? = workspace.id
output reportsFolderId string? = reportsFolder.id
output tagId string? = governanceTag.id
output governedNotebookId string? = governedNotebook.id
output releasePipelineId string? = releasePipeline.id
