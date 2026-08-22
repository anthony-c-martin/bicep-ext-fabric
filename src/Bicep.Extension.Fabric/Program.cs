using Microsoft.AspNetCore.Builder;
using Bicep.Local.Extension.Host.Extensions;
using Bicep.Extension.Fabric.Handlers;
using Azure.Bicep.Types.Concrete;
using Microsoft.Extensions.DependencyInjection;
using Bicep.Extension.Fabric;

var builder = WebApplication.CreateBuilder();

builder.AddBicepExtensionHost(args);
builder.Services
    .AddBicepExtension()
    .WithDefaults(
        name: ThisAssembly.AssemblyName.Split('-')[^1],
        version: ThisAssembly.AssemblyInformationalVersion.Split('+')[0],
        isSingleton: true)
    .WithTypeAssembly(typeof(Program).Assembly)
    .WithConfigurationType(typeof(Configuration))
    .WithResourceHandler<WorkspaceHandler>()
    .WithResourceHandler<AnomalyDetectorHandler>()
    .WithResourceHandler<ApacheAirflowJobHandler>()
    .WithResourceHandler<CopyJobHandler>()
    .WithResourceHandler<CosmosDBDatabaseHandler>()
    .WithResourceHandler<ConnectionHandler>()
    .WithResourceHandler<ConnectionRoleAssignmentHandler>()
    .WithResourceHandler<DashboardHandler>()
    .WithResourceHandler<DataAgentHandler>()
    .WithResourceHandler<DataflowHandler>()
    .WithResourceHandler<DatamartHandler>()
    .WithResourceHandler<DataPipelineHandler>()
    .WithResourceHandler<DeploymentPipelineHandler>()
    .WithResourceHandler<DeploymentPipelineRoleAssignmentHandler>()
    .WithResourceHandler<DigitalTwinBuilderHandler>()
    .WithResourceHandler<DigitalTwinBuilderFlowHandler>()
    .WithResourceHandler<DomainHandler>()
    .WithResourceHandler<DomainRoleAssignmentsHandler>()
    .WithResourceHandler<DomainWorkspaceAssignmentHandler>()
    .WithResourceHandler<EnvironmentHandler>()
    .WithResourceHandler<EventhouseHandler>()
    .WithResourceHandler<EventstreamHandler>()
    .WithResourceHandler<ExternalDataShareHandler>()
    .WithResourceHandler<FolderHandler>()
    .WithResourceHandler<GatewayHandler>()
    .WithResourceHandler<GatewayRoleAssignmentHandler>()
    .WithResourceHandler<GraphQLApiHandler>()
    .WithResourceHandler<ItemJobSchedulerHandler>()
    .WithResourceHandler<KQLDashboardHandler>()
    .WithResourceHandler<KQLDatabaseHandler>()
    .WithResourceHandler<KQLQuerysetHandler>()
    .WithResourceHandler<LakehouseHandler>()
    .WithResourceHandler<MapHandler>()
    .WithResourceHandler<MirroredCatalogHandler>()
    .WithResourceHandler<MirroredDatabaseHandler>()
    .WithResourceHandler<MirroredWarehouseHandler>()
    .WithResourceHandler<MLExperimentHandler>()
    .WithResourceHandler<MLModelHandler>()
    .WithResourceHandler<MountedDataFactoryHandler>()
    .WithResourceHandler<NotebookHandler>()
    .WithResourceHandler<OneLakeDataAccessSecurityHandler>()
    .WithResourceHandler<OntologyHandler>()
    .WithResourceHandler<OperationsAgentHandler>()
    .WithResourceHandler<PaginatedReportHandler>()
    .WithResourceHandler<ReflexHandler>()
    .WithResourceHandler<ReportHandler>()
    .WithResourceHandler<SemanticModelHandler>()
    .WithResourceHandler<ShortcutHandler>()
    .WithResourceHandler<SparkCustomPoolHandler>()
    .WithResourceHandler<SparkEnvironmentSettingsHandler>()
    .WithResourceHandler<SparkJobDefinitionHandler>()
    .WithResourceHandler<SparkWorkspaceSettingsHandler>()
    .WithResourceHandler<SQLDatabaseHandler>()
    .WithResourceHandler<TagHandler>()
    .WithResourceHandler<TenantSettingHandler>()
    .WithResourceHandler<VariableLibraryHandler>()
    .WithResourceHandler<WarehouseHandler>()
    .WithResourceHandler<WarehouseSnapshotHandler>()
    .WithResourceHandler<WarehouseSqlAuditSettingsHandler>()
    .WithResourceHandler<WorkspaceGitOutboundPolicyHandler>()
    .WithResourceHandler<WorkspaceGitHandler>()
    .WithResourceHandler<WorkspaceManagedPrivateEndpointHandler>()
    .WithResourceHandler<WorkspaceNetworkCommunicationPolicyHandler>()
    .WithResourceHandler<WorkspaceOutboundCloudConnectionRulesHandler>()
    .WithResourceHandler<WorkspaceOutboundGatewayRulesHandler>()
    .WithResourceHandler<WorkspaceRoleAssignmentHandler>();

var app = builder.Build();
app.MapBicepExtension();

await app.RunAsync();