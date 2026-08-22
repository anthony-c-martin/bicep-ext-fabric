using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace Bicep.Extension.Fabric.Models;

public enum TagScopeType
{
    Tenant,
    Domain,
}

public class TagScope
{
    [TypeProperty("The tag scope type", ObjectTypePropertyFlags.Required)]
    public required TagScopeType Type { get; set; }

    [TypeProperty("The domain object ID. Required when 'type' is 'Domain'.")]
    public string? DomainId { get; set; }
}

public class TagIdentifiers
{
    [TypeProperty("The Tag ID", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.ReadOnly)]
    public string? Id { get; set; }
}

[ResourceType("Tag")]
public class Tag : TagIdentifiers
{
    [TypeProperty("The Tag display name", ObjectTypePropertyFlags.Required)]
    public required string DisplayName { get; set; }

    [TypeProperty("The tag scope. Defaults to tenant scope when not specified. The scope cannot be changed after creation.")]
    public TagScope? Scope { get; set; }
}
