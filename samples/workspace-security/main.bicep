targetScope = 'local'

@secure()
@description('Microsoft Entra access token for https://api.fabric.microsoft.com.')
param fabricToken string

@description('Display name of the Fabric workspace.')
param workspaceName string

@description('Object ID of the Microsoft Entra group to grant OneLake data access to.')
param principalId string

@description('Microsoft Entra tenant ID that principalId belongs to.')
param tenantId string

@description('Azure resource ID of the private-link resource (e.g. an Azure SQL server) to target with the Managed Private Endpoint.')
param targetPrivateLinkResourceId string

extension fabric with {
  accessToken: fabricToken
}

resource workspace 'Workspace' = {
  displayName: workspaceName
  description: 'Workspace security and settings managed with Bicep'
}

// Restrict Git integration to only reach explicitly trusted destinations.
resource gitOutboundPolicy 'WorkspaceGitOutboundPolicy' = {
  workspaceId: workspace.id!
  defaultAction: 'Deny'
}

// Block outbound access through on-premises data gateways by default.
resource outboundGatewayRules 'WorkspaceOutboundGatewayRules' = {
  workspaceId: workspace.id!
  defaultAction: 'Deny'
  allowedGateways: []
}

// Allow outbound web connections only to a trusted hostname pattern; deny everything else.
resource outboundCloudConnectionRules 'WorkspaceOutboundCloudConnectionRules' = {
  workspaceId: workspace.id!
  defaultAction: 'Deny'
  rules: [
    {
      connectionType: 'Web'
      defaultAction: 'Deny'
      allowedEndpoints: [
        {
          hostnamePattern: '*.contoso.com'
        }
      ]
    }
  ]
}

// Deny inbound and outbound communication with public networks by default.
resource networkCommunicationPolicy 'WorkspaceNetworkCommunicationPolicy' = {
  workspaceId: workspace.id!
  inbound: {
    publicAccessRules: {
      defaultAction: 'Deny'
    }
  }
  outbound: {
    publicAccessRules: {
      defaultAction: 'Deny'
    }
  }
}

// Enable automatic logging and reserve compute for admitted Spark jobs across the workspace.
resource sparkSettings 'SparkWorkspaceSettings' = {
  workspaceId: workspace.id!
  automaticLog: {
    enabled: true
  }
  job: {
    conservativeJobAdmissionEnabled: true
    sessionTimeoutInMinutes: 60
  }
}

resource lakehouse 'Lakehouse' = {
  workspaceId: workspace.id!
  displayName: 'SecuredLakehouse'
  configuration: {
    enableSchemas: true
  }
}

// Grant a Microsoft Entra group read-only access to a single lakehouse folder via a OneLake data access role.
resource lakehouseDataAccess 'OneLakeDataAccessSecurity' = {
  workspaceId: workspace.id!
  itemId: lakehouse.id!
  roleName: 'AnalystsReadOnly'
  decisionRules: [
    {
      effect: 'Permit'
      permission: [
        {
          attributeName: 'Path'
          attributeValueIncludedIn: [
            '*'
          ]
        }
      ]
    }
  ]
  members: {
    microsoftEntraMembers: [
      {
        objectId: principalId
        objectType: 'Group'
        tenantId: tenantId
      }
    ]
  }
}

resource warehouse 'Warehouse' = {
  workspaceId: workspace.id!
  displayName: 'SecuredWarehouse'
  configuration: {
    collationType: 'Latin1_General_100_BIN2_UTF8'
  }
}

// Capture failed and successful authentication attempts against the warehouse for 90 days.
resource warehouseAuditSettings 'WarehouseSqlAuditSettings' = {
  workspaceId: workspace.id!
  warehouseId: warehouse.id!
  state: 'Enabled'
  retentionDays: 90
  auditActionsAndGroups: [
    'SUCCESSFUL_DATABASE_AUTHENTICATION_GROUP'
    'FAILED_DATABASE_AUTHENTICATION_GROUP'
    'BATCH_COMPLETED_GROUP'
  ]
}

// Allow the workspace to privately reach the target Azure resource without traversing the public internet.
resource managedPrivateEndpoint 'WorkspaceManagedPrivateEndpoint' = {
  workspaceId: workspace.id!
  name: 'sql-server-endpoint'
  targetPrivateLinkResourceId: targetPrivateLinkResourceId
  targetSubresourceType: 'sqlServer'
  requestMessage: 'Requested by Bicep workspace-security sample'
}

output workspaceId string? = workspace.id
output lakehouseId string? = lakehouse.id
output warehouseId string? = warehouse.id
output managedPrivateEndpointId string? = managedPrivateEndpoint.id
