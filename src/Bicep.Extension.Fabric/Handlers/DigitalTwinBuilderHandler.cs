using Microsoft.Fabric.Api.Core.Models;

namespace Bicep.Extension.Fabric.Handlers;

public sealed class DigitalTwinBuilderHandler : FabricItemHandlerBase<DigitalTwinBuilder>
{
    protected override ItemType FabricItemType => ItemType.DigitalTwinBuilder;
}