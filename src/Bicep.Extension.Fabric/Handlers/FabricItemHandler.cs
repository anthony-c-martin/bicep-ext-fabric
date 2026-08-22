using Bicep.Local.Extension.Host.Handlers;
using Microsoft.Fabric.Api.Core.Models;

namespace Bicep.Extension.Fabric.Handlers;

internal interface IFabricItemHandlerMetadata
{
    string ItemTypeName { get; }
}

public abstract class FabricItemHandlerBase<TProperties> : FabricResourceHandlerBase<TProperties, FabricItemIdentifiers>, IFabricItemHandlerMetadata
    where TProperties : FabricItem, new()
{
    protected abstract ItemType FabricItemType { get; }

    string IFabricItemHandlerMetadata.ItemTypeName => FabricItemType.ToString();

    protected override Task<ResourceResponse> Preview(ResourceRequest request, CancellationToken cancellationToken)
        => Task.FromResult(GetResponse(request));

    protected override Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            Item result;

            var workspaceId = ParseId(request.Properties.WorkspaceId, "workspaceId");

            if (request.Properties.Id is { } itemIdValue)
            {
                var itemId = ParseId(itemIdValue, "id");
                var update = new UpdateItemRequest
                {
                    DisplayName = request.Properties.DisplayName,
                    Description = request.Properties.Description,
                };

                result = (await client.Core.Items.UpdateItemAsync(
                    workspaceId,
                    itemId,
                    update,
                    cancellationToken)).Value;

                if (request.Properties.FolderId is { } folderId)
                {
                    await client.Core.Items.MoveItemAsync(
                        workspaceId,
                        itemId,
                        new MoveItemRequest { TargetFolderId = ParseId(folderId, "folderId") },
                        cancellationToken);
                }

                if (request.Properties.Definition is { } definition)
                {
                    await client.Core.Items.UpdateItemDefinitionAsync(
                        workspaceId,
                        itemId,
                        new UpdateItemDefinitionRequest(ToSdkDefinition(definition)),
                        cancellationToken: cancellationToken);
                }
            }
            else
            {
                var create = new CreateItemRequest(request.Properties.DisplayName, FabricItemType)
                {
                    Description = request.Properties.Description,
                    FolderId = ParseOptionalId(request.Properties.FolderId, "folderId"),
                    Definition = request.Properties.Definition is { } definition
                        ? ToSdkDefinition(definition)
                        : null,
                };

                result = (await client.Core.Items.CreateItemAsync(
                    workspaceId,
                    create,
                    cancellationToken)).Value;
            }

            var properties = ToProperties(result, workspaceId);

            if (request.Properties.Tags is { } desiredTags)
            {
                properties.Tags = await SyncTagsAsync(
                    client,
                    workspaceId,
                    RequireId(properties.Id),
                    desiredTags,
                    properties.Tags ?? [],
                    cancellationToken);
            }

            return BuildResponse(request.Type, request.ApiVersion, properties);
        });

    protected override Task<ResourceResponse> Get(ReferenceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            var workspaceId = ParseId(request.Identifiers.WorkspaceId, "workspaceId");
            var result = (await client.Core.Items.GetItemAsync(
                workspaceId,
                RequireId(request.Identifiers.Id),
                cancellationToken: cancellationToken)).Value;

            return BuildResponse(request.Type, request.ApiVersion, ToProperties(result, workspaceId));
        });

    protected override Task<ResourceResponse> Delete(ReferenceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            await client.Core.Items.DeleteItemAsync(
                ParseId(request.Identifiers.WorkspaceId, "workspaceId"),
                RequireId(request.Identifiers.Id),
                cancellationToken: cancellationToken);

            return GetResponse(request, properties: null);
        });

    protected override FabricItemIdentifiers GetIdentifiers(TProperties properties)
        => new()
        {
            WorkspaceId = properties.WorkspaceId,
            Id = properties.Id,
        };

    private static Guid RequireId(string? id)
        => id is null
            ? throw new ResourceErrorException("MissingIdentifier", "The Fabric item ID is required for this operation.")
            : ParseId(id, "id");

    private static Guid ParseId(string value, string target)
        => Guid.TryParse(value, out var result)
            ? result
            : throw new ResourceErrorException("InvalidIdentifier", $"'{value}' is not a valid GUID.", target);

    private static Guid? ParseOptionalId(string? value, string target)
        => value is null ? null : ParseId(value, target);

    private static ItemDefinition ToSdkDefinition(FabricItemDefinition definition)
    {
        var result = new ItemDefinition(definition.Parts.Select(part =>
            new ItemDefinitionPart(part.Path, part.Payload, PayloadType.InlineBase64)))
        {
            Format = definition.Format,
        };

        return result;
    }

    private static TProperties ToProperties(Item item, Guid workspaceId)
        => new()
        {
            WorkspaceId = (item.WorkspaceId ?? workspaceId).ToString(),
            Id = item.Id?.ToString(),
            DisplayName = item.DisplayName,
            Description = item.Description,
            FolderId = item.FolderId?.ToString(),
            Tags = ToTagIds(item),
            Type = item.Type.ToString(),
        };

    private static ResourceResponse BuildResponse(string type, string? apiVersion, TProperties properties)
        => new()
        {
            Type = type,
            ApiVersion = apiVersion,
            Properties = properties,
            Identifiers = new FabricItemIdentifiers
            {
                WorkspaceId = properties.WorkspaceId,
                Id = properties.Id,
            },
        };
}