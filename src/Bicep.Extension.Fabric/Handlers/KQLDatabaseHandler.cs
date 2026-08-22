using Microsoft.Fabric.Api;
using Microsoft.Fabric.Api.Core.Models;
using KQLDatabaseSdk = Microsoft.Fabric.Api.KQLDatabase.Models;

namespace Bicep.Extension.Fabric.Handlers;

public sealed class KQLDatabaseHandler : FabricTypedItemHandlerBase<KQLDatabase, KQLDatabaseSdk.KQLDatabase>
{
    protected override ItemType FabricItemType => ItemType.KQLDatabase;

    protected override async Task<KQLDatabaseSdk.KQLDatabase> CreateItemAsync(FabricClient client, Guid workspaceId, KQLDatabase properties, CancellationToken cancellationToken)
    {
        var create = new KQLDatabaseSdk.CreateKQLDatabaseRequest(properties.DisplayName)
        {
            Description = properties.Description,
            FolderId = ParseOptionalGuid(properties.FolderId, "folderId"),
            CreationPayload = properties.Configuration is { } configuration ? ToCreationPayload(configuration) : null,
            Definition = properties.Definition is { } definition ? ToSdkDefinition(definition) : null,
        };

        if (create.CreationPayload is null && create.Definition is null)
        {
            throw new ResourceErrorException(
                "InvalidConfiguration",
                "A KQL Database requires either 'configuration' or 'definition'.",
                "configuration");
        }

        return (await client.KQLDatabase.Items.CreateKQLDatabaseAsync(workspaceId, create, cancellationToken)).Value;
    }

    protected override async Task<KQLDatabaseSdk.KQLDatabase> UpdateItemAsync(FabricClient client, Guid workspaceId, Guid itemId, KQLDatabase properties, CancellationToken cancellationToken)
    {
        var update = new KQLDatabaseSdk.UpdateKQLDatabaseRequest
        {
            DisplayName = properties.DisplayName,
            Description = properties.Description,
        };

        return (await client.KQLDatabase.Items.UpdateKQLDatabaseAsync(workspaceId, itemId, update, cancellationToken)).Value;
    }

    protected override async Task<KQLDatabaseSdk.KQLDatabase> GetItemAsync(FabricClient client, Guid workspaceId, Guid itemId, CancellationToken cancellationToken)
        => (await client.KQLDatabase.Items.GetKQLDatabaseAsync(workspaceId, itemId, cancellationToken)).Value;

    protected override Task DeleteItemAsync(FabricClient client, Guid workspaceId, Guid itemId, CancellationToken cancellationToken)
        => client.KQLDatabase.Items.DeleteKQLDatabaseAsync(workspaceId, itemId, cancellationToken);

    protected override Task UpdateDefinitionAsync(FabricClient client, Guid workspaceId, Guid itemId, FabricItemDefinition definition, CancellationToken cancellationToken)
        => client.KQLDatabase.Items.UpdateKQLDatabaseDefinitionAsync(
            workspaceId,
            itemId,
            new KQLDatabaseSdk.UpdateKQLDatabaseDefinitionRequest(ToSdkDefinition(definition)),
            cancellationToken: cancellationToken);

    protected override KQLDatabase ToProperties(KQLDatabaseSdk.KQLDatabase item, Guid workspaceId)
        => new()
        {
            WorkspaceId = (item.WorkspaceId ?? workspaceId).ToString(),
            Id = item.Id?.ToString(),
            DisplayName = item.DisplayName,
            Description = item.Description,
            FolderId = item.FolderId?.ToString(),
            Type = item.Type.ToString(),
            Configuration = item.Properties?.ParentEventhouseItemId is { } parentEventhouseId
                ? new KQLDatabaseConfiguration
                {
                    EventhouseId = parentEventhouseId,
                    DatabaseType = ToDatabaseType(item.Properties?.DatabaseType),
                }
                : null,
            DatabaseType = ToDatabaseType(item.Properties?.DatabaseType),
            QueryServiceUri = item.Properties?.QueryServiceUri,
            IngestionServiceUri = item.Properties?.IngestionServiceUri,
        };

    private static KQLDatabaseType? ToDatabaseType(KQLDatabaseSdk.KqlDatabaseType? databaseType)
        => databaseType is { } value && Enum.TryParse<KQLDatabaseType>(value.ToString(), out var result)
            ? result
            : null;

    private static KQLDatabaseSdk.KQLDatabaseDefinition ToSdkDefinition(FabricItemDefinition definition)
        => new(definition.Parts.Select(part => new KQLDatabaseSdk.KQLDatabaseDefinitionPart
        {
            Path = part.Path,
            Payload = part.Payload,
            PayloadType = PayloadType.InlineBase64,
        }))
        {
            Format = definition.Format,
        };

    private KQLDatabaseSdk.KQLDatabaseCreationPayload ToCreationPayload(KQLDatabaseConfiguration configuration)
    {
        var eventhouseId = ParseGuid(configuration.EventhouseId, "configuration.eventhouseId");

        if (configuration.DatabaseType is not KQLDatabaseType.Shortcut)
        {
            if (configuration.SourceClusterUri is not null || configuration.SourceDatabaseName is not null || configuration.InvitationToken is not null)
            {
                throw new ResourceErrorException(
                    "InvalidConfiguration",
                    "'sourceClusterUri', 'sourceDatabaseName' and 'invitationToken' are only valid when 'configuration.databaseType' is 'Shortcut'.",
                    "configuration");
            }

            return new KQLDatabaseSdk.ReadWriteDatabaseCreationPayload(eventhouseId);
        }

        if (configuration.InvitationToken is { } invitationToken)
        {
            if (configuration.SourceClusterUri is not null || configuration.SourceDatabaseName is not null)
            {
                throw new ResourceErrorException(
                    "InvalidConfiguration",
                    "'configuration.invitationToken' cannot be combined with 'sourceClusterUri' or 'sourceDatabaseName'.",
                    "configuration.invitationToken");
            }

            return new KQLDatabaseSdk.ShortcutDatabaseCreationPayload(eventhouseId) { InvitationToken = invitationToken };
        }

        if (configuration.SourceClusterUri is not { } sourceClusterUri || configuration.SourceDatabaseName is not { } sourceDatabaseName)
        {
            throw new ResourceErrorException(
                "InvalidConfiguration",
                "A shortcut KQL Database requires either 'configuration.invitationToken', or both 'configuration.sourceClusterUri' and 'configuration.sourceDatabaseName'.",
                "configuration");
        }

        return new KQLDatabaseSdk.ShortcutDatabaseCreationPayload(eventhouseId)
        {
            SourceClusterUri = sourceClusterUri,
            SourceDatabaseName = sourceDatabaseName,
        };
    }
}
