using Microsoft.Fabric.Api.Core.Models;

namespace Bicep.Extension.Fabric.Handlers;

public sealed class AnomalyDetectorHandler : FabricItemHandlerBase<AnomalyDetector>
{
    protected override ItemType FabricItemType => ItemType.AnomalyDetector;
}