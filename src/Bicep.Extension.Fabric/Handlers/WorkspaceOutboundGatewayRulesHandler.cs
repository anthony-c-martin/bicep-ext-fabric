using CoreModels = Microsoft.Fabric.Api.Core.Models;

namespace Bicep.Extension.Fabric.Handlers;

public sealed class WorkspaceOutboundGatewayRulesHandler : FabricResourceHandlerBase<WorkspaceOutboundGatewayRules, WorkspaceOutboundGatewayRulesIdentifiers>
{
    protected override Task<ResourceResponse> Preview(ResourceRequest request, CancellationToken cancellationToken)
        => Task.FromResult(GetResponse(request));

    protected override Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            var workspaceId = ParseId(request.Properties.WorkspaceId, "workspaceId");

            await client.Core.Workspaces.SetOutboundGatewayRulesAsync(workspaceId, ToSdkRules(request.Properties), cancellationToken);

            var result = (await client.Core.Workspaces.GetOutboundGatewayRulesAsync(workspaceId, cancellationToken)).Value;

            return BuildResponse(request.Type, request.ApiVersion, ToProperties(result, workspaceId));
        });

    protected override Task<ResourceResponse> Get(ReferenceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            var workspaceId = ParseId(request.Identifiers.WorkspaceId, "workspaceId");
            var result = (await client.Core.Workspaces.GetOutboundGatewayRulesAsync(workspaceId, cancellationToken)).Value;

            return BuildResponse(request.Type, request.ApiVersion, ToProperties(result, workspaceId));
        });

    protected override Task<ResourceResponse> Delete(ReferenceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            // The Fabric API does not support deleting these rules; reset them to their default (Allow, no gateways), matching the Terraform provider's behavior.
            await client.Core.Workspaces.SetOutboundGatewayRulesAsync(
                ParseId(request.Identifiers.WorkspaceId, "workspaceId"),
                new CoreModels.WorkspaceOutboundGateways { DefaultAction = CoreModels.GatewayAccessActionType.Allow },
                cancellationToken);

            return GetResponse(request, properties: null);
        });

    protected override WorkspaceOutboundGatewayRulesIdentifiers GetIdentifiers(WorkspaceOutboundGatewayRules properties)
        => new() { WorkspaceId = properties.WorkspaceId };

    private static CoreModels.WorkspaceOutboundGateways ToSdkRules(WorkspaceOutboundGatewayRules properties)
    {
        var rules = new CoreModels.WorkspaceOutboundGateways { DefaultAction = ToSdkAction(properties.DefaultAction) };

        if (properties.AllowedGateways is { } allowedGateways)
        {
            foreach (var gateway in allowedGateways)
            {
                rules.AllowedGateways.Add(new CoreModels.GatewayAccessRuleMetadata(ParseId(gateway.Id, "allowedGateways.id")));
            }
        }

        return rules;
    }

    private static CoreModels.GatewayAccessActionType ToSdkAction(GatewayAccessActionType value)
        => value switch
        {
            GatewayAccessActionType.Allow => CoreModels.GatewayAccessActionType.Allow,
            GatewayAccessActionType.Deny => CoreModels.GatewayAccessActionType.Deny,
            _ => throw new ResourceErrorException("InvalidProperty", $"Unsupported default action '{value}'.", "defaultAction"),
        };

    private static GatewayAccessActionType ToModelAction(CoreModels.GatewayAccessActionType? value)
        => value?.ToString() switch
        {
            nameof(GatewayAccessActionType.Allow) => GatewayAccessActionType.Allow,
            nameof(GatewayAccessActionType.Deny) => GatewayAccessActionType.Deny,
            var other => throw new ResourceErrorException("InvalidResponse", $"Unsupported default action '{other}' returned by the Fabric API."),
        };

    private static WorkspaceOutboundGatewayRules ToProperties(CoreModels.WorkspaceOutboundGateways rules, Guid workspaceId)
        => new()
        {
            WorkspaceId = workspaceId.ToString(),
            DefaultAction = ToModelAction(rules.DefaultAction),
            AllowedGateways = [.. rules.AllowedGateways.Select(gateway => new GatewayAccessRuleMetadata { Id = gateway.Id.ToString() })],
        };

    private static ResourceResponse BuildResponse(string type, string? apiVersion, WorkspaceOutboundGatewayRules properties)
        => new()
        {
            Type = type,
            ApiVersion = apiVersion,
            Properties = properties,
            Identifiers = new WorkspaceOutboundGatewayRulesIdentifiers { WorkspaceId = properties.WorkspaceId },
        };

    private static Guid ParseId(string value, string target)
        => Guid.TryParse(value, out var result)
            ? result
            : throw new ResourceErrorException("InvalidIdentifier", $"'{value}' is not a valid GUID.", target);
}
