using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace Bicep.Extension.Fabric.Models;

public enum WorkspaceRole
{
    Admin,
    Contributor,
    Member,
    Viewer,
}

public class WorkspaceRoleAssignmentIdentifiers
{
    [TypeProperty("The containing Fabric workspace ID", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public string WorkspaceId { get; set; } = string.Empty;

    [TypeProperty("The Workspace Role Assignment ID", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.ReadOnly)]
    public string? Id { get; set; }
}

[ResourceType("WorkspaceRoleAssignment")]
public class WorkspaceRoleAssignment : WorkspaceRoleAssignmentIdentifiers
{
    [TypeProperty("The principal", ObjectTypePropertyFlags.Required)]
    public required Principal Principal { get; set; }

    [TypeProperty("The workspace role of the principal", ObjectTypePropertyFlags.Required)]
    public required WorkspaceRole Role { get; set; }
}
