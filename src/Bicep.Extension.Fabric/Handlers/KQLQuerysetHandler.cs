using Microsoft.Fabric.Api.Core.Models;

namespace Bicep.Extension.Fabric.Handlers;

public sealed class KQLQuerysetHandler : FabricItemHandlerBase<KQLQueryset>
{
    protected override ItemType FabricItemType => ItemType.KQLQueryset;
}