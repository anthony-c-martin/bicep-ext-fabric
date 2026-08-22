using CoreModels = Microsoft.Fabric.Api.Core.Models;

namespace Bicep.Extension.Fabric.Handlers;

public sealed class WorkspaceNetworkCommunicationPolicyHandler : FabricResourceHandlerBase<WorkspaceNetworkCommunicationPolicy, WorkspaceNetworkCommunicationPolicyIdentifiers>
{
    protected override Task<ResourceResponse> Preview(ResourceRequest request, CancellationToken cancellationToken)
        => Task.FromResult(GetResponse(request));

    protected override Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            var workspaceId = ParseId(request.Properties.WorkspaceId, "workspaceId");

            await client.Core.Workspaces.SetNetworkCommunicationPolicyAsync(
                workspaceId,
                ToSdkPolicy(request.Properties),
                ifMatch: null,
                cancellationToken);

            var result = (await client.Core.Workspaces.GetNetworkCommunicationPolicyAsync(workspaceId, cancellationToken)).Value;

            return BuildResponse(request.Type, request.ApiVersion, ToProperties(result, workspaceId));
        });

    protected override Task<ResourceResponse> Get(ReferenceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            var workspaceId = ParseId(request.Identifiers.WorkspaceId, "workspaceId");
            var result = (await client.Core.Workspaces.GetNetworkCommunicationPolicyAsync(workspaceId, cancellationToken)).Value;

            return BuildResponse(request.Type, request.ApiVersion, ToProperties(result, workspaceId));
        });

    protected override Task<ResourceResponse> Delete(ReferenceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            // The Fabric API does not support deleting this policy; reset it to its defaults, matching the Terraform provider's behavior.
            await client.Core.Workspaces.SetNetworkCommunicationPolicyAsync(
                ParseId(request.Identifiers.WorkspaceId, "workspaceId"),
                new CoreModels.WorkspaceNetworkingCommunicationPolicy(),
                ifMatch: null,
                cancellationToken);

            return GetResponse(request, properties: null);
        });

    protected override WorkspaceNetworkCommunicationPolicyIdentifiers GetIdentifiers(WorkspaceNetworkCommunicationPolicy properties)
        => new() { WorkspaceId = properties.WorkspaceId };

    private static CoreModels.WorkspaceNetworkingCommunicationPolicy ToSdkPolicy(WorkspaceNetworkCommunicationPolicy properties)
    {
        var policy = new CoreModels.WorkspaceNetworkingCommunicationPolicy();

        if (properties.Inbound?.PublicAccessRules is { } inboundRules)
        {
            policy.Inbound = new CoreModels.InboundRules { PublicAccessRules = ToSdkNetworkRules(inboundRules) };
        }

        if (properties.Outbound?.PublicAccessRules is { } outboundRules)
        {
            policy.Outbound = new CoreModels.OutboundRules { PublicAccessRules = ToSdkNetworkRules(outboundRules) };
        }

        return policy;
    }

    private static CoreModels.NetworkRules ToSdkNetworkRules(WorkspacePublicAccessRules rules)
        => new() { DefaultAction = rules.DefaultAction is { } action ? ToSdkAction(action) : null };

    private static CoreModels.NetworkAccessRule ToSdkAction(NetworkAccessRule value)
        => value switch
        {
            NetworkAccessRule.Allow => CoreModels.NetworkAccessRule.Allow,
            NetworkAccessRule.Deny => CoreModels.NetworkAccessRule.Deny,
            _ => throw new ResourceErrorException("InvalidProperty", $"Unsupported default action '{value}'.", "defaultAction"),
        };

    private static NetworkAccessRule? ToModelAction(CoreModels.NetworkAccessRule? value)
        => value?.ToString() switch
        {
            null => null,
            nameof(NetworkAccessRule.Allow) => NetworkAccessRule.Allow,
            nameof(NetworkAccessRule.Deny) => NetworkAccessRule.Deny,
            var other => throw new ResourceErrorException("InvalidResponse", $"Unsupported default action '{other}' returned by the Fabric API."),
        };

    private static WorkspaceNetworkCommunicationPolicy ToProperties(CoreModels.WorkspaceNetworkingCommunicationPolicy policy, Guid workspaceId)
        => new()
        {
            WorkspaceId = workspaceId.ToString(),
            Inbound = policy.Inbound?.PublicAccessRules is { } inboundRules
                ? new WorkspaceInboundRules { PublicAccessRules = new WorkspacePublicAccessRules { DefaultAction = ToModelAction(inboundRules.DefaultAction) } }
                : null,
            Outbound = policy.Outbound?.PublicAccessRules is { } outboundRules
                ? new WorkspaceOutboundRules { PublicAccessRules = new WorkspacePublicAccessRules { DefaultAction = ToModelAction(outboundRules.DefaultAction) } }
                : null,
        };

    private static ResourceResponse BuildResponse(string type, string? apiVersion, WorkspaceNetworkCommunicationPolicy properties)
        => new()
        {
            Type = type,
            ApiVersion = apiVersion,
            Properties = properties,
            Identifiers = new WorkspaceNetworkCommunicationPolicyIdentifiers { WorkspaceId = properties.WorkspaceId },
        };

    private static Guid ParseId(string value, string target)
        => Guid.TryParse(value, out var result)
            ? result
            : throw new ResourceErrorException("InvalidIdentifier", $"'{value}' is not a valid GUID.", target);
}
