using Microsoft.Fabric.Api.Core.Models;

namespace Bicep.Extension.Fabric.Handlers;

public sealed class GraphQLApiHandler : FabricItemHandlerBase<GraphQLApi>
{
    protected override ItemType FabricItemType => ItemType.GraphQLApi;
}