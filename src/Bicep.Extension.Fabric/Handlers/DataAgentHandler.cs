using Microsoft.Fabric.Api.Core.Models;

namespace Bicep.Extension.Fabric.Handlers;

public sealed class DataAgentHandler : FabricItemHandlerBase<DataAgent>
{
    protected override ItemType FabricItemType => ItemType.DataAgent;
}