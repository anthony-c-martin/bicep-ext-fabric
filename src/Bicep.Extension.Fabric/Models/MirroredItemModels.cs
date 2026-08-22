using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace Bicep.Extension.Fabric.Models;

public class FabricSqlEndpointProperties
{
    [TypeProperty("The SQL endpoint ID", ObjectTypePropertyFlags.ReadOnly)]
    public string? Id { get; set; }

    [TypeProperty("The SQL endpoint connection string", ObjectTypePropertyFlags.ReadOnly)]
    public string? ConnectionString { get; set; }

    [TypeProperty("The SQL endpoint provisioning status", ObjectTypePropertyFlags.ReadOnly)]
    public string? ProvisioningStatus { get; set; }
}

[ResourceType("MirroredDatabase")]
public class MirroredDatabase : FabricItem
{
    [TypeProperty("The default schema of the mirrored database", ObjectTypePropertyFlags.ReadOnly)]
    public string? DefaultSchema { get; set; }

    [TypeProperty("OneLake path to the mirrored database tables directory", ObjectTypePropertyFlags.ReadOnly)]
    public string? OneLakeTablesPath { get; set; }

    [TypeProperty("Properties of the mirrored database SQL endpoint", ObjectTypePropertyFlags.ReadOnly)]
    public FabricSqlEndpointProperties? SqlEndpointProperties { get; set; }
}

[ResourceType("MirroredCatalog")]
public class MirroredCatalog : FabricItem
{
    [TypeProperty("The type of the mirrored catalog source", ObjectTypePropertyFlags.ReadOnly)]
    public string? SourceType { get; set; }

    [TypeProperty("The connection used to reach the mirrored catalog source", ObjectTypePropertyFlags.ReadOnly)]
    public string? ConnectionId { get; set; }

    [TypeProperty("The scope of the mirrored catalog", ObjectTypePropertyFlags.ReadOnly)]
    public string[]? Scope { get; set; }

    [TypeProperty("OneLake path to the mirrored catalog tables directory", ObjectTypePropertyFlags.ReadOnly)]
    public string? OneLakeTablesPath { get; set; }

    [TypeProperty("Properties of the mirrored catalog SQL endpoint", ObjectTypePropertyFlags.ReadOnly)]
    public FabricSqlEndpointProperties? SqlEndpointProperties { get; set; }
}

[ResourceType("OperationsAgent")]
public class OperationsAgent : FabricItem
{
    [TypeProperty("The state of the Operations Agent", ObjectTypePropertyFlags.ReadOnly)]
    public string? State { get; set; }
}

[ResourceType("VariableLibrary")]
public class VariableLibrary : FabricItem
{
    [TypeProperty("The name of the active value set of the Variable Library")]
    public string? ActiveValueSetName { get; set; }
}
