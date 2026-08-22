using Microsoft.Fabric.Api.Core.Models;

namespace Bicep.Extension.Fabric.Handlers;

public sealed class CopyJobHandler : FabricItemHandlerBase<CopyJob>
{
    protected override ItemType FabricItemType => ItemType.CopyJob;
}