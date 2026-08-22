using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace Bicep.Extension.Fabric.Models;

public class DomainWorkspaceAssignmentIdentifiers
{
    [TypeProperty("The Domain ID", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public string DomainId { get; set; } = string.Empty;
}

// This resource owns the full set of workspaces assigned to the domain: any workspace not
// listed in `workspaceIds` is unassigned, and deleting the resource unassigns all workspaces.
[ResourceType("DomainWorkspaceAssignment")]
public class DomainWorkspaceAssignment : DomainWorkspaceAssignmentIdentifiers
{
    [TypeProperty("The set of Workspace IDs assigned to the domain", ObjectTypePropertyFlags.Required)]
    public required string[] WorkspaceIds { get; set; }
}
