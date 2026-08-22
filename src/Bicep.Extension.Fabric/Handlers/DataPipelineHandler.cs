using Microsoft.Fabric.Api.Core.Models;

namespace Bicep.Extension.Fabric.Handlers;

public sealed class DataPipelineHandler : FabricItemHandlerBase<DataPipeline>
{
    protected override ItemType FabricItemType => ItemType.DataPipeline;
}