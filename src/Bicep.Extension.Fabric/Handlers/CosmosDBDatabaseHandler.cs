using Microsoft.Fabric.Api.Core.Models;

namespace Bicep.Extension.Fabric.Handlers;

public sealed class CosmosDBDatabaseHandler : FabricItemHandlerBase<CosmosDBDatabase>
{
    protected override ItemType FabricItemType => ItemType.CosmosDBDatabase;
}