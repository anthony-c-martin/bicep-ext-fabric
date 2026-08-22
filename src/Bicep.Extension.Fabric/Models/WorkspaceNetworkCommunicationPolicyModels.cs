using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace Bicep.Extension.Fabric.Models;

public class WorkspacePublicAccessRules
{
    [TypeProperty("The default policy for workspace access from public networks")]
    public NetworkAccessRule? DefaultAction { get; set; }
}

public class WorkspaceInboundRules
{
    [TypeProperty("The policy for inbound communication to the workspace from public networks")]
    public WorkspacePublicAccessRules? PublicAccessRules { get; set; }
}

public class WorkspaceOutboundRules
{
    [TypeProperty("The policy for outbound communication from the workspace to public networks")]
    public WorkspacePublicAccessRules? PublicAccessRules { get; set; }
}

public class WorkspaceNetworkCommunicationPolicyIdentifiers
{
    [TypeProperty("The containing Fabric workspace ID", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public string WorkspaceId { get; set; } = string.Empty;
}

[ResourceType("WorkspaceNetworkCommunicationPolicy")]
public class WorkspaceNetworkCommunicationPolicy : WorkspaceNetworkCommunicationPolicyIdentifiers
{
    [TypeProperty("The policy for all inbound communication to the workspace")]
    public WorkspaceInboundRules? Inbound { get; set; }

    [TypeProperty("The policy for all outbound communication from the workspace")]
    public WorkspaceOutboundRules? Outbound { get; set; }
}
