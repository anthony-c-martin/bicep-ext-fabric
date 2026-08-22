using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace Bicep.Extension.Fabric.Models;

public enum ConnectionPrincipalType
{
    User,
    Group,
    ServicePrincipal,
    ServicePrincipalProfile,
    EntireTenant,
}

public class ConnectionPrincipal
{
    [TypeProperty("The principal ID", ObjectTypePropertyFlags.Required)]
    public required string Id { get; set; }

    [TypeProperty("The type of the principal", ObjectTypePropertyFlags.Required)]
    public required ConnectionPrincipalType Type { get; set; }
}

public enum ConnectionRole
{
    Owner,
    User,
    UserWithReshare,
}

public class ConnectionRoleAssignmentIdentifiers
{
    [TypeProperty("The Connection ID", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public string ConnectionId { get; set; } = string.Empty;

    [TypeProperty("The Connection Role Assignment ID", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.ReadOnly)]
    public string? Id { get; set; }
}

[ResourceType("ConnectionRoleAssignment")]
public class ConnectionRoleAssignment : ConnectionRoleAssignmentIdentifiers
{
    [TypeProperty("The principal", ObjectTypePropertyFlags.Required)]
    public required ConnectionPrincipal Principal { get; set; }

    [TypeProperty("The connection role of the principal", ObjectTypePropertyFlags.Required)]
    public required ConnectionRole Role { get; set; }
}
