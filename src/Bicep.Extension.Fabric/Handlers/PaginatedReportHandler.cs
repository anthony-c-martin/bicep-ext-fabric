using Microsoft.Fabric.Api.Core.Models;

namespace Bicep.Extension.Fabric.Handlers;

public sealed class PaginatedReportHandler : FabricItemHandlerBase<PaginatedReport>
{
    protected override ItemType FabricItemType => ItemType.PaginatedReport;
}