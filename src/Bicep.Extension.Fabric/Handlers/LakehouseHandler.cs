using Microsoft.Fabric.Api;
using Microsoft.Fabric.Api.Core.Models;
using LakehouseSdk = Microsoft.Fabric.Api.Lakehouse.Models;

namespace Bicep.Extension.Fabric.Handlers;

public sealed class LakehouseHandler : FabricTypedItemHandlerBase<Lakehouse, LakehouseSdk.Lakehouse>
{
    protected override ItemType FabricItemType => ItemType.Lakehouse;

    protected override async Task<LakehouseSdk.Lakehouse> CreateItemAsync(FabricClient client, Guid workspaceId, Lakehouse properties, CancellationToken cancellationToken)
    {
        var create = new LakehouseSdk.CreateLakehouseRequest(properties.DisplayName)
        {
            Description = properties.Description,
            FolderId = ParseOptionalGuid(properties.FolderId, "folderId"),
            CreationPayload = new LakehouseSdk.LakehouseCreationPayload(properties.Configuration.EnableSchemas),
            Definition = properties.Definition is { } definition ? ToSdkDefinition(definition) : null,
        };

        return (await client.Lakehouse.Items.CreateLakehouseAsync(workspaceId, create, cancellationToken)).Value;
    }

    protected override async Task<LakehouseSdk.Lakehouse> UpdateItemAsync(FabricClient client, Guid workspaceId, Guid itemId, Lakehouse properties, CancellationToken cancellationToken)
    {
        var update = new LakehouseSdk.UpdateLakehouseRequest
        {
            DisplayName = properties.DisplayName,
            Description = properties.Description,
        };

        return (await client.Lakehouse.Items.UpdateLakehouseAsync(workspaceId, itemId, update, cancellationToken)).Value;
    }

    protected override async Task<LakehouseSdk.Lakehouse> GetItemAsync(FabricClient client, Guid workspaceId, Guid itemId, CancellationToken cancellationToken)
        => (await client.Lakehouse.Items.GetLakehouseAsync(workspaceId, itemId, cancellationToken)).Value;

    protected override Task DeleteItemAsync(FabricClient client, Guid workspaceId, Guid itemId, CancellationToken cancellationToken)
        => client.Lakehouse.Items.DeleteLakehouseAsync(workspaceId, itemId, cancellationToken);

    protected override Task UpdateDefinitionAsync(FabricClient client, Guid workspaceId, Guid itemId, FabricItemDefinition definition, CancellationToken cancellationToken)
        => client.Lakehouse.Items.UpdateLakehouseDefinitionAsync(
            workspaceId,
            itemId,
            new LakehouseSdk.UpdateLakehouseDefinitionRequest(ToSdkDefinition(definition)),
            cancellationToken: cancellationToken);

    protected override Lakehouse ToProperties(LakehouseSdk.Lakehouse item, Guid workspaceId)
        => new()
        {
            WorkspaceId = (item.WorkspaceId ?? workspaceId).ToString(),
            Id = item.Id?.ToString(),
            DisplayName = item.DisplayName,
            Description = item.Description,
            FolderId = item.FolderId?.ToString(),
            Type = item.Type.ToString(),
            Configuration = new LakehouseConfiguration { EnableSchemas = item.Properties?.DefaultSchema is not null },
            OneLakeFilesPath = item.Properties?.OneLakeFilesPath,
            OneLakeTablesPath = item.Properties?.OneLakeTablesPath,
            DefaultSchema = item.Properties?.DefaultSchema,
            SqlEndpointProperties = item.Properties?.SqlEndpointProperties is { } sqlEndpoint
                ? new LakehouseSqlEndpointProperties
                {
                    Id = sqlEndpoint.Id,
                    ConnectionString = sqlEndpoint.ConnectionString,
                    ProvisioningStatus = sqlEndpoint.ProvisioningStatus.ToString(),
                }
                : null,
        };

    private static LakehouseSdk.LakehouseDefinition ToSdkDefinition(FabricItemDefinition definition)
        => new(definition.Parts.Select(part => new LakehouseSdk.LakehouseDefinitionPart
        {
            Path = part.Path,
            Payload = part.Payload,
            PayloadType = PayloadType.InlineBase64,
        }))
        {
            Format = definition.Format,
        };
}
