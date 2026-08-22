using Microsoft.Fabric.Api;
using Microsoft.Fabric.Api.Core.Models;
using EnvironmentSdk = Microsoft.Fabric.Api.Environment.Models;
using FabricEnvironment = Bicep.Extension.Fabric.Models.Environment;

namespace Bicep.Extension.Fabric.Handlers;

public sealed class EnvironmentHandler : FabricTypedItemHandlerBase<FabricEnvironment, EnvironmentSdk.Environment>
{
    protected override ItemType FabricItemType => ItemType.Environment;

    protected override async Task<EnvironmentSdk.Environment> CreateItemAsync(FabricClient client, Guid workspaceId, FabricEnvironment properties, CancellationToken cancellationToken)
    {
        var create = new EnvironmentSdk.CreateEnvironmentRequest(properties.DisplayName)
        {
            Description = properties.Description,
            FolderId = ParseOptionalGuid(properties.FolderId, "folderId"),
            Definition = properties.Definition is { } definition ? ToSdkDefinition(definition) : null,
        };

        return (await client.Environment.Items.CreateEnvironmentAsync(workspaceId, create, cancellationToken)).Value;
    }

    protected override async Task<EnvironmentSdk.Environment> UpdateItemAsync(FabricClient client, Guid workspaceId, Guid itemId, FabricEnvironment properties, CancellationToken cancellationToken)
    {
        var update = new EnvironmentSdk.UpdateEnvironmentRequest
        {
            DisplayName = properties.DisplayName,
            Description = properties.Description,
        };

        return (await client.Environment.Items.UpdateEnvironmentAsync(workspaceId, itemId, update, cancellationToken)).Value;
    }

    protected override async Task<EnvironmentSdk.Environment> GetItemAsync(FabricClient client, Guid workspaceId, Guid itemId, CancellationToken cancellationToken)
        => (await client.Environment.Items.GetEnvironmentAsync(workspaceId, itemId, cancellationToken)).Value;

    protected override Task DeleteItemAsync(FabricClient client, Guid workspaceId, Guid itemId, CancellationToken cancellationToken)
        => client.Environment.Items.DeleteEnvironmentAsync(workspaceId, itemId, cancellationToken);

    protected override Task UpdateDefinitionAsync(FabricClient client, Guid workspaceId, Guid itemId, FabricItemDefinition definition, CancellationToken cancellationToken)
        => client.Environment.Items.UpdateEnvironmentDefinitionAsync(
            workspaceId,
            itemId,
            new EnvironmentSdk.UpdateEnvironmentDefinitionRequest(ToSdkDefinition(definition)),
            cancellationToken: cancellationToken);

    protected override FabricEnvironment ToProperties(EnvironmentSdk.Environment item, Guid workspaceId)
        => new()
        {
            WorkspaceId = (item.WorkspaceId ?? workspaceId).ToString(),
            Id = item.Id?.ToString(),
            DisplayName = item.DisplayName,
            Description = item.Description,
            FolderId = item.FolderId?.ToString(),
            Type = item.Type.ToString(),
            PublishDetails = item.Properties?.PublishDetails is { } publishDetails
                ? new EnvironmentPublishDetails
                {
                    State = ToPublishState(publishDetails.State),
                    TargetVersion = publishDetails.TargetVersion?.ToString(),
                    StartTime = publishDetails.StartTime?.ToString("o"),
                    EndTime = publishDetails.EndTime?.ToString("o"),
                    ComponentPublishInfo = publishDetails.ComponentPublishInfo is { } componentInfo
                        ? new EnvironmentComponentPublishInfo
                        {
                            SparkLibraries = componentInfo.SparkLibraries is { } sparkLibraries
                                ? new EnvironmentComponentPublishState { State = ToPublishState(sparkLibraries.State) }
                                : null,
                            SparkSettings = componentInfo.SparkSettings is { } sparkSettings
                                ? new EnvironmentComponentPublishState { State = ToPublishState(sparkSettings.State) }
                                : null,
                        }
                        : null,
                }
                : null,
        };

    private static EnvironmentPublishState? ToPublishState<T>(T? state) where T : struct
        => state is { } value ? Enum.Parse<EnvironmentPublishState>(value.ToString()!) : null;

    private static EnvironmentSdk.EnvironmentDefinition ToSdkDefinition(FabricItemDefinition definition)
        => new(definition.Parts.Select(part => new EnvironmentSdk.EnvironmentDefinitionPart
        {
            Path = part.Path,
            Payload = part.Payload,
            PayloadType = PayloadType.InlineBase64,
        }))
        {
            Format = definition.Format,
        };
}
