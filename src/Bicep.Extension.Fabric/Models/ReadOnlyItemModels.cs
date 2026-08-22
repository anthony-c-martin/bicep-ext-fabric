using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace Bicep.Extension.Fabric.Models;

public class FabricReadOnlyItemIdentifiers
{
    [TypeProperty("The containing Fabric workspace ID", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public string WorkspaceId { get; set; } = string.Empty;

    [TypeProperty("The Fabric item ID", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public string Id { get; set; } = string.Empty;
}

/// <summary>
/// Base for item types that the Fabric API only exposes for listing and reading. They can be
/// referenced from Bicep with an <c>existing</c> resource, but cannot be created, updated or deleted.
/// </summary>
public class FabricReadOnlyItem : FabricReadOnlyItemIdentifiers
{
    [TypeProperty("The item display name", ObjectTypePropertyFlags.ReadOnly)]
    public string? DisplayName { get; set; }

    [TypeProperty("The item description", ObjectTypePropertyFlags.ReadOnly)]
    public string? Description { get; set; }

    [TypeProperty("The folder that contains the item", ObjectTypePropertyFlags.ReadOnly)]
    public string? FolderId { get; set; }

    [TypeProperty("The IDs of the tags applied to the item", ObjectTypePropertyFlags.ReadOnly)]
    public string[]? Tags { get; set; }

    [TypeProperty("The Fabric item type", ObjectTypePropertyFlags.ReadOnly)]
    public string? Type { get; set; }
}

// The Fabric API only exposes a list operation for these item types, so they cannot be provisioned
// from Bicep. The Terraform provider models them as data sources for the same reason.
[ResourceType("Dashboard")]
public class Dashboard : FabricReadOnlyItem;

[ResourceType("Datamart")]
public class Datamart : FabricReadOnlyItem;

[ResourceType("MirroredWarehouse")]
public class MirroredWarehouse : FabricReadOnlyItem;
