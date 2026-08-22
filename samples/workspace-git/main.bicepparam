using 'main.bicep'

param fabricToken = readEnvironmentVariable('FABRIC_TOKEN')
param workspaceName = 'bicep-fabric-git'
param organizationName = readEnvironmentVariable('FABRIC_GIT_ORGANIZATION')
param projectName = readEnvironmentVariable('FABRIC_GIT_PROJECT')
param repositoryName = readEnvironmentVariable('FABRIC_GIT_REPOSITORY')
param branchName = 'main'
param directoryName = '/fabric'
param gitConnectionId = readEnvironmentVariable('FABRIC_GIT_CONNECTION_ID')
