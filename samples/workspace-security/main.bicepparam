using 'main.bicep'

param fabricToken = readEnvironmentVariable('FABRIC_TOKEN')
param workspaceName = 'bicep-fabric-workspace-security'
param principalId = readEnvironmentVariable('FABRIC_PRINCIPAL_ID')
param tenantId = readEnvironmentVariable('FABRIC_TENANT_ID')
param targetPrivateLinkResourceId = readEnvironmentVariable('FABRIC_TARGET_PRIVATE_LINK_RESOURCE_ID')
