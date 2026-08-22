using Microsoft.Fabric.Api.Core.Models;

namespace Bicep.Extension.Fabric.Handlers;

public sealed class ApacheAirflowJobHandler : FabricItemHandlerBase<ApacheAirflowJob>
{
    protected override ItemType FabricItemType => ItemType.ApacheAirflowJob;
}