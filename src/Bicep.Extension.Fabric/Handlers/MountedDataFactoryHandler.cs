using Microsoft.Fabric.Api.Core.Models;

namespace Bicep.Extension.Fabric.Handlers;

public sealed class MountedDataFactoryHandler : FabricItemHandlerBase<MountedDataFactory>
{
    protected override ItemType FabricItemType => ItemType.MountedDataFactory;
}