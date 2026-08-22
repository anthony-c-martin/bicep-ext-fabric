using Microsoft.Fabric.Api;
using Microsoft.Fabric.Api.Core.Models;

namespace Bicep.Extension.Fabric.Handlers;

/// <summary>
/// Base class for handlers that manage a Fabric item through its workload-specific SDK client
/// (for example <c>client.Lakehouse.Items</c>) rather than the generic <c>client.Core.Items</c> client.
/// It implements the shared create/update/get/delete skeleton, folder moves, definition updates and
/// tag synchronisation, leaving each handler to supply only the workload-specific calls.
/// </summary>
public abstract class FabricTypedItemHandlerBase<TProperties, TItem> : FabricResourceHandlerBase<TProperties, FabricItemIdentifiers>, IFabricItemHandlerMetadata
    where TProperties : FabricItem
    where TItem : Item
{
    protected abstract ItemType FabricItemType { get; }

    string IFabricItemHandlerMetadata.ItemTypeName => FabricItemType.ToString();

    protected abstract Task<TItem> CreateItemAsync(FabricClient client, Guid workspaceId, TProperties properties, CancellationToken cancellationToken);

    protected abstract Task<TItem> UpdateItemAsync(FabricClient client, Guid workspaceId, Guid itemId, TProperties properties, CancellationToken cancellationToken);

    protected abstract Task<TItem> GetItemAsync(FabricClient client, Guid workspaceId, Guid itemId, CancellationToken cancellationToken);

    protected abstract Task DeleteItemAsync(FabricClient client, Guid workspaceId, Guid itemId, CancellationToken cancellationToken);

    protected abstract TProperties ToProperties(TItem item, Guid workspaceId);

    /// <summary>
    /// Applies an updated item definition. Only called when the user supplied a definition for an
    /// item that already exists; item types without a definition API leave this unimplemented.
    /// </summary>
    protected virtual Task UpdateDefinitionAsync(FabricClient client, Guid workspaceId, Guid itemId, FabricItemDefinition definition, CancellationToken cancellationToken)
        => throw new ResourceErrorException(
            "DefinitionNotSupported",
            $"The Fabric API does not support item definitions for {FabricItemType} items.",
            "definition");

    protected override Task<ResourceResponse> Preview(ResourceRequest request, CancellationToken cancellationToken)
        => Task.FromResult(GetResponse(request));

    protected override Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            var workspaceId = ParseGuid(request.Properties.WorkspaceId, "workspaceId");
            TItem result;

            if (request.Properties.Id is { } itemIdValue)
            {
                var existingItemId = ParseGuid(itemIdValue, "id");
                result = await UpdateItemAsync(client, workspaceId, existingItemId, request.Properties, cancellationToken);

                if (request.Properties.FolderId is { } folderId)
                {
                    await client.Core.Items.MoveItemAsync(
                        workspaceId,
                        existingItemId,
                        new MoveItemRequest { TargetFolderId = ParseGuid(folderId, "folderId") },
                        cancellationToken);
                }

                if (request.Properties.Definition is { } definition)
                {
                    await UpdateDefinitionAsync(client, workspaceId, existingItemId, definition, cancellationToken);
                }
            }
            else
            {
                result = await CreateItemAsync(client, workspaceId, request.Properties, cancellationToken);
            }

            var properties = ToProperties(result, workspaceId);
            properties.Tags = ToTagIds(result);

            if (request.Properties.Tags is { } desiredTags)
            {
                properties.Tags = await SyncTagsAsync(
                    client,
                    workspaceId,
                    RequireGuid(properties.Id, $"{FabricItemType} ID"),
                    desiredTags,
                    properties.Tags ?? [],
                    cancellationToken);
            }

            return BuildResponse(request.Type, request.ApiVersion, properties);
        });

    protected override Task<ResourceResponse> Get(ReferenceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            var workspaceId = ParseGuid(request.Identifiers.WorkspaceId, "workspaceId");
            var itemId = RequireGuid(request.Identifiers.Id, $"{FabricItemType} ID");
            var result = await GetItemAsync(client, workspaceId, itemId, cancellationToken);

            var properties = ToProperties(result, workspaceId);
            properties.Tags = ToTagIds(result);

            return BuildResponse(request.Type, request.ApiVersion, properties);
        });

    protected override Task<ResourceResponse> Delete(ReferenceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            await DeleteItemAsync(
                client,
                ParseGuid(request.Identifiers.WorkspaceId, "workspaceId"),
                RequireGuid(request.Identifiers.Id, $"{FabricItemType} ID"),
                cancellationToken);

            return GetResponse(request, properties: null);
        });

    protected override FabricItemIdentifiers GetIdentifiers(TProperties properties)
        => new()
        {
            WorkspaceId = properties.WorkspaceId,
            Id = properties.Id,
        };

    protected ResourceResponse BuildResponse(string type, string? apiVersion, TProperties properties)
        => new()
        {
            Type = type,
            ApiVersion = apiVersion,
            Properties = properties,
            Identifiers = GetIdentifiers(properties),
        };
}
