using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace Bicep.Extension.Fabric.Models;

[ResourceType("WarehouseSnapshot")]
public class WarehouseSnapshot : FabricItem
{
    // Warehouse Snapshot items do not support the Fabric item definition API.
    [TypeProperty("Not supported for Warehouse Snapshot items.", ObjectTypePropertyFlags.ReadOnly)]
    public new FabricItemDefinition? Definition { get; set; }

    [TypeProperty("The Warehouse Snapshot creation configuration", ObjectTypePropertyFlags.Required)]
    public required WarehouseSnapshotConfiguration Configuration { get; set; }

    [TypeProperty("The SQL connection string connected to the workspace containing this Warehouse Snapshot", ObjectTypePropertyFlags.ReadOnly)]
    public string? ConnectionString { get; set; }
}

public class WarehouseSnapshotConfiguration
{
    [TypeProperty("The Warehouse that this snapshot is taken from. Changing this forces recreation of the Warehouse Snapshot.", ObjectTypePropertyFlags.Required)]
    public required string ParentWarehouseId { get; set; }

    [TypeProperty("The point in time the snapshot represents, as an ISO 8601 timestamp. Defaults to the current time when not specified.")]
    public string? SnapshotDateTime { get; set; }
}
