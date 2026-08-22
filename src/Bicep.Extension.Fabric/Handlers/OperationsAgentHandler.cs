using Microsoft.Fabric.Api;
using Microsoft.Fabric.Api.Core.Models;
using OperationsAgentSdk = Microsoft.Fabric.Api.OperationsAgent.Models;

namespace Bicep.Extension.Fabric.Handlers;

public sealed class OperationsAgentHandler : FabricTypedItemHandlerBase<OperationsAgent, OperationsAgentSdk.OperationsAgent>
{
    protected override ItemType FabricItemType => ItemType.OperationsAgent;

    protected override async Task<OperationsAgentSdk.OperationsAgent> CreateItemAsync(FabricClient client, Guid workspaceId, OperationsAgent properties, CancellationToken cancellationToken)
    {
        var create = new OperationsAgentSdk.CreateOperationsAgentRequest(properties.DisplayName)
        {
            Description = properties.Description,
            FolderId = ParseOptionalGuid(properties.FolderId, "folderId"),
            Definition = properties.Definition is { } definition ? ToSdkDefinition(definition) : null,
        };

        return (await client.OperationsAgent.Items.CreateOperationsAgentAsync(workspaceId, create, cancellationToken)).Value;
    }

    protected override async Task<OperationsAgentSdk.OperationsAgent> UpdateItemAsync(FabricClient client, Guid workspaceId, Guid itemId, OperationsAgent properties, CancellationToken cancellationToken)
    {
        var update = new OperationsAgentSdk.UpdateOperationsAgentRequest
        {
            DisplayName = properties.DisplayName,
            Description = properties.Description,
        };

        return (await client.OperationsAgent.Items.UpdateOperationsAgentAsync(workspaceId, itemId, update, cancellationToken)).Value;
    }

    protected override async Task<OperationsAgentSdk.OperationsAgent> GetItemAsync(FabricClient client, Guid workspaceId, Guid itemId, CancellationToken cancellationToken)
        => (await client.OperationsAgent.Items.GetOperationsAgentAsync(workspaceId, itemId, cancellationToken)).Value;

    protected override Task DeleteItemAsync(FabricClient client, Guid workspaceId, Guid itemId, CancellationToken cancellationToken)
        => client.OperationsAgent.Items.DeleteOperationsAgentAsync(workspaceId, itemId, cancellationToken);

    protected override Task UpdateDefinitionAsync(FabricClient client, Guid workspaceId, Guid itemId, FabricItemDefinition definition, CancellationToken cancellationToken)
        => client.OperationsAgent.Items.UpdateOperationsAgentDefinitionAsync(
            workspaceId,
            itemId,
            new OperationsAgentSdk.UpdateOperationsAgentDefinitionRequest(ToSdkDefinition(definition)),
            cancellationToken: cancellationToken);

    protected override OperationsAgent ToProperties(OperationsAgentSdk.OperationsAgent item, Guid workspaceId)
        => new()
        {
            WorkspaceId = (item.WorkspaceId ?? workspaceId).ToString(),
            Id = item.Id?.ToString(),
            DisplayName = item.DisplayName,
            Description = item.Description,
            FolderId = item.FolderId?.ToString(),
            Type = item.Type.ToString(),
            State = item.Properties?.State.ToString(),
        };

    private static OperationsAgentSdk.OperationsAgentPublicDefinition ToSdkDefinition(FabricItemDefinition definition)
        => new(definition.Parts.Select(part => new OperationsAgentSdk.OperationsAgentPublicDefinitionPart
        {
            Path = part.Path,
            Payload = part.Payload,
            PayloadType = PayloadType.InlineBase64,
        }))
        {
            Format = definition.Format,
        };
}
