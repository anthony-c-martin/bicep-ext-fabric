using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace Bicep.Extension.Fabric.Models;

public class DeploymentPipelineIdentifiers
{
    [TypeProperty("The Deployment Pipeline ID", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.ReadOnly)]
    public string? Id { get; set; }
}

[ResourceType("DeploymentPipeline")]
public class DeploymentPipeline : DeploymentPipelineIdentifiers
{
    [TypeProperty("The Deployment Pipeline display name", ObjectTypePropertyFlags.Required)]
    public required string DisplayName { get; set; }

    [TypeProperty("The Deployment Pipeline description")]
    public string? Description { get; set; }

    [TypeProperty("The deployment pipeline stages, ordered from earliest to latest. Must contain between 2 and 10 stages.", ObjectTypePropertyFlags.Required)]
    public required DeploymentPipelineStage[] Stages { get; set; }
}

public class DeploymentPipelineStage
{
    [TypeProperty("The stage ID", ObjectTypePropertyFlags.ReadOnly)]
    public string? Id { get; set; }

    [TypeProperty("The stage display name", ObjectTypePropertyFlags.Required)]
    public required string DisplayName { get; set; }

    [TypeProperty("The stage description", ObjectTypePropertyFlags.Required)]
    public required string Description { get; set; }

    [TypeProperty("Whether the stage is public", ObjectTypePropertyFlags.Required)]
    public required bool IsPublic { get; set; }

    [TypeProperty("The workspace assigned to this stage")]
    public string? WorkspaceId { get; set; }
}
