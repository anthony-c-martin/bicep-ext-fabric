using 'main.bicep'

param fabricToken = readEnvironmentVariable('FABRIC_TOKEN')
param workspaceName = 'bicep-fabric-workspace'
param lakehouseName = 'bicep-fabric-lakehouse'
