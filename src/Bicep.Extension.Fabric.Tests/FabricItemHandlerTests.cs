using System.Text.Json;
using Bicep.Extension.Fabric.Handlers;
using Bicep.Local.Extension.Host.Handlers;

namespace Bicep.Extension.Fabric.Tests;

[TestClass]
public sealed class FabricItemHandlerTests
{
    public static IEnumerable<object[]> ItemHandlers
    {
        get
        {
            yield return [typeof(AnomalyDetectorHandler), "AnomalyDetector"];
            yield return [typeof(ApacheAirflowJobHandler), "ApacheAirflowJob"];
            yield return [typeof(CopyJobHandler), "CopyJob"];
            yield return [typeof(CosmosDBDatabaseHandler), "CosmosDBDatabase"];
            yield return [typeof(DataAgentHandler), "DataAgent"];
            yield return [typeof(DataflowHandler), "Dataflow"];
            yield return [typeof(DataPipelineHandler), "DataPipeline"];
            yield return [typeof(DigitalTwinBuilderHandler), "DigitalTwinBuilder"];
            yield return [typeof(DigitalTwinBuilderFlowHandler), "DigitalTwinBuilderFlow"];
            yield return [typeof(EnvironmentHandler), "Environment"];
            yield return [typeof(EventhouseHandler), "Eventhouse"];
            yield return [typeof(EventstreamHandler), "Eventstream"];
            yield return [typeof(GraphQLApiHandler), "GraphQLApi"];
            yield return [typeof(KQLDashboardHandler), "KQLDashboard"];
            yield return [typeof(KQLDatabaseHandler), "KQLDatabase"];
            yield return [typeof(KQLQuerysetHandler), "KQLQueryset"];
            yield return [typeof(LakehouseHandler), "Lakehouse"];
            yield return [typeof(MapHandler), "Map"];
            yield return [typeof(MirroredCatalogHandler), "MirroredCatalog"];
            yield return [typeof(MirroredDatabaseHandler), "MirroredDatabase"];
            yield return [typeof(MLExperimentHandler), "MLExperiment"];
            yield return [typeof(MLModelHandler), "MLModel"];
            yield return [typeof(MountedDataFactoryHandler), "MountedDataFactory"];
            yield return [typeof(NotebookHandler), "Notebook"];
            yield return [typeof(OntologyHandler), "Ontology"];
            yield return [typeof(OperationsAgentHandler), "OperationsAgent"];
            yield return [typeof(PaginatedReportHandler), "PaginatedReport"];
            yield return [typeof(ReflexHandler), "Reflex"];
            yield return [typeof(ReportHandler), "Report"];
            yield return [typeof(SemanticModelHandler), "SemanticModel"];
            yield return [typeof(SparkJobDefinitionHandler), "SparkJobDefinition"];
            yield return [typeof(SQLDatabaseHandler), "SQLDatabase"];
            yield return [typeof(VariableLibraryHandler), "VariableLibrary"];
            yield return [typeof(WarehouseHandler), "Warehouse"];
            yield return [typeof(WarehouseSnapshotHandler), "WarehouseSnapshot"];
        }
    }

    /// <summary>
    /// Item types whose model declares a required <c>configuration</c>, so that the shared preview
    /// test can supply a valid value for it.
    /// </summary>
    private static object? RequiredConfiguration(string resourceType) => resourceType switch
    {
        "Lakehouse" => new { enableSchemas = true },
        "WarehouseSnapshot" => new { parentWarehouseId = "11111111-1111-1111-1111-111111111111" },
        _ => null,
    };

    /// <summary>
    /// Item types that the Fabric API only exposes for listing and reading.
    /// </summary>
    public static IEnumerable<object[]> ReadOnlyItemHandlers
    {
        get
        {
            yield return [typeof(DashboardHandler), "Dashboard"];
            yield return [typeof(DatamartHandler), "Datamart"];
            yield return [typeof(MirroredWarehouseHandler), "MirroredWarehouse"];
        }
    }

    [TestMethod]
    public async Task Workspace_preview_uses_camel_case_properties()
    {
        var handler = new WorkspaceHandler();
        var capacityId = Guid.NewGuid();

        var response = await HandlerHarness.PreviewAsync(handler, "Workspace", new
        {
            displayName = "Analytics",
            description = "Fabric analytics workspace",
            capacityId,
        });

        var properties = response.ResourceProperties();
        Assert.AreEqual("Analytics", properties.GetProperty("displayName").GetString());
        Assert.AreEqual(capacityId, properties.GetProperty("capacityId").GetGuid());
        Assert.IsFalse(properties.TryGetProperty("DisplayName", out _));
    }

    [TestMethod]
    public async Task Item_preview_preserves_definition_parts()
    {
        var handler = new NotebookHandler();
        var workspaceId = Guid.NewGuid();

        var response = await HandlerHarness.PreviewAsync(handler, "Notebook", new
        {
            workspaceId,
            displayName = "Ingest data",
            definition = new
            {
                format = "ipynb",
                parts = new[]
                {
                    new
                    {
                        path = "notebook-content.py",
                        payload = Convert.ToBase64String("print('hello')"u8.ToArray()),
                    },
                },
            },
        });

        var properties = response.ResourceProperties();
        Assert.AreEqual(workspaceId, properties.GetProperty("workspaceId").GetGuid());
        Assert.AreEqual("notebook-content.py", properties.GetProperty("definition").GetProperty("parts")[0].GetProperty("path").GetString());
    }

