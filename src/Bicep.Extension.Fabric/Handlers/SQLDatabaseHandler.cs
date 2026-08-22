using System.Globalization;
using Microsoft.Fabric.Api;
using Microsoft.Fabric.Api.Core.Models;
using SqlDbSdk = Microsoft.Fabric.Api.SQLDatabase.Models;

namespace Bicep.Extension.Fabric.Handlers;

public sealed class SQLDatabaseHandler : FabricTypedItemHandlerBase<SQLDatabase, SqlDbSdk.SQLDatabase>
{
    protected override ItemType FabricItemType => ItemType.SQLDatabase;

    protected override async Task<SqlDbSdk.SQLDatabase> CreateItemAsync(FabricClient client, Guid workspaceId, SQLDatabase properties, CancellationToken cancellationToken)
    {
        var create = new SqlDbSdk.CreateSQLDatabaseRequest(properties.DisplayName)
        {
            Description = properties.Description,
            FolderId = ParseOptionalGuid(properties.FolderId, "folderId"),
            CreationPayload = properties.Configuration is { } configuration ? ToSdkCreationPayload(configuration) : null,
            Definition = properties.Definition is { } definition ? ToSdkDefinition(definition) : null,
        };

        return (await client.SQLDatabase.Items.CreateSQLDatabaseAsync(workspaceId, create, cancellationToken)).Value;
    }

    protected override async Task<SqlDbSdk.SQLDatabase> UpdateItemAsync(FabricClient client, Guid workspaceId, Guid itemId, SQLDatabase properties, CancellationToken cancellationToken)
    {
        var update = new SqlDbSdk.UpdateSQLDatabaseRequest
        {
            Description = properties.Description,
        };

        return (await client.SQLDatabase.Items.UpdateSQLDatabaseAsync(workspaceId, itemId, update, cancellationToken)).Value;
    }

    protected override async Task<SqlDbSdk.SQLDatabase> GetItemAsync(FabricClient client, Guid workspaceId, Guid itemId, CancellationToken cancellationToken)
        => (await client.SQLDatabase.Items.GetSQLDatabaseAsync(workspaceId, itemId, cancellationToken)).Value;

    protected override Task DeleteItemAsync(FabricClient client, Guid workspaceId, Guid itemId, CancellationToken cancellationToken)
        => client.SQLDatabase.Items.DeleteSQLDatabaseAsync(workspaceId, itemId, cancellationToken);

    protected override Task UpdateDefinitionAsync(FabricClient client, Guid workspaceId, Guid itemId, FabricItemDefinition definition, CancellationToken cancellationToken)
        => client.SQLDatabase.Items.UpdateSQLDatabasesDefinitionAsync(
            workspaceId,
            itemId,
            new SqlDbSdk.UpdateSQLDatabaseDefinitionRequest(ToSdkDefinition(definition)),
            cancellationToken: cancellationToken);

    protected override SQLDatabase ToProperties(SqlDbSdk.SQLDatabase item, Guid workspaceId)
        => new()
        {
            WorkspaceId = (item.WorkspaceId ?? workspaceId).ToString(),
            Id = item.Id?.ToString(),
            DisplayName = item.DisplayName,
            Description = item.Description,
            FolderId = item.FolderId?.ToString(),
            Type = item.Type.ToString(),
            ConnectionString = item.Properties?.ConnectionString,
            DatabaseName = item.Properties?.DatabaseName,
            ServerFqdn = item.Properties?.ServerFqdn,
            EarliestRestorePoint = item.Properties?.EarliestRestorePoint?.ToString("o"),
            LatestRestorePoint = item.Properties?.LatestRestorePoint?.ToString("o"),
            BackupRetentionDays = item.Properties?.BackupRetentionDays,
            Collation = item.Properties?.Collation,
        };

    private static SqlDbSdk.SQLDatabaseDefinition ToSdkDefinition(FabricItemDefinition definition)
        => new(definition.Parts.Select(part => new SqlDbSdk.SQLDatabasePublicDefinitionPart
        {
            Path = part.Path,
            Payload = part.Payload,
            PayloadType = PayloadType.InlineBase64,
        }))
        {
            Format = definition.Format,
        };

    private static DateTimeOffset? ParseOptionalDateTime(string? value, string target)
        => value is null
            ? null
            : DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result)
                ? result
                : throw new ResourceErrorException("InvalidDateTime", $"'{value}' is not a valid ISO 8601 date and time.", target);

    private static ItemReferenceById? ToSdkSourceReference(SQLDatabaseSourceReference? reference)
        => reference is null
            ? null
            : new ItemReferenceById(
                ParseGuid(reference.ItemId, "configuration.sourceDatabaseReference.itemId"),
                ParseGuid(reference.WorkspaceId, "configuration.sourceDatabaseReference.workspaceId"));

    private static SqlDbSdk.SQLDatabaseCreationPayload ToSdkCreationPayload(SQLDatabaseConfiguration configuration)
        => configuration.CreationMode switch
        {
            SQLDatabaseCreationMode.New => new SqlDbSdk.NewSQLDatabaseCreationPayload
            {
                Collation = configuration.Collation,
                BackupRetentionDays = configuration.BackupRetentionDays,
                RestorePointInTime = ParseOptionalDateTime(configuration.RestorePointInTime, "configuration.restorePointInTime"),
                SourceDatabaseReference = ToSdkSourceReference(configuration.SourceDatabaseReference),
            },
            SQLDatabaseCreationMode.Restore => new SqlDbSdk.RestoreSQLDatabaseCreationPayload
            {
                BackupRetentionDays = configuration.BackupRetentionDays,
                RestorePointInTime = ParseOptionalDateTime(configuration.RestorePointInTime, "configuration.restorePointInTime"),
                SourceDatabaseReference = ToSdkSourceReference(configuration.SourceDatabaseReference)
                    ?? throw new ResourceErrorException("MissingSourceDatabaseReference", "configuration.sourceDatabaseReference is required when creationMode is 'Restore'."),
            },
            SQLDatabaseCreationMode.RestoreDeletedDatabase => new SqlDbSdk.RestoreDeletedDatabaseCreationPayload(
                configuration.RestorableDeletedDatabaseName
                    ?? throw new ResourceErrorException("MissingRestorableDeletedDatabaseName", "configuration.restorableDeletedDatabaseName is required when creationMode is 'RestoreDeletedDatabase'."))
            {
                BackupRetentionDays = configuration.BackupRetentionDays,
                RestorePointInTime = ParseOptionalDateTime(configuration.RestorePointInTime, "configuration.restorePointInTime"),
                SourceDatabaseReference = ToSdkSourceReference(configuration.SourceDatabaseReference),
            },
            _ => throw new ResourceErrorException("InvalidCreationMode", $"Unsupported creation mode '{configuration.CreationMode}'."),
        };
}
