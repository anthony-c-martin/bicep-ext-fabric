using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace Bicep.Extension.Fabric.Models;

public enum GatewayRole
{
    Admin,
    ConnectionCreator,
    ConnectionCreatorWithResharing,
}

public class GatewayRoleAssignmentIdentifiers
{
    [TypeProperty("The Gateway ID", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public string GatewayId { get; set; } = string.Empty;

    [TypeProperty("The Gateway Role Assignment ID", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.ReadOnly)]
    public string? Id { get; set; }
}

[ResourceType("GatewayRoleAssignment")]
public class GatewayRoleAssignment : GatewayRoleAssignmentIdentifiers
{
    [TypeProperty("The principal", ObjectTypePropertyFlags.Required)]
    public required Principal Principal { get; set; }

    [TypeProperty("The gateway role of the principal", ObjectTypePropertyFlags.Required)]
    public required GatewayRole Role { get; set; }
}
