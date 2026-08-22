using Microsoft.Fabric.Api;
using Microsoft.Fabric.Api.Core.Models;
using DigitalTwinBuilderFlowSdk = Microsoft.Fabric.Api.DigitalTwinBuilderFlow.Models;

namespace Bicep.Extension.Fabric.Handlers;

public sealed class DigitalTwinBuilderFlowHandler : FabricTypedItemHandlerBase<DigitalTwinBuilderFlow, DigitalTwinBuilderFlowSdk.DigitalTwinBuilderFlow>
{
    protected override ItemType FabricItemType => ItemType.DigitalTwinBuilderFlow;

    protected override async Task<DigitalTwinBuilderFlowSdk.DigitalTwinBuilderFlow> CreateItemAsync(FabricClient client, Guid workspaceId, DigitalTwinBuilderFlow properties, CancellationToken cancellationToken)
    {
        // The Fabric API does not accept a folder for Digital Twin Builder Flow creation; the base
        // handler moves the item into the requested folder after it has been created instead.
        var create = new DigitalTwinBuilderFlowSdk.CreateDigitalTwinBuilderFlowRequest(properties.DisplayName)
        {
            Description = properties.Description,
            CreationPayload = properties.Configuration is { } configuration
                ? new DigitalTwinBuilderFlowSdk.DigitalTwinBuilderFlowCreationPayload(ToItemReference(configuration.DigitalTwinBuilderItemReference))
                : null,
            Definition = properties.Definition is { } definition ? ToSdkDefinition(definition) : null,
        };

        if (create.CreationPayload is null && create.Definition is null)
        {
            throw new ResourceErrorException(
                "InvalidConfiguration",
                "A Digital Twin Builder Flow requires either 'configuration' or 'definition'.",
                "configuration");
        }

        var result = (await client.DigitalTwinBuilderFlow.Items.CreateDigitalTwinBuilderFlowAsync(workspaceId, create, cancellationToken)).Value;

        if (properties.FolderId is { } folderId && result.Id is { } itemId)
        {
            await client.Core.Items.MoveItemAsync(
                workspaceId,
                itemId,
                new MoveItemRequest { TargetFolderId = ParseGuid(folderId, "folderId") },
                cancellationToken);
        }

        return result;
    }

    protected override async Task<DigitalTwinBuilderFlowSdk.DigitalTwinBuilderFlow> UpdateItemAsync(FabricClient client, Guid workspaceId, Guid itemId, DigitalTwinBuilderFlow properties, CancellationToken cancellationToken)
    {
        var update = new DigitalTwinBuilderFlowSdk.UpdateDigitalTwinBuilderFlowRequest
        {
            DisplayName = properties.DisplayName,
            Description = properties.Description,
        };

        return (await client.DigitalTwinBuilderFlow.Items.UpdateDigitalTwinBuilderFlowAsync(workspaceId, itemId, update, cancellationToken)).Value;
    }

    protected override async Task<DigitalTwinBuilderFlowSdk.DigitalTwinBuilderFlow> GetItemAsync(FabricClient client, Guid workspaceId, Guid itemId, CancellationToken cancellationToken)
        => (await client.DigitalTwinBuilderFlow.Items.GetDigitalTwinBuilderFlowAsync(workspaceId, itemId, cancellationToken)).Value;

    protected override Task DeleteItemAsync(FabricClient client, Guid workspaceId, Guid itemId, CancellationToken cancellationToken)
        => client.DigitalTwinBuilderFlow.Items.DeleteDigitalTwinBuilderFlowAsync(workspaceId, itemId, cancellationToken);

    protected override Task UpdateDefinitionAsync(FabricClient client, Guid workspaceId, Guid itemId, FabricItemDefinition definition, CancellationToken cancellationToken)
        => client.DigitalTwinBuilderFlow.Items.UpdateDigitalTwinBuilderFlowDefinitionAsync(
            workspaceId,
            itemId,
            new DigitalTwinBuilderFlowSdk.UpdateDigitalTwinBuilderFlowDefinitionRequest(ToSdkDefinition(definition)),
            cancellationToken: cancellationToken);

    protected override DigitalTwinBuilderFlow ToProperties(DigitalTwinBuilderFlowSdk.DigitalTwinBuilderFlow item, Guid workspaceId)
        => new()
        {
            WorkspaceId = (item.WorkspaceId ?? workspaceId).ToString(),
            Id = item.Id?.ToString(),
            DisplayName = item.DisplayName,
            Description = item.Description,
            FolderId = item.FolderId?.ToString(),
            Type = item.Type.ToString(),
            Configuration = item.Properties?.DigitalTwinBuilderItemReference is ItemReferenceById reference
                ? new DigitalTwinBuilderFlowConfiguration
                {
                    DigitalTwinBuilderItemReference = new DigitalTwinBuilderItemReference
                    {
                        WorkspaceId = reference.WorkspaceId.ToString(),
                        ItemId = reference.ItemId.ToString(),
                    },
                }
                : null,
        };

    private ItemReferenceById ToItemReference(DigitalTwinBuilderItemReference reference)
        => new(
            ParseGuid(reference.ItemId, "configuration.digitalTwinBuilderItemReference.itemId"),
            ParseGuid(reference.WorkspaceId, "configuration.digitalTwinBuilderItemReference.workspaceId"));

    private static DigitalTwinBuilderFlowSdk.DigitalTwinBuilderFlowPublicDefinition ToSdkDefinition(FabricItemDefinition definition)
        => new(definition.Parts.Select(part => new DigitalTwinBuilderFlowSdk.DigitalTwinBuilderFlowPublicDefinitionPart
        {
            Path = part.Path,
            Payload = part.Payload,
            PayloadType = PayloadType.InlineBase64,
        }))
        {
            Format = definition.Format,
        };
}
