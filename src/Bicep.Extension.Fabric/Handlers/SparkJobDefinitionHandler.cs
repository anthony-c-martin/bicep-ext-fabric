using Microsoft.Fabric.Api;
using Microsoft.Fabric.Api.Core.Models;
using SparkJobDefinitionSdk = Microsoft.Fabric.Api.SparkJobDefinition.Models;

namespace Bicep.Extension.Fabric.Handlers;

public sealed class SparkJobDefinitionHandler : FabricTypedItemHandlerBase<SparkJobDefinition, SparkJobDefinitionSdk.SparkJobDefinition>
{
    protected override ItemType FabricItemType => ItemType.SparkJobDefinition;

    protected override async Task<SparkJobDefinitionSdk.SparkJobDefinition> CreateItemAsync(FabricClient client, Guid workspaceId, SparkJobDefinition properties, CancellationToken cancellationToken)
    {
        var create = new SparkJobDefinitionSdk.CreateSparkJobDefinitionRequest(properties.DisplayName)
        {
            Description = properties.Description,
            FolderId = ParseOptionalGuid(properties.FolderId, "folderId"),
            Definition = properties.Definition is { } definition ? ToSdkDefinition(definition) : null,
        };

        return (await client.SparkJobDefinition.Items.CreateSparkJobDefinitionAsync(workspaceId, create, cancellationToken)).Value;
    }

    protected override async Task<SparkJobDefinitionSdk.SparkJobDefinition> UpdateItemAsync(FabricClient client, Guid workspaceId, Guid itemId, SparkJobDefinition properties, CancellationToken cancellationToken)
    {
        var update = new SparkJobDefinitionSdk.UpdateSparkJobDefinitionRequest
        {
            DisplayName = properties.DisplayName,
            Description = properties.Description,
        };

        return (await client.SparkJobDefinition.Items.UpdateSparkJobDefinitionAsync(workspaceId, itemId, update, cancellationToken)).Value;
    }

    protected override async Task<SparkJobDefinitionSdk.SparkJobDefinition> GetItemAsync(FabricClient client, Guid workspaceId, Guid itemId, CancellationToken cancellationToken)
        => (await client.SparkJobDefinition.Items.GetSparkJobDefinitionAsync(workspaceId, itemId, cancellationToken)).Value;

    protected override Task DeleteItemAsync(FabricClient client, Guid workspaceId, Guid itemId, CancellationToken cancellationToken)
        => client.SparkJobDefinition.Items.DeleteSparkJobDefinitionAsync(workspaceId, itemId, cancellationToken);

    protected override Task UpdateDefinitionAsync(FabricClient client, Guid workspaceId, Guid itemId, FabricItemDefinition definition, CancellationToken cancellationToken)
        => client.SparkJobDefinition.Items.UpdateSparkJobDefinitionDefinitionAsync(
            workspaceId,
            itemId,
            new SparkJobDefinitionSdk.UpdateSparkJobDefinitionDefinitionRequest(ToSdkDefinition(definition)),
            cancellationToken: cancellationToken);

    protected override SparkJobDefinition ToProperties(SparkJobDefinitionSdk.SparkJobDefinition item, Guid workspaceId)
        => new()
        {
            WorkspaceId = (item.WorkspaceId ?? workspaceId).ToString(),
            Id = item.Id?.ToString(),
            DisplayName = item.DisplayName,
            Description = item.Description,
            FolderId = item.FolderId?.ToString(),
            Type = item.Type.ToString(),
            OneLakeRootPath = item.Properties?.OneLakeRootPath,
        };

    private static SparkJobDefinitionSdk.SparkJobDefinitionPublicDefinition ToSdkDefinition(FabricItemDefinition definition)
        => new(definition.Parts.Select(part => new SparkJobDefinitionSdk.SparkJobDefinitionPublicDefinitionPart
        {
            Path = part.Path,
            Payload = part.Payload,
            PayloadType = PayloadType.InlineBase64,
        }))
        {
            Format = definition.Format is { } format ? new SparkJobDefinitionSdk.SparkJobDefinitionFormat(format) : null,
        };
}