    [TestMethod]
    [DynamicData(nameof(ItemHandlers))]
    public async Task Item_handler_previews_its_resource_type(Type handlerType, string resourceType)
    {
        var handler = (IResourceHandler)Activator.CreateInstance(handlerType)!;
        var workspaceId = Guid.NewGuid();
        var folderId = Guid.NewGuid();
        var tagId = Guid.NewGuid();
        var configuration = RequiredConfiguration(resourceType);

        var propertiesInput = configuration is null
            ? (object)new
            {
                workspaceId,
                displayName = $"Test {resourceType}",
                description = $"Preview for {resourceType}",
                folderId,
                tags = new[] { tagId },
            }
            : new
            {
                workspaceId,
                displayName = $"Test {resourceType}",
                description = $"Preview for {resourceType}",
                folderId,
                tags = new[] { tagId },
                configuration,
            };

        var response = await HandlerHarness.PreviewAsync(handler, resourceType, propertiesInput);

        var properties = response.ResourceProperties();
        Assert.AreEqual(resourceType, handler.Type);
        Assert.AreEqual(resourceType, ((IFabricItemHandlerMetadata)handler).ItemTypeName);
        Assert.AreEqual(workspaceId, properties.GetProperty("workspaceId").GetGuid());
        Assert.AreEqual($"Test {resourceType}", properties.GetProperty("displayName").GetString());
        Assert.AreEqual(folderId, properties.GetProperty("folderId").GetGuid());
        Assert.AreEqual(tagId, properties.GetProperty("tags")[0].GetGuid());
        Assert.IsFalse(properties.TryGetProperty("WorkspaceId", out _));

        if (resourceType == "Lakehouse")
        {
            Assert.IsTrue(properties.GetProperty("configuration").GetProperty("enableSchemas").GetBoolean());
        }
    }

    [TestMethod]
    [DynamicData(nameof(ReadOnlyItemHandlers))]
    public async Task ReadOnly_item_handler_previews_identifiers_only(Type handlerType, string resourceType)
    {
        var handler = (IResourceHandler)Activator.CreateInstance(handlerType)!;
        var workspaceId = Guid.NewGuid();
        var id = Guid.NewGuid();

        var response = await HandlerHarness.PreviewAsync(handler, resourceType, new { workspaceId, id });

        var properties = response.ResourceProperties();
        Assert.AreEqual(resourceType, handler.Type);
        Assert.AreEqual(resourceType, ((IFabricItemHandlerMetadata)handler).ItemTypeName);
        Assert.AreEqual(workspaceId, properties.GetProperty("workspaceId").GetGuid());
        Assert.AreEqual(id, properties.GetProperty("id").GetGuid());
    }

    [TestMethod]
    [DynamicData(nameof(ReadOnlyItemHandlers))]
    public async Task ReadOnly_item_handler_rejects_create_or_update(Type handlerType, string resourceType)
    {
        var handler = (IResourceHandler)Activator.CreateInstance(handlerType)!;

        var response = await HandlerHarness.CreateOrUpdateAsync(
            handler,
            resourceType,
            new { workspaceId = Guid.NewGuid(), id = Guid.NewGuid() });

        Assert.IsNotNull(response.ErrorData);
        StringAssert.Contains(response.ErrorData.Error.Message, "does not support creating or updating");
    }

    [TestMethod]
    public void Item_handler_test_matrix_covers_every_concrete_item_handler()
    {
        var testedHandlerTypes = ItemHandlers
            .Concat(ReadOnlyItemHandlers)
            .Select(row => (Type)row[0])
            .ToHashSet();

        var concreteHandlerTypes = typeof(WorkspaceHandler).Assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } &&
                type.Namespace == typeof(WorkspaceHandler).Namespace &&
                type != typeof(WorkspaceHandler) &&
                !NonItemHandlerTypes.Contains(type) &&
                typeof(IResourceHandler).IsAssignableFrom(type))
            .ToHashSet();

        CollectionAssert.AreEquivalent(concreteHandlerTypes.ToArray(), testedHandlerTypes.ToArray());
    }

    // Handlers for resources that are not Fabric work items (no IFabricItemHandlerMetadata / WorkspaceId+Id shape)
    // and therefore have their own dedicated preview tests below instead of the generic item-handler matrix.
    private static readonly HashSet<Type> NonItemHandlerTypes =
    [
        typeof(FolderHandler),
        typeof(DomainHandler),
        typeof(TagHandler),
        typeof(DeploymentPipelineHandler),
        typeof(WorkspaceRoleAssignmentHandler),
        typeof(DeploymentPipelineRoleAssignmentHandler),
        typeof(DomainWorkspaceAssignmentHandler),
        typeof(WorkspaceGitOutboundPolicyHandler),
        typeof(WorkspaceOutboundGatewayRulesHandler),
        typeof(WorkspaceOutboundCloudConnectionRulesHandler),
        typeof(WorkspaceNetworkCommunicationPolicyHandler),
        typeof(WorkspaceManagedPrivateEndpointHandler),
        typeof(OneLakeDataAccessSecurityHandler),
        typeof(SparkWorkspaceSettingsHandler),
        typeof(WarehouseSqlAuditSettingsHandler),
        typeof(ConnectionHandler),
        typeof(ConnectionRoleAssignmentHandler),
        typeof(GatewayHandler),
        typeof(GatewayRoleAssignmentHandler),
        typeof(ShortcutHandler),
        typeof(ItemJobSchedulerHandler),
        typeof(SparkCustomPoolHandler),
        typeof(SparkEnvironmentSettingsHandler),
        typeof(ExternalDataShareHandler),
        typeof(TenantSettingHandler),
        typeof(WorkspaceGitHandler),
        typeof(DomainRoleAssignmentsHandler),
    ];
}
