using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace Bicep.Extension.Fabric.Models;

public enum ConnectionAccessActionType
{
    Allow,
    Deny,
}

public class ConnectionRuleEndpointMetadata
{
    [TypeProperty("A wildcard-supported hostname pattern that is allowed for outbound communication (e.g. *.microsoft.com)", ObjectTypePropertyFlags.Required)]
    public required string HostnamePattern { get; set; }
}

public class ConnectionRuleWorkspaceMetadata
{
    [TypeProperty("The ID of the target workspace that is allowed to be connected to from the current workspace", ObjectTypePropertyFlags.Required)]
    public required string WorkspaceId { get; set; }
}

public class OutboundConnectionRule
{
    [TypeProperty("The cloud connection type the rule applies to (e.g. Lakehouse, Warehouse, FabricSql, PowerPlatformDataflows, Web, Sql)", ObjectTypePropertyFlags.Required)]
    public required string ConnectionType { get; set; }

    [TypeProperty("The default outbound access behavior for this connection type", ObjectTypePropertyFlags.Required)]
    public required ConnectionAccessActionType DefaultAction { get; set; }

    [TypeProperty("The explicitly permitted external endpoints for this connection type. Only applicable to endpoint-based connection types")]
    public ConnectionRuleEndpointMetadata[]? AllowedEndpoints { get; set; }

    [TypeProperty("The workspaces explicitly permitted for outbound communication for this connection type. Only applicable to Lakehouse, Warehouse, FabricSql, and PowerPlatformDataflows")]
    public ConnectionRuleWorkspaceMetadata[]? AllowedWorkspaces { get; set; }
}

public class WorkspaceOutboundCloudConnectionRulesIdentifiers
{
    [TypeProperty("The containing Fabric workspace ID", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public string WorkspaceId { get; set; } = string.Empty;
}

[ResourceType("WorkspaceOutboundCloudConnectionRules")]
public class WorkspaceOutboundCloudConnectionRules : WorkspaceOutboundCloudConnectionRulesIdentifiers
{
    [TypeProperty("The default behavior for cloud connection types not explicitly listed in rules", ObjectTypePropertyFlags.Required)]
    public required ConnectionAccessActionType DefaultAction { get; set; }

    [TypeProperty("The rules that define outbound access behavior for specific cloud connection types")]
    public OutboundConnectionRule[]? Rules { get; set; }
}
