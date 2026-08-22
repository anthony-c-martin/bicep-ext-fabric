using Microsoft.Fabric.Api.Core.Models;

namespace Bicep.Extension.Fabric.Handlers;

public sealed class SemanticModelHandler : FabricItemHandlerBase<SemanticModel>
{
    protected override ItemType FabricItemType => ItemType.SemanticModel;
}