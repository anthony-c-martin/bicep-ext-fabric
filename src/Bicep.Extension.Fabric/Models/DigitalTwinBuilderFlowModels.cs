using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace Bicep.Extension.Fabric.Models;

[ResourceType("DigitalTwinBuilderFlow")]
public class DigitalTwinBuilderFlow : FabricItem
{
    [TypeProperty("The Digital Twin Builder Flow creation configuration. Changing this forces recreation of the flow. Required unless the flow is created from a definition.")]
    public DigitalTwinBuilderFlowConfiguration? Configuration { get; set; }
}

public class DigitalTwinBuilderFlowConfiguration
{
    [TypeProperty("The Digital Twin Builder item that this flow belongs to", ObjectTypePropertyFlags.Required)]
    public required DigitalTwinBuilderItemReference DigitalTwinBuilderItemReference { get; set; }
}

public class DigitalTwinBuilderItemReference
{
    [TypeProperty("The workspace that contains the Digital Twin Builder item", ObjectTypePropertyFlags.Required)]
    public required string WorkspaceId { get; set; }

    [TypeProperty("The Digital Twin Builder item ID", ObjectTypePropertyFlags.Required)]
    public required string ItemId { get; set; }
}
