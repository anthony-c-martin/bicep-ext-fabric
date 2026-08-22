using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace Bicep.Extension.Fabric.Models;

public enum NetworkAccessRule
{
    Allow,
    Deny,
}

public class WorkspaceGitOutboundPolicyIdentifiers
{
    [TypeProperty("The containing Fabric workspace ID", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public string WorkspaceId { get; set; } = string.Empty;
}

[ResourceType("WorkspaceGitOutboundPolicy")]
public class WorkspaceGitOutboundPolicy : WorkspaceGitOutboundPolicyIdentifiers
{
    [TypeProperty("The default policy for Git-related outbound access from the workspace to public networks", ObjectTypePropertyFlags.Required)]
    public required NetworkAccessRule DefaultAction { get; set; }
}
