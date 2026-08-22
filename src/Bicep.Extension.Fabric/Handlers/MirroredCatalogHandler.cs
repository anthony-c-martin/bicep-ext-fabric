using Microsoft.Fabric.Api;
using Microsoft.Fabric.Api.Core.Models;
using MirroredCatalogSdk = Microsoft.Fabric.Api.MirroredCatalog.Models;

namespace Bicep.Extension.Fabric.Handlers;

public sealed class MirroredCatalogHandler : FabricTypedItemHandlerBase<MirroredCatalog, MirroredCatalogSdk.MirroredCatalog>
{
    protected override ItemType FabricItemType => ItemType.MirroredCatalog;

    protected override async Task<MirroredCatalogSdk.MirroredCatalog> CreateItemAsync(FabricClient client, Guid workspaceId, MirroredCatalog properties, CancellationToken cancellationToken)
    {
        if (properties.Definition is not { } definition)
        {
            throw new ResourceErrorException(
                "MissingDefinition",
                "Creating a Mirrored Catalog requires 'definition', which carries the mirroring configuration.",
                "definition");
        }

        var create = new MirroredCatalogSdk.CreateMirroredCatalogRequest(properties.DisplayName, ToSdkDefinition(definition))
        {
            Description = properties.Description,
            FolderId = ParseOptionalGuid(properties.FolderId, "folderId"),
        };

        return (await client.MirroredCatalog.Items.CreateMirroredCatalogAsync(workspaceId, create, cancellationToken)).Value;
    }

    protected override async Task<MirroredCatalogSdk.MirroredCatalog> UpdateItemAsync(FabricClient client, Guid workspaceId, Guid itemId, MirroredCatalog properties, CancellationToken cancellationToken)
    {
        var update = new MirroredCatalogSdk.UpdateMirroredCatalogRequest
        {
            DisplayName = properties.DisplayName,
            Description = properties.Description,
        };

        return (await client.MirroredCatalog.Items.UpdateMirroredCatalogAsync(workspaceId, itemId, update, cancellationToken)).Value;
    }

    protected override async Task<MirroredCatalogSdk.MirroredCatalog> GetItemAsync(FabricClient client, Guid workspaceId, Guid itemId, CancellationToken cancellationToken)
        => (await client.MirroredCatalog.Items.GetMirroredCatalogAsync(workspaceId, itemId, cancellationToken)).Value;

    protected override Task DeleteItemAsync(FabricClient client, Guid workspaceId, Guid itemId, CancellationToken cancellationToken)
        => client.MirroredCatalog.Items.DeleteMirroredCatalogAsync(workspaceId, itemId, cancellationToken);

    protected override Task UpdateDefinitionAsync(FabricClient client, Guid workspaceId, Guid itemId, FabricItemDefinition definition, CancellationToken cancellationToken)
        => client.MirroredCatalog.Items.UpdateMirroredCatalogDefinitionAsync(
            workspaceId,
            itemId,
            new MirroredCatalogSdk.UpdateMirroredCatalogDefinitionRequest(ToSdkDefinition(definition)),
            cancellationToken: cancellationToken);

    protected override MirroredCatalog ToProperties(MirroredCatalogSdk.MirroredCatalog item, Guid workspaceId)
        => new()
        {
            WorkspaceId = (item.WorkspaceId ?? workspaceId).ToString(),
            Id = item.Id?.ToString(),
            DisplayName = item.DisplayName,
            Description = item.Description,
            FolderId = item.FolderId?.ToString(),
            Type = item.Type.ToString(),
            SourceType = item.Properties?.SourceType,
            ConnectionId = item.Properties?.ConnectionId?.ToString(),
            Scope = item.Properties?.Scope?.ToArray(),
            OneLakeTablesPath = item.Properties?.OneLakeTablesPath,
            SqlEndpointProperties = item.Properties?.SqlEndpointProperties is { } sqlEndpoint
                ? new FabricSqlEndpointProperties
                {
                    Id = sqlEndpoint.Id?.ToString(),
                    ConnectionString = sqlEndpoint.ConnectionString,
                    ProvisioningStatus = sqlEndpoint.ProvisioningStatus.ToString(),
                }
                : null,
        };

    private static MirroredCatalogSdk.MirroredCatalogDefinition ToSdkDefinition(FabricItemDefinition definition)
        => new(definition.Parts.Select(part => new MirroredCatalogSdk.MirroredCatalogDefinitionPart
        {
            Path = part.Path,
            Payload = part.Payload,
            PayloadType = PayloadType.InlineBase64,
        }))
        {
            Format = definition.Format,
        };
}
