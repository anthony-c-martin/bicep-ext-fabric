using 'main.bicep'

param fabricToken = readEnvironmentVariable('FABRIC_TOKEN')
param workspaceName = 'bicep-fabric-governance'
param domainId = readEnvironmentVariable('FABRIC_DOMAIN_ID')
param principalId = readEnvironmentVariable('FABRIC_PRINCIPAL_ID')
