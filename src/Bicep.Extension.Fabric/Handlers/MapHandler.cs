using Microsoft.Fabric.Api.Core.Models;

namespace Bicep.Extension.Fabric.Handlers;

public sealed class MapHandler : FabricItemHandlerBase<Map>
{
    protected override ItemType FabricItemType => ItemType.Map;
}