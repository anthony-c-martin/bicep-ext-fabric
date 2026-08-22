using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace Bicep.Extension.Fabric.Models;

[ResourceType("Warehouse")]
public class Warehouse : FabricItem
{
    // Warehouse items do not support the Fabric item definition API.
    [TypeProperty("Not supported for Warehouse items.", ObjectTypePropertyFlags.ReadOnly)]
    public new FabricItemDefinition? Definition { get; set; }

    [TypeProperty("The Warehouse creation configuration. Changing this forces recreation of the Warehouse.")]
    public WarehouseConfiguration? Configuration { get; set; }

    [TypeProperty("The SQL connection string connected to the workspace containing this warehouse", ObjectTypePropertyFlags.ReadOnly)]
    public string? ConnectionString { get; set; }

    [TypeProperty("The date and time the warehouse was created", ObjectTypePropertyFlags.ReadOnly)]
    public string? CreatedDate { get; set; }

    [TypeProperty("The date and time the warehouse was last updated", ObjectTypePropertyFlags.ReadOnly)]
    public string? LastUpdatedTime { get; set; }

    [TypeProperty("The collation type of the warehouse", ObjectTypePropertyFlags.ReadOnly)]
    public CollationType? CollationType { get; set; }
}

public class WarehouseConfiguration
{
    [TypeProperty("The collation type of the warehouse", ObjectTypePropertyFlags.Required)]
    public CollationType CollationType { get; set; }
}

public enum CollationType
{
    Latin1_General_100_BIN2_UTF8,
    Latin1_General_100_CI_AS_KS_WS_SC_UTF8,
}
