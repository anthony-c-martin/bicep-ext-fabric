using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace Bicep.Extension.Fabric.Models;

[ResourceType("Eventhouse")]
public class Eventhouse : FabricItem
{
    [TypeProperty("The Eventhouse creation configuration. Changing this forces recreation of the Eventhouse. Cannot be used together with definition.")]
    public EventhouseConfiguration? Configuration { get; set; }

    [TypeProperty("The query service URI", ObjectTypePropertyFlags.ReadOnly)]
    public string? QueryServiceUri { get; set; }

    [TypeProperty("The ingestion service URI", ObjectTypePropertyFlags.ReadOnly)]
    public string? IngestionServiceUri { get; set; }

    [TypeProperty("The IDs of all KQL Database children of this Eventhouse", ObjectTypePropertyFlags.ReadOnly)]
    public string[]? DatabaseIds { get; set; }

    [TypeProperty("The minimum consumption units for the Eventhouse", ObjectTypePropertyFlags.ReadOnly)]
    public string? MinimumConsumptionUnits { get; set; }
}

public class EventhouseConfiguration
{
    [TypeProperty("The minimum consumption units for the Eventhouse. Accepted values: 0, 13, 18, 2.25, 26, 34, 4.25, 50, 8.5, or any number between 51 and 322.", ObjectTypePropertyFlags.Required)]
    public required string MinimumConsumptionUnits { get; set; }
}
