using Microsoft.Fabric.Api;
using Microsoft.Fabric.Api.Core.Models;
using MirroredDatabaseSdk = Microsoft.Fabric.Api.MirroredDatabase.Models;

namespace Bicep.Extension.Fabric.Handlers;

public sealed class MirroredDatabaseHandler : FabricTypedItemHandlerBase<MirroredDatabase, MirroredDatabaseSdk.MirroredDatabase>
{
    protected override ItemType FabricItemType => ItemType.MirroredDatabase;

    protected override async Task<MirroredDatabaseSdk.MirroredDatabase> CreateItemAsync(FabricClient client, Guid workspaceId, MirroredDatabase properties, CancellationToken cancellationToken)
    {
        if (properties.Definition is not { } definition)
        {
            throw new ResourceErrorException(
                "MissingDefinition",
                "Creating a Mirrored Database requires 'definition', which carries the mirroring configuration.",
                "definition");
        }

        var create = new MirroredDatabaseSdk.CreateMirroredDatabaseRequest(properties.DisplayName, ToSdkDefinition(definition))
        {
            Description = properties.Description,
            FolderId = ParseOptionalGuid(properties.FolderId, "folderId"),
        };

        return (await client.MirroredDatabase.Items.CreateMirroredDatabaseAsync(workspaceId, create, cancellationToken)).Value;
    }

    protected override async Task<MirroredDatabaseSdk.MirroredDatabase> UpdateItemAsync(FabricClient client, Guid workspaceId, Guid itemId, MirroredDatabase properties, CancellationToken cancellationToken)
    {
        var update = new MirroredDatabaseSdk.UpdateMirroredDatabaseRequest
        {
            DisplayName = properties.DisplayName,
            Description = properties.Description,
        };

        return (await client.MirroredDatabase.Items.UpdateMirroredDatabaseAsync(workspaceId, itemId, update, cancellationToken)).Value;
    }

    protected override async Task<MirroredDatabaseSdk.MirroredDatabase> GetItemAsync(FabricClient client, Guid workspaceId, Guid itemId, CancellationToken cancellationToken)
        => (await client.MirroredDatabase.Items.GetMirroredDatabaseAsync(workspaceId, itemId, cancellationToken)).Value;

    protected override Task DeleteItemAsync(FabricClient client, Guid workspaceId, Guid itemId, CancellationToken cancellationToken)
        => client.MirroredDatabase.Items.DeleteMirroredDatabaseAsync(workspaceId, itemId, cancellationToken);

    protected override Task UpdateDefinitionAsync(FabricClient client, Guid workspaceId, Guid itemId, FabricItemDefinition definition, CancellationToken cancellationToken)
        => client.MirroredDatabase.Items.UpdateMirroredDatabaseDefinitionAsync(
            workspaceId,
            itemId,
            new MirroredDatabaseSdk.UpdateMirroredDatabaseDefinitionRequest(ToSdkDefinition(definition)),
            cancellationToken: cancellationToken);

    protected override MirroredDatabase ToProperties(MirroredDatabaseSdk.MirroredDatabase item, Guid workspaceId)
        => new()
        {
            WorkspaceId = (item.WorkspaceId ?? workspaceId).ToString(),
            Id = item.Id?.ToString(),
            DisplayName = item.DisplayName,
            Description = item.Description,
            FolderId = item.FolderId?.ToString(),
            Type = item.Type.ToString(),
            DefaultSchema = item.Properties?.DefaultSchema,
            OneLakeTablesPath = item.Properties?.OneLakeTablesPath,
            SqlEndpointProperties = item.Properties?.SqlEndpointProperties is { } sqlEndpoint
                ? new FabricSqlEndpointProperties
                {
                    Id = sqlEndpoint.Id,
                    ConnectionString = sqlEndpoint.ConnectionString,
                    ProvisioningStatus = sqlEndpoint.ProvisioningStatus.ToString(),
                }
                : null,
        };

    private static MirroredDatabaseSdk.MirroredDatabaseDefinition ToSdkDefinition(FabricItemDefinition definition)
        => new(definition.Parts.Select(part => new MirroredDatabaseSdk.MirroredDatabaseDefinitionPart
        {
            Path = part.Path,
            Payload = part.Payload,
            PayloadType = PayloadType.InlineBase64,
        }))
        {
            Format = definition.Format,
        };
}
