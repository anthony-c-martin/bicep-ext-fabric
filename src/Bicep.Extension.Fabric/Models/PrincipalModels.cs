using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace Bicep.Extension.Fabric.Models;

public enum PrincipalType
{
    User,
    Group,
    ServicePrincipal,
    ServicePrincipalProfile,
}

public class Principal
{
    [TypeProperty("The principal ID", ObjectTypePropertyFlags.Required)]
    public required string Id { get; set; }

    [TypeProperty("The type of the principal", ObjectTypePropertyFlags.Required)]
    public required PrincipalType Type { get; set; }
}
