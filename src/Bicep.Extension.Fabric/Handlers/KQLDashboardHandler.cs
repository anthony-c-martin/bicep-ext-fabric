using Microsoft.Fabric.Api.Core.Models;

namespace Bicep.Extension.Fabric.Handlers;

public sealed class KQLDashboardHandler : FabricItemHandlerBase<KQLDashboard>
{
    protected override ItemType FabricItemType => ItemType.KQLDashboard;
}