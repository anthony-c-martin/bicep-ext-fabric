using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace Bicep.Extension.Fabric.Models;

public enum DomainRole
{
    Admin,
    Contributor,
}

public enum DomainPrincipalType
{
    User,
    Group,
    ServicePrincipal,
    ServicePrincipalProfile,
    EntireTenant,
}

public class DomainPrincipal
{
    [TypeProperty("The principal ID. Ignored when 'type' is 'EntireTenant'.")]
    public string? Id { get; set; }

    [TypeProperty("The type of the principal", ObjectTypePropertyFlags.Required)]
    public required DomainPrincipalType Type { get; set; }
}

public class DomainRoleAssignmentsIdentifiers
{
    [TypeProperty("The Domain ID", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public string DomainId { get; set; } = string.Empty;

    [TypeProperty("The role assigned to the principals", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public DomainRole Role { get; set; }
}

// This resource owns the set of principals holding the given role on the domain: any principal not
// listed in `principals` is unassigned, and deleting the resource unassigns all of them.
[ResourceType("DomainRoleAssignments")]
public class DomainRoleAssignments : DomainRoleAssignmentsIdentifiers
{
    [TypeProperty("The set of principals that hold the role on the domain", ObjectTypePropertyFlags.Required)]
    public required DomainPrincipal[] Principals { get; set; }
}
