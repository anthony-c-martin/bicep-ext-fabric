using Microsoft.Fabric.Api.Core.Models;

namespace Bicep.Extension.Fabric.Handlers;

public sealed class OntologyHandler : FabricItemHandlerBase<Ontology>
{
    protected override ItemType FabricItemType => ItemType.Ontology;
}