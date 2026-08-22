using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace Bicep.Extension.Fabric.Models;

public enum DeploymentPipelineRole
{
    Admin,
}

public class DeploymentPipelineRoleAssignmentIdentifiers
{
    [TypeProperty("The containing Deployment Pipeline ID", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public string DeploymentPipelineId { get; set; } = string.Empty;

    // The Fabric API deletes deployment pipeline role assignments by principal ID, so this
    // identifier is always set to the assignment's principal ID (see DeploymentPipelineRoleAssignmentHandler).
    [TypeProperty("The Deployment Pipeline Role Assignment ID", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.ReadOnly)]
    public string? Id { get; set; }
}

[ResourceType("DeploymentPipelineRoleAssignment")]
public class DeploymentPipelineRoleAssignment : DeploymentPipelineRoleAssignmentIdentifiers
{
    [TypeProperty("The principal", ObjectTypePropertyFlags.Required)]
    public required Principal Principal { get; set; }

    [TypeProperty("The deployment pipeline role of the principal", ObjectTypePropertyFlags.Required)]
    public required DeploymentPipelineRole Role { get; set; }
}
