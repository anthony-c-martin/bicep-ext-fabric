using Microsoft.Fabric.Api.Core.Models;

namespace Bicep.Extension.Fabric.Handlers;

public sealed class ReportHandler : FabricItemHandlerBase<Report>
{
    protected override ItemType FabricItemType => ItemType.Report;
}