using CoreModels = Microsoft.Fabric.Api.Core.Models;

namespace Bicep.Extension.Fabric.Handlers;

public sealed class WorkspaceOutboundCloudConnectionRulesHandler : FabricResourceHandlerBase<WorkspaceOutboundCloudConnectionRules, WorkspaceOutboundCloudConnectionRulesIdentifiers>
{
    protected override Task<ResourceResponse> Preview(ResourceRequest request, CancellationToken cancellationToken)
        => Task.FromResult(GetResponse(request));

    protected override Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            var workspaceId = ParseId(request.Properties.WorkspaceId, "workspaceId");

            await client.Core.Workspaces.SetOutboundCloudConnectionRulesAsync(workspaceId, ToSdkRules(request.Properties), cancellationToken);

            var result = (await client.Core.Workspaces.GetOutboundCloudConnectionRulesAsync(workspaceId, cancellationToken)).Value;

            return BuildResponse(request.Type, request.ApiVersion, ToProperties(result, workspaceId));
        });

    protected override Task<ResourceResponse> Get(ReferenceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            var workspaceId = ParseId(request.Identifiers.WorkspaceId, "workspaceId");
            var result = (await client.Core.Workspaces.GetOutboundCloudConnectionRulesAsync(workspaceId, cancellationToken)).Value;

            return BuildResponse(request.Type, request.ApiVersion, ToProperties(result, workspaceId));
        });

    protected override Task<ResourceResponse> Delete(ReferenceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            // The Fabric API does not support deleting these rules; reset them to their default (Allow, no rules), matching the Terraform provider's behavior.
            await client.Core.Workspaces.SetOutboundCloudConnectionRulesAsync(
                ParseId(request.Identifiers.WorkspaceId, "workspaceId"),
                new CoreModels.WorkspaceOutboundConnections { DefaultAction = CoreModels.ConnectionAccessActionType.Allow },
                cancellationToken);

            return GetResponse(request, properties: null);
        });

    protected override WorkspaceOutboundCloudConnectionRulesIdentifiers GetIdentifiers(WorkspaceOutboundCloudConnectionRules properties)
        => new() { WorkspaceId = properties.WorkspaceId };

    private static CoreModels.WorkspaceOutboundConnections ToSdkRules(WorkspaceOutboundCloudConnectionRules properties)
    {
        var rules = new CoreModels.WorkspaceOutboundConnections { DefaultAction = ToSdkAction(properties.DefaultAction) };

        if (properties.Rules is { } ruleList)
        {
            foreach (var rule in ruleList)
            {
                var sdkRule = new CoreModels.OutboundConnectionRule(rule.ConnectionType) { DefaultAction = ToSdkAction(rule.DefaultAction) };

                if (rule.AllowedEndpoints is { } allowedEndpoints)
                {
                    foreach (var endpoint in allowedEndpoints)
                    {
                        sdkRule.AllowedEndpoints.Add(new CoreModels.ConnectionRuleEndpointMetadata(endpoint.HostnamePattern));
                    }
                }

                if (rule.AllowedWorkspaces is { } allowedWorkspaces)
                {
                    foreach (var workspace in allowedWorkspaces)
                    {
                        sdkRule.AllowedWorkspaces.Add(new CoreModels.ConnectionRuleWorkspaceMetadata(ParseId(workspace.WorkspaceId, "rules.allowedWorkspaces.workspaceId")));
                    }
                }

                rules.Rules.Add(sdkRule);
            }
        }

        return rules;
    }

    private static CoreModels.ConnectionAccessActionType ToSdkAction(ConnectionAccessActionType value)
        => value switch
        {
            ConnectionAccessActionType.Allow => CoreModels.ConnectionAccessActionType.Allow,
            ConnectionAccessActionType.Deny => CoreModels.ConnectionAccessActionType.Deny,
            _ => throw new ResourceErrorException("InvalidProperty", $"Unsupported default action '{value}'.", "defaultAction"),
        };

    private static ConnectionAccessActionType ToModelAction(CoreModels.ConnectionAccessActionType? value)
        => value?.ToString() switch
        {
            nameof(ConnectionAccessActionType.Allow) => ConnectionAccessActionType.Allow,
            nameof(ConnectionAccessActionType.Deny) => ConnectionAccessActionType.Deny,
            var other => throw new ResourceErrorException("InvalidResponse", $"Unsupported default action '{other}' returned by the Fabric API."),
        };

    private static WorkspaceOutboundCloudConnectionRules ToProperties(CoreModels.WorkspaceOutboundConnections rules, Guid workspaceId)
        => new()
        {
            WorkspaceId = workspaceId.ToString(),
            DefaultAction = ToModelAction(rules.DefaultAction),
            Rules =
            [
                .. rules.Rules.Select(rule => new OutboundConnectionRule
                {
                    ConnectionType = rule.ConnectionType,
                    DefaultAction = ToModelAction(rule.DefaultAction),
                    AllowedEndpoints = rule.AllowedEndpoints is { Count: > 0 }
                        ? [.. rule.AllowedEndpoints.Select(endpoint => new ConnectionRuleEndpointMetadata { HostnamePattern = endpoint.HostnamePattern })]
                        : null,
                    AllowedWorkspaces = rule.AllowedWorkspaces is { Count: > 0 }
                        ? [.. rule.AllowedWorkspaces.Select(workspace => new ConnectionRuleWorkspaceMetadata { WorkspaceId = workspace.WorkspaceId.ToString() })]
                        : null,
                }),
            ],
        };

    private static ResourceResponse BuildResponse(string type, string? apiVersion, WorkspaceOutboundCloudConnectionRules properties)
        => new()
        {
            Type = type,
            ApiVersion = apiVersion,
            Properties = properties,
            Identifiers = new WorkspaceOutboundCloudConnectionRulesIdentifiers { WorkspaceId = properties.WorkspaceId },
        };

    private static Guid ParseId(string value, string target)
        => Guid.TryParse(value, out var result)
            ? result
            : throw new ResourceErrorException("InvalidIdentifier", $"'{value}' is not a valid GUID.", target);
}
