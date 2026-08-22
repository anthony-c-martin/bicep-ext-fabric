using Microsoft.Fabric.Api.Core.Models;

namespace Bicep.Extension.Fabric.Handlers;

public sealed class EventstreamHandler : FabricItemHandlerBase<Eventstream>
{
    protected override ItemType FabricItemType => ItemType.Eventstream;
}