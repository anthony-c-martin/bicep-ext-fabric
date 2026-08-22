using Microsoft.Fabric.Api.Core.Models;

namespace Bicep.Extension.Fabric.Handlers;

public sealed class NotebookHandler : FabricItemHandlerBase<Notebook>
{
    protected override ItemType FabricItemType => ItemType.Notebook;
}