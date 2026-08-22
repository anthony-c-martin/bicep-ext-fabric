using Microsoft.Fabric.Api.Core.Models;

namespace Bicep.Extension.Fabric.Handlers;

/// <summary>
/// Base class for item types that the Fabric API only exposes for listing and reading. Such items
/// can be referenced from Bicep, but any attempt to provision or delete them fails with a clear error.
/// </summary>
public abstract class FabricReadOnlyItemHandlerBase<TProperties> : FabricResourceHandlerBase<TProperties, FabricReadOnlyItemIdentifiers>, IFabricItemHandlerMetadata
    where TProperties : FabricReadOnlyItem, new()
{
    protected abstract ItemType FabricItemType { get; }

    string IFabricItemHandlerMetadata.ItemTypeName => FabricItemType.ToString();

    protected override Task<ResourceResponse> Preview(ResourceRequest request, CancellationToken cancellationToken)
        => Task.FromResult(GetResponse(request));

    protected override Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
        => throw new ResourceErrorException(
            "ResourceNotProvisionable",
            $"The Fabric API does not support creating or updating {FabricItemType} items. Reference an existing item with an 'existing' resource instead.");

    protected override Task<ResourceResponse> Delete(ReferenceRequest request, CancellationToken cancellationToken)
        => throw new ResourceErrorException(
            "ResourceNotProvisionable",
            $"The Fabric API does not support deleting {FabricItemType} items.");

    protected override Task<ResourceResponse> Get(ReferenceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            var workspaceId = ParseGuid(request.Identifiers.WorkspaceId, "workspaceId");
            var itemId = RequireGuid(request.Identifiers.Id, $"{FabricItemType} ID");

            var item = (await client.Core.Items.GetItemAsync(workspaceId, itemId, cancellationToken: cancellationToken)).Value;

            var properties = new TProperties
            {
                WorkspaceId = (item.WorkspaceId ?? workspaceId).ToString(),
                Id = (item.Id ?? itemId).ToString(),
                DisplayName = item.DisplayName,
                Description = item.Description,
                FolderId = item.FolderId?.ToString(),
                Tags = ToTagIds(item),
                Type = item.Type.ToString(),
            };

            return new ResourceResponse
            {
                Type = request.Type,
                ApiVersion = request.ApiVersion,
                Properties = properties,
                Identifiers = GetIdentifiers(properties),
            };
        });

    protected override FabricReadOnlyItemIdentifiers GetIdentifiers(TProperties properties)
        => new()
        {
            WorkspaceId = properties.WorkspaceId,
            Id = properties.Id,
        };
}

public sealed class DashboardHandler : FabricReadOnlyItemHandlerBase<Dashboard>
{
    protected override ItemType FabricItemType => ItemType.Dashboard;
}

public sealed class DatamartHandler : FabricReadOnlyItemHandlerBase<Datamart>
{
    protected override ItemType FabricItemType => ItemType.Datamart;
}

public sealed class MirroredWarehouseHandler : FabricReadOnlyItemHandlerBase<MirroredWarehouse>
{
    protected override ItemType FabricItemType => ItemType.MirroredWarehouse;
}
