using Microsoft.Fabric.Api;
using Microsoft.Fabric.Api.Core.Models;
using VariableLibrarySdk = Microsoft.Fabric.Api.VariableLibrary.Models;

namespace Bicep.Extension.Fabric.Handlers;

public sealed class VariableLibraryHandler : FabricTypedItemHandlerBase<VariableLibrary, VariableLibrarySdk.VariableLibrary>
{
    protected override ItemType FabricItemType => ItemType.VariableLibrary;

    protected override async Task<VariableLibrarySdk.VariableLibrary> CreateItemAsync(FabricClient client, Guid workspaceId, VariableLibrary properties, CancellationToken cancellationToken)
    {
        var create = new VariableLibrarySdk.CreateVariableLibraryRequest(properties.DisplayName)
        {
            Description = properties.Description,
            FolderId = ParseOptionalGuid(properties.FolderId, "folderId"),
            Definition = properties.Definition is { } definition ? ToSdkDefinition(definition) : null,
        };

        var result = (await client.VariableLibrary.Items.CreateVariableLibraryAsync(workspaceId, create, cancellationToken)).Value;

        // The active value set can only be selected once the library exists.
        if (properties.ActiveValueSetName is not null && result.Id is { } itemId)
        {
            result = await UpdateItemAsync(client, workspaceId, itemId, properties, cancellationToken);
        }

        return result;
    }

    protected override async Task<VariableLibrarySdk.VariableLibrary> UpdateItemAsync(FabricClient client, Guid workspaceId, Guid itemId, VariableLibrary properties, CancellationToken cancellationToken)
    {
        var update = new VariableLibrarySdk.UpdateVariableLibraryRequest
        {
            DisplayName = properties.DisplayName,
            Description = properties.Description,
            Properties = properties.ActiveValueSetName is { } activeValueSetName
                ? new VariableLibrarySdk.VariableLibraryProperties(activeValueSetName)
                : null,
        };

        return (await client.VariableLibrary.Items.UpdateVariableLibraryAsync(workspaceId, itemId, update, cancellationToken)).Value;
    }

    protected override async Task<VariableLibrarySdk.VariableLibrary> GetItemAsync(FabricClient client, Guid workspaceId, Guid itemId, CancellationToken cancellationToken)
        => (await client.VariableLibrary.Items.GetVariableLibraryAsync(workspaceId, itemId, cancellationToken)).Value;

    protected override Task DeleteItemAsync(FabricClient client, Guid workspaceId, Guid itemId, CancellationToken cancellationToken)
        => client.VariableLibrary.Items.DeleteVariableLibraryAsync(workspaceId, itemId, cancellationToken);

    protected override Task UpdateDefinitionAsync(FabricClient client, Guid workspaceId, Guid itemId, FabricItemDefinition definition, CancellationToken cancellationToken)
        => client.VariableLibrary.Items.UpdateVariableLibraryDefinitionAsync(
            workspaceId,
            itemId,
            new VariableLibrarySdk.UpdateVariableLibraryDefinitionRequest(ToSdkDefinition(definition)),
            cancellationToken: cancellationToken);

    protected override VariableLibrary ToProperties(VariableLibrarySdk.VariableLibrary item, Guid workspaceId)
        => new()
        {
            WorkspaceId = (item.WorkspaceId ?? workspaceId).ToString(),
            Id = item.Id?.ToString(),
            DisplayName = item.DisplayName,
            Description = item.Description,
            FolderId = item.FolderId?.ToString(),
            Type = item.Type.ToString(),
            ActiveValueSetName = item.Properties?.ActiveValueSetName,
        };

    private static VariableLibrarySdk.VariableLibraryPublicDefinition ToSdkDefinition(FabricItemDefinition definition)
        => new(definition.Parts.Select(part => new VariableLibrarySdk.VariableLibraryPublicDefinitionPart(
            part.Path,
            part.Payload,
            PayloadType.InlineBase64)))
        {
            Format = definition.Format,
        };
}
