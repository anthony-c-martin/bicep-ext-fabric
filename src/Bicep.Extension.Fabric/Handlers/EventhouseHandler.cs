using Microsoft.Fabric.Api;
using Microsoft.Fabric.Api.Core.Models;
using EventhouseSdk = Microsoft.Fabric.Api.Eventhouse.Models;

namespace Bicep.Extension.Fabric.Handlers;

public sealed class EventhouseHandler : FabricTypedItemHandlerBase<Eventhouse, EventhouseSdk.Eventhouse>
{
    protected override ItemType FabricItemType => ItemType.Eventhouse;

    protected override async Task<EventhouseSdk.Eventhouse> CreateItemAsync(FabricClient client, Guid workspaceId, Eventhouse properties, CancellationToken cancellationToken)
    {
        var create = new EventhouseSdk.CreateEventhouseRequest(properties.DisplayName)
        {
            Description = properties.Description,
            FolderId = ParseOptionalGuid(properties.FolderId, "folderId"),
            CreationPayload = properties.Configuration is { } configuration
                ? new EventhouseSdk.EventhouseCreationPayload
                {
                    MinimumConsumptionUnits = ParseInvariantDouble(configuration.MinimumConsumptionUnits, "configuration.minimumConsumptionUnits"),
                }
                : null,
            Definition = properties.Definition is { } definition ? ToSdkDefinition(definition) : null,
        };

        return (await client.Eventhouse.Items.CreateEventhouseAsync(workspaceId, create, cancellationToken)).Value;
    }

    protected override async Task<EventhouseSdk.Eventhouse> UpdateItemAsync(FabricClient client, Guid workspaceId, Guid itemId, Eventhouse properties, CancellationToken cancellationToken)
    {
        var update = new EventhouseSdk.UpdateEventhouseRequest
        {
            DisplayName = properties.DisplayName,
            Description = properties.Description,
        };

        return (await client.Eventhouse.Items.UpdateEventhouseAsync(workspaceId, itemId, update, cancellationToken)).Value;
    }

    protected override async Task<EventhouseSdk.Eventhouse> GetItemAsync(FabricClient client, Guid workspaceId, Guid itemId, CancellationToken cancellationToken)
        => (await client.Eventhouse.Items.GetEventhouseAsync(workspaceId, itemId, cancellationToken)).Value;

    protected override Task DeleteItemAsync(FabricClient client, Guid workspaceId, Guid itemId, CancellationToken cancellationToken)
        => client.Eventhouse.Items.DeleteEventhouseAsync(workspaceId, itemId, cancellationToken);

    protected override Task UpdateDefinitionAsync(FabricClient client, Guid workspaceId, Guid itemId, FabricItemDefinition definition, CancellationToken cancellationToken)
        => client.Eventhouse.Items.UpdateEventhouseDefinitionAsync(
            workspaceId,
            itemId,
            new EventhouseSdk.UpdateEventhouseDefinitionRequest(ToSdkDefinition(definition)),
            cancellationToken: cancellationToken);

    protected override Eventhouse ToProperties(EventhouseSdk.Eventhouse item, Guid workspaceId)
        => new()
        {
            WorkspaceId = (item.WorkspaceId ?? workspaceId).ToString(),
            Id = item.Id?.ToString(),
            DisplayName = item.DisplayName,
            Description = item.Description,
            FolderId = item.FolderId?.ToString(),
            Type = item.Type.ToString(),
            QueryServiceUri = item.Properties?.QueryServiceUri,
            IngestionServiceUri = item.Properties?.IngestionServiceUri,
            DatabaseIds = item.Properties?.DatabasesItemIds?.Select(id => id.ToString()).ToArray(),
            MinimumConsumptionUnits = item.Properties?.MinimumConsumptionUnits?.ToString(System.Globalization.CultureInfo.InvariantCulture),
        };

    private static EventhouseSdk.EventhouseDefinition ToSdkDefinition(FabricItemDefinition definition)
        => new(definition.Parts.Select(part => new EventhouseSdk.EventhouseDefinitionPart(part.Path, part.Payload, PayloadType.InlineBase64)))
        {
            Format = definition.Format,
        };
}
