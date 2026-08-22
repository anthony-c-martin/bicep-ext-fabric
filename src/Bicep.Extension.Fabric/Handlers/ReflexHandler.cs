using Microsoft.Fabric.Api.Core.Models;

namespace Bicep.Extension.Fabric.Handlers;

public sealed class ReflexHandler : FabricItemHandlerBase<Reflex>
{
    protected override ItemType FabricItemType => ItemType.Reflex;
}