using System.Globalization;
using Microsoft.Fabric.Api;
using Microsoft.Fabric.Api.Core.Models;
using WarehouseSnapshotSdk = Microsoft.Fabric.Api.WarehouseSnapshot.Models;

namespace Bicep.Extension.Fabric.Handlers;

public sealed class WarehouseSnapshotHandler : FabricTypedItemHandlerBase<WarehouseSnapshot, WarehouseSnapshotSdk.WarehouseSnapshot>
{
    protected override ItemType FabricItemType => ItemType.WarehouseSnapshot;

    protected override async Task<WarehouseSnapshotSdk.WarehouseSnapshot> CreateItemAsync(FabricClient client, Guid workspaceId, WarehouseSnapshot properties, CancellationToken cancellationToken)
    {
        var create = new WarehouseSnapshotSdk.CreateWarehouseSnapshotRequest(properties.DisplayName)
        {
            Description = properties.Description,
            FolderId = ParseOptionalGuid(properties.FolderId, "folderId"),
            CreationPayload = new WarehouseSnapshotSdk.WarehouseSnapshotCreationPayload(
                ParseGuid(properties.Configuration.ParentWarehouseId, "configuration.parentWarehouseId"))
            {
                SnapshotDateTime = ParseOptionalTimestamp(properties.Configuration.SnapshotDateTime),
            },
        };

        return (await client.WarehouseSnapshot.Items.CreateWarehouseSnapshotAsync(workspaceId, create, cancellationToken)).Value;
    }

    protected override async Task<WarehouseSnapshotSdk.WarehouseSnapshot> UpdateItemAsync(FabricClient client, Guid workspaceId, Guid itemId, WarehouseSnapshot properties, CancellationToken cancellationToken)
    {
        var update = new WarehouseSnapshotSdk.UpdateWarehouseSnapshotRequest
        {
            DisplayName = properties.DisplayName,
            Description = properties.Description,
            Properties = ParseOptionalTimestamp(properties.Configuration.SnapshotDateTime) is { } snapshotDateTime
                ? new WarehouseSnapshotSdk.WarehouseSnapshotUpdateProperties(snapshotDateTime)
                : null,
        };

        return (await client.WarehouseSnapshot.Items.UpdateWarehouseSnapshotAsync(workspaceId, itemId, update, cancellationToken)).Value;
    }

    protected override async Task<WarehouseSnapshotSdk.WarehouseSnapshot> GetItemAsync(FabricClient client, Guid workspaceId, Guid itemId, CancellationToken cancellationToken)
        => (await client.WarehouseSnapshot.Items.GetWarehouseSnapshotAsync(workspaceId, itemId, cancellationToken)).Value;

    protected override Task DeleteItemAsync(FabricClient client, Guid workspaceId, Guid itemId, CancellationToken cancellationToken)
        => client.WarehouseSnapshot.Items.DeleteWarehouseSnapshotAsync(workspaceId, itemId, cancellationToken);

    protected override WarehouseSnapshot ToProperties(WarehouseSnapshotSdk.WarehouseSnapshot item, Guid workspaceId)
        => new()
        {
            WorkspaceId = (item.WorkspaceId ?? workspaceId).ToString(),
            Id = item.Id?.ToString(),
            DisplayName = item.DisplayName,
            Description = item.Description,
            FolderId = item.FolderId?.ToString(),
            Type = item.Type.ToString(),
            Configuration = new WarehouseSnapshotConfiguration
            {
                ParentWarehouseId = item.Properties?.ParentWarehouseId ?? string.Empty,
                SnapshotDateTime = item.Properties?.SnapshotDateTime.ToString("O", CultureInfo.InvariantCulture),
            },
            ConnectionString = item.Properties?.ConnectionString,
        };

    private static DateTimeOffset? ParseOptionalTimestamp(string? value)
        => value is null ? null : ParseTimestamp(value, "configuration.snapshotDateTime");
}
