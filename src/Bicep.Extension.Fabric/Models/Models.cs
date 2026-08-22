using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace Bicep.Extension.Fabric.Models;

public class WorkspaceIdentifiers
{
    [TypeProperty("The Fabric workspace ID", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.ReadOnly)]
    public string? Id { get; set; }
}

[ResourceType("Workspace")]
public class Workspace : WorkspaceIdentifiers
{
    [TypeProperty("The workspace display name", ObjectTypePropertyFlags.Required)]
    public required string DisplayName { get; set; }

    [TypeProperty("The workspace description")]
    public string? Description { get; set; }

    [TypeProperty("The capacity assigned to the workspace")]
    public string? CapacityId { get; set; }

    [TypeProperty("The domain assigned to the workspace")]
    public string? DomainId { get; set; }

    [TypeProperty("The workspace identity. Set this to provision a system-assigned identity for the workspace.")]
    public WorkspaceManagedIdentity? Identity { get; set; }

    [TypeProperty("The workspace type", ObjectTypePropertyFlags.ReadOnly)]
    public string? Type { get; set; }

    [TypeProperty("The workspace API endpoint", ObjectTypePropertyFlags.ReadOnly)]
    public string? ApiEndpoint { get; set; }

    [TypeProperty("The region of the capacity associated with the workspace", ObjectTypePropertyFlags.ReadOnly)]
    public string? CapacityRegion { get; set; }

    [TypeProperty("The progress of the workspace assignment to a capacity", ObjectTypePropertyFlags.ReadOnly)]
    public string? CapacityAssignmentProgress { get; set; }

    [TypeProperty("The OneLake API endpoints associated with the workspace", ObjectTypePropertyFlags.ReadOnly)]
    public WorkspaceOneLakeEndpoints? OneLakeEndpoints { get; set; }
}

public enum WorkspaceIdentityType
{
    SystemAssigned,
}

public class WorkspaceManagedIdentity
{
    [TypeProperty("The identity type", ObjectTypePropertyFlags.Required)]
    public required WorkspaceIdentityType Type { get; set; }

    [TypeProperty("The application ID of the workspace identity", ObjectTypePropertyFlags.ReadOnly)]
    public string? ApplicationId { get; set; }

    [TypeProperty("The service principal ID of the workspace identity", ObjectTypePropertyFlags.ReadOnly)]
    public string? ServicePrincipalId { get; set; }
}

public class WorkspaceOneLakeEndpoints
{
    [TypeProperty("The OneLake Blob API endpoint", ObjectTypePropertyFlags.ReadOnly)]
    public string? BlobEndpoint { get; set; }

    [TypeProperty("The OneLake DFS API endpoint", ObjectTypePropertyFlags.ReadOnly)]
    public string? DfsEndpoint { get; set; }
}

public class FabricItemIdentifiers
{
    [TypeProperty("The containing Fabric workspace ID", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public string WorkspaceId { get; set; } = string.Empty;

    [TypeProperty("The Fabric item ID", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.ReadOnly)]
    public string? Id { get; set; }
}

public class FabricItem : FabricItemIdentifiers
{
    [TypeProperty("The item display name", ObjectTypePropertyFlags.Required)]
    public string DisplayName { get; set; } = string.Empty;

    [TypeProperty("The item description")]
    public string? Description { get; set; }

    [TypeProperty("The folder that contains the item")]
    public string? FolderId { get; set; }

    [TypeProperty("The IDs of the tags applied to the item. Tags applied outside of Bicep are left untouched when this is not set.")]
    public string[]? Tags { get; set; }

    [TypeProperty("The item definition")]
    public FabricItemDefinition? Definition { get; set; }

    [TypeProperty("The Fabric item type", ObjectTypePropertyFlags.ReadOnly)]
    public string? Type { get; set; }
}

public class FabricItemDefinition
{
    [TypeProperty("The definition format")]
    public string? Format { get; set; }

    [TypeProperty("The item definition parts", ObjectTypePropertyFlags.Required)]
    public required FabricItemDefinitionPart[] Parts { get; set; }
}

public class FabricItemDefinitionPart
{
    [TypeProperty("The definition part path", ObjectTypePropertyFlags.Required)]
    public required string Path { get; set; }

    [TypeProperty("The base64-encoded definition part payload", ObjectTypePropertyFlags.Required)]
    public required string Payload { get; set; }
}

[ResourceType("AnomalyDetector")]
public class AnomalyDetector : FabricItem;

[ResourceType("ApacheAirflowJob")]
public class ApacheAirflowJob : FabricItem;

[ResourceType("CopyJob")]
public class CopyJob : FabricItem;

[ResourceType("CosmosDBDatabase")]
public class CosmosDBDatabase : FabricItem;

[ResourceType("DataAgent")]
public class DataAgent : FabricItem;

[ResourceType("Dataflow")]
public class Dataflow : FabricItem;

[ResourceType("DataPipeline")]
public class DataPipeline : FabricItem;

[ResourceType("DigitalTwinBuilder")]
public class DigitalTwinBuilder : FabricItem;

[ResourceType("Eventstream")]
public class Eventstream : FabricItem;

[ResourceType("GraphQLApi")]
public class GraphQLApi : FabricItem;

[ResourceType("KQLDashboard")]
public class KQLDashboard : FabricItem;

[ResourceType("KQLQueryset")]
public class KQLQueryset : FabricItem;

[ResourceType("Lakehouse")]
public class Lakehouse : FabricItem
{
    [TypeProperty("Lakehouse creation configuration", ObjectTypePropertyFlags.Required)]
    public required LakehouseConfiguration Configuration { get; set; }

    [TypeProperty("OneLake path to the Lakehouse files directory", ObjectTypePropertyFlags.ReadOnly)]
    public string? OneLakeFilesPath { get; set; }

    [TypeProperty("OneLake path to the Lakehouse tables directory", ObjectTypePropertyFlags.ReadOnly)]
    public string? OneLakeTablesPath { get; set; }

    [TypeProperty("The default schema for a schema-enabled Lakehouse", ObjectTypePropertyFlags.ReadOnly)]
    public string? DefaultSchema { get; set; }

    [TypeProperty("Properties of the Lakehouse SQL endpoint", ObjectTypePropertyFlags.ReadOnly)]
    public LakehouseSqlEndpointProperties? SqlEndpointProperties { get; set; }
}

public class LakehouseConfiguration
{
    [TypeProperty("Whether schemas are enabled for the Lakehouse", ObjectTypePropertyFlags.Required)]
    public bool EnableSchemas { get; set; }
}

public class LakehouseSqlEndpointProperties
{
    [TypeProperty("The SQL endpoint ID", ObjectTypePropertyFlags.ReadOnly)]
    public string? Id { get; set; }

    [TypeProperty("The SQL endpoint connection string", ObjectTypePropertyFlags.ReadOnly)]
    public string? ConnectionString { get; set; }

    [TypeProperty("The SQL endpoint provisioning status", ObjectTypePropertyFlags.ReadOnly)]
    public string? ProvisioningStatus { get; set; }
}

[ResourceType("Map")]
public class Map : FabricItem;

[ResourceType("MLExperiment")]
public class MLExperiment : FabricItem
{
    // MLExperiment items do not support the Fabric item definition API.
    [TypeProperty("Not supported for MLExperiment items.", ObjectTypePropertyFlags.ReadOnly)]
    public new FabricItemDefinition? Definition { get; set; }
}

[ResourceType("MLModel")]
public class MLModel : FabricItem
{
    // MLModel items do not support the Fabric item definition API.
    [TypeProperty("Not supported for MLModel items.", ObjectTypePropertyFlags.ReadOnly)]
    public new FabricItemDefinition? Definition { get; set; }
}

[ResourceType("MountedDataFactory")]
public class MountedDataFactory : FabricItem;

[ResourceType("Notebook")]
public class Notebook : FabricItem;

[ResourceType("Ontology")]
public class Ontology : FabricItem;

[ResourceType("PaginatedReport")]
public class PaginatedReport : FabricItem;

[ResourceType("Reflex")]
public class Reflex : FabricItem;

[ResourceType("Report")]
public class Report : FabricItem;

[ResourceType("SemanticModel")]
public class SemanticModel : FabricItem;

public class Configuration
{
    [TypeProperty("A Microsoft Entra access token for the Fabric API", ObjectTypePropertyFlags.Required, isSecure: true)]
    public required string AccessToken { get; set; }
}