using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace Bicep.Extension.Fabric.Models;

public enum AuditSettingsState
{
    Enabled,
    Disabled,
}

public class WarehouseSqlAuditSettingsIdentifiers
{
    [TypeProperty("The containing Fabric workspace ID", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public string WorkspaceId { get; set; } = string.Empty;

    [TypeProperty("The Warehouse ID", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public string WarehouseId { get; set; } = string.Empty;
}

// Deletion is not supported by the Fabric API; the settings remain unchanged in Fabric when the resource is removed from a Bicep deployment.
[ResourceType("WarehouseSqlAuditSettings")]
public class WarehouseSqlAuditSettings : WarehouseSqlAuditSettingsIdentifiers
{
    [TypeProperty("The audit settings state. Defaults to 'Disabled' when not specified")]
    public AuditSettingsState? State { get; set; }

    [TypeProperty("The retention period in days. '0' indicates an indefinite retention period. Defaults to '0' when not specified")]
    public int? RetentionDays { get; set; }

    [TypeProperty("The audit actions and groups to capture. Defaults to the standard database authentication and batch completion groups when not specified")]
    public string[]? AuditActionsAndGroups { get; set; }
}
