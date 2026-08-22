using CoreModels = Microsoft.Fabric.Api.Core.Models;

namespace Bicep.Extension.Fabric.Handlers;

public sealed class WorkspaceGitOutboundPolicyHandler : FabricResourceHandlerBase<WorkspaceGitOutboundPolicy, WorkspaceGitOutboundPolicyIdentifiers>
{
    protected override Task<ResourceResponse> Preview(ResourceRequest request, CancellationToken cancellationToken)
        => Task.FromResult(GetResponse(request));

    protected override Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            var workspaceId = ParseId(request.Properties.WorkspaceId, "workspaceId");

            await client.Core.Workspaces.SetGitOutboundPolicyAsync(
                workspaceId,
                new CoreModels.NetworkRules { DefaultAction = ToSdkAction(request.Properties.DefaultAction) },
                ifMatch: null,
                cancellationToken);

            var result = (await client.Core.Workspaces.GetGitOutboundPolicyAsync(workspaceId, cancellationToken)).Value;

            return BuildResponse(request.Type, request.ApiVersion, ToProperties(result, workspaceId));
        });

    protected override Task<ResourceResponse> Get(ReferenceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            var workspaceId = ParseId(request.Identifiers.WorkspaceId, "workspaceId");
            var result = (await client.Core.Workspaces.GetGitOutboundPolicyAsync(workspaceId, cancellationToken)).Value;

            return BuildResponse(request.Type, request.ApiVersion, ToProperties(result, workspaceId));
        });

    protected override Task<ResourceResponse> Delete(ReferenceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            // The Fabric API does not support deleting this policy; reset it to its default (Allow), matching the Terraform provider's behavior.
            await client.Core.Workspaces.SetGitOutboundPolicyAsync(
                ParseId(request.Identifiers.WorkspaceId, "workspaceId"),
                new CoreModels.NetworkRules { DefaultAction = CoreModels.NetworkAccessRule.Allow },
                ifMatch: null,
                cancellationToken);

            return GetResponse(request, properties: null);
        });

    protected override WorkspaceGitOutboundPolicyIdentifiers GetIdentifiers(WorkspaceGitOutboundPolicy properties)
        => new() { WorkspaceId = properties.WorkspaceId };

    private static CoreModels.NetworkAccessRule ToSdkAction(NetworkAccessRule value)
        => value switch
        {
            NetworkAccessRule.Allow => CoreModels.NetworkAccessRule.Allow,
            NetworkAccessRule.Deny => CoreModels.NetworkAccessRule.Deny,
            _ => throw new ResourceErrorException("InvalidProperty", $"Unsupported default action '{value}'.", "defaultAction"),
        };

    private static NetworkAccessRule ToModelAction(CoreModels.NetworkAccessRule? value)
        => value?.ToString() switch
        {
            nameof(NetworkAccessRule.Allow) => NetworkAccessRule.Allow,
            nameof(NetworkAccessRule.Deny) => NetworkAccessRule.Deny,
            var other => throw new ResourceErrorException("InvalidResponse", $"Unsupported default action '{other}' returned by the Fabric API."),
        };

    private static WorkspaceGitOutboundPolicy ToProperties(CoreModels.NetworkRules rules, Guid workspaceId)
        => new()
        {
            WorkspaceId = workspaceId.ToString(),
            DefaultAction = ToModelAction(rules.DefaultAction),
        };

    private static ResourceResponse BuildResponse(string type, string? apiVersion, WorkspaceGitOutboundPolicy properties)
        => new()
        {
            Type = type,
            ApiVersion = apiVersion,
            Properties = properties,
            Identifiers = new WorkspaceGitOutboundPolicyIdentifiers { WorkspaceId = properties.WorkspaceId },
        };

    private static Guid ParseId(string value, string target)
        => Guid.TryParse(value, out var result)
            ? result
            : throw new ResourceErrorException("InvalidIdentifier", $"'{value}' is not a valid GUID.", target);
}
