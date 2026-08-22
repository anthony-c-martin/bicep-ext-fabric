using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace Bicep.Extension.Fabric.Models;

public class DomainIdentifiers
{
    [TypeProperty("The Domain ID. Domain creation is not supported by Microsoft.Fabric.Api 2.20.0 (no create API is exposed); set this to reference an existing Domain.", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public required string Id { get; set; }
}

// NOTE: Microsoft.Fabric.Api 2.20.0 does not expose a create/update Domain API (only get/list/delete).
// This resource can therefore only reference (via `id`) and delete an existing domain.
[ResourceType("Domain")]
public class Domain : DomainIdentifiers
{
    [TypeProperty("The Domain display name", ObjectTypePropertyFlags.ReadOnly)]
    public string? DisplayName { get; set; }

    [TypeProperty("The Domain description", ObjectTypePropertyFlags.ReadOnly)]
    public string? Description { get; set; }

    [TypeProperty("The parent Domain ID", ObjectTypePropertyFlags.ReadOnly)]
    public string? ParentDomainId { get; set; }
}
