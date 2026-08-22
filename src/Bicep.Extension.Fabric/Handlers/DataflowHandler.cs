using Microsoft.Fabric.Api.Core.Models;

namespace Bicep.Extension.Fabric.Handlers;

public sealed class DataflowHandler : FabricItemHandlerBase<Dataflow>
{
    protected override ItemType FabricItemType => ItemType.Dataflow;
}