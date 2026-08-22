using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace Bicep.Extension.Fabric.Models;

public enum GatewayAccessActionType
{
    Allow,
    Deny,
}

public class GatewayAccessRuleMetadata
{
    [TypeProperty("The gateway ID to allow outbound access to", ObjectTypePropertyFlags.Required)]
    public required string Id { get; set; }
}

public class WorkspaceOutboundGatewayRulesIdentifiers
{
    [TypeProperty("The containing Fabric workspace ID", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public string WorkspaceId { get; set; } = string.Empty;
}

[ResourceType("WorkspaceOutboundGatewayRules")]
public class WorkspaceOutboundGatewayRules : WorkspaceOutboundGatewayRulesIdentifiers
{
    [TypeProperty("The default behavior for gateways not explicitly listed in allowedGateways", ObjectTypePropertyFlags.Required)]
    public required GatewayAccessActionType DefaultAction { get; set; }

    [TypeProperty("The set of gateways that are explicitly allowed for outbound access from the workspace")]
    public GatewayAccessRuleMetadata[]? AllowedGateways { get; set; }
}
