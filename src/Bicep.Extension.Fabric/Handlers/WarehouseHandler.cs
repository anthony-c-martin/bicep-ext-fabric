using Microsoft.Fabric.Api;
using Microsoft.Fabric.Api.Core.Models;
using WarehouseSdk = Microsoft.Fabric.Api.Warehouse.Models;

namespace Bicep.Extension.Fabric.Handlers;

public sealed class WarehouseHandler : FabricTypedItemHandlerBase<Warehouse, WarehouseSdk.Warehouse>
{
    protected override ItemType FabricItemType => ItemType.Warehouse;

    protected override async Task<WarehouseSdk.Warehouse> CreateItemAsync(FabricClient client, Guid workspaceId, Warehouse properties, CancellationToken cancellationToken)
    {
        var create = new WarehouseSdk.CreateWarehouseRequest(properties.DisplayName)
        {
            Description = properties.Description,
            FolderId = ParseOptionalGuid(properties.FolderId, "folderId"),
            CreationPayload = properties.Configuration is { } configuration
                ? new WarehouseSdk.WarehouseCreationPayload(ToSdkCollationType(configuration.CollationType))
                : null,
        };

        return (await client.Warehouse.Items.CreateWarehouseAsync(workspaceId, create, cancellationToken)).Value;
    }

    protected override async Task<WarehouseSdk.Warehouse> UpdateItemAsync(FabricClient client, Guid workspaceId, Guid itemId, Warehouse properties, CancellationToken cancellationToken)
    {
        var update = new WarehouseSdk.UpdateWarehouseRequest
        {
            DisplayName = properties.DisplayName,
            Description = properties.Description,
        };

        return (await client.Warehouse.Items.UpdateWarehouseAsync(workspaceId, itemId, update, cancellationToken)).Value;
    }

    protected override async Task<WarehouseSdk.Warehouse> GetItemAsync(FabricClient client, Guid workspaceId, Guid itemId, CancellationToken cancellationToken)
        => (await client.Warehouse.Items.GetWarehouseAsync(workspaceId, itemId, cancellationToken)).Value;

    protected override Task DeleteItemAsync(FabricClient client, Guid workspaceId, Guid itemId, CancellationToken cancellationToken)
        => client.Warehouse.Items.DeleteWarehouseAsync(workspaceId, itemId, cancellationToken);

    protected override Warehouse ToProperties(WarehouseSdk.Warehouse item, Guid workspaceId)
        => new()
        {
            WorkspaceId = (item.WorkspaceId ?? workspaceId).ToString(),
            Id = item.Id?.ToString(),
            DisplayName = item.DisplayName,
            Description = item.Description,
            FolderId = item.FolderId?.ToString(),
            Type = item.Type.ToString(),
            ConnectionString = item.Properties?.ConnectionString,
            CreatedDate = item.Properties?.CreatedDate.ToString("o"),
            LastUpdatedTime = item.Properties?.LastUpdatedTime.ToString("o"),
            CollationType = item.Properties?.CollationType is { } collationType
                ? Enum.Parse<CollationType>(collationType.ToString())
                : null,
        };

    private static WarehouseSdk.CollationType ToSdkCollationType(CollationType value)
        => value switch
        {
            CollationType.Latin1_General_100_BIN2_UTF8 => WarehouseSdk.CollationType.Latin1General100BIN2UTF8,
            CollationType.Latin1_General_100_CI_AS_KS_WS_SC_UTF8 => WarehouseSdk.CollationType.Latin1General100CIASKSWSSCUTF8,
            _ => throw new ResourceErrorException("InvalidCollationType", $"Unsupported collation type '{value}'."),
        };
}
