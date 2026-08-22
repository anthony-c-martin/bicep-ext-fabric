using Microsoft.Fabric.Api.Core.Models;

namespace Bicep.Extension.Fabric.Handlers;

public sealed class MLExperimentHandler : FabricItemHandlerBase<MLExperiment>
{
    protected override ItemType FabricItemType => ItemType.MLExperiment;
}