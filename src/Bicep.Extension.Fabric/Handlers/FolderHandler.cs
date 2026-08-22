using Microsoft.Fabric.Api.Core.Models;

namespace Bicep.Extension.Fabric.Handlers;

public sealed class FolderHandler : FabricResourceHandlerBase<FabricFolder, FolderIdentifiers>
{
    protected override Task<ResourceResponse> Preview(ResourceRequest request, CancellationToken cancellationToken)
        => Task.FromResult(GetResponse(request));

    protected override Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            var workspaceId = ParseId(request.Properties.WorkspaceId, "workspaceId");
            Microsoft.Fabric.Api.Core.Models.Folder result;

            if (request.Properties.Id is { } folderIdValue)
            {
                var folderId = ParseId(folderIdValue, "id");

                result = (await client.Core.Folders.UpdateFolderAsync(
                    workspaceId,
                    folderId,
                    new UpdateFolderRequest(request.Properties.DisplayName),
                    cancellationToken)).Value;

                if (request.Properties.ParentFolderId is { } parentFolderId)
                {
                    result = (await client.Core.Folders.MoveFolderAsync(
                        workspaceId,
                        folderId,
                        new MoveFolderRequest { TargetFolderId = ParseId(parentFolderId, "parentFolderId") },
                        cancellationToken)).Value;
                }
            }
            else
            {
                var create = new CreateFolderRequest(request.Properties.DisplayName)
                {
                    ParentFolderId = ParseOptionalId(request.Properties.ParentFolderId, "parentFolderId"),
                };

                result = (await client.Core.Folders.CreateFolderAsync(workspaceId, create, cancellationToken)).Value;
            }

            return BuildResponse(request.Type, request.ApiVersion, ToProperties(result, workspaceId));
        });

    protected override Task<ResourceResponse> Get(ReferenceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            var workspaceId = ParseId(request.Identifiers.WorkspaceId, "workspaceId");
            var result = (await client.Core.Folders.GetFolderAsync(
                workspaceId,
                RequireId(request.Identifiers.Id),
                cancellationToken)).Value;

            return BuildResponse(request.Type, request.ApiVersion, ToProperties(result, workspaceId));
        });

    protected override Task<ResourceResponse> Delete(ReferenceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            await client.Core.Folders.DeleteFolderAsync(
                ParseId(request.Identifiers.WorkspaceId, "workspaceId"),
                RequireId(request.Identifiers.Id),
                cancellationToken);

            return GetResponse(request, properties: null);
        });

    protected override FolderIdentifiers GetIdentifiers(FabricFolder properties)
        => new()
        {
            WorkspaceId = properties.WorkspaceId,
            Id = properties.Id,
        };

    private static FabricFolder ToProperties(Microsoft.Fabric.Api.Core.Models.Folder folder, Guid workspaceId)
        => new()
        {
            WorkspaceId = (folder.WorkspaceId ?? workspaceId).ToString(),
            Id = folder.Id?.ToString(),
            DisplayName = folder.DisplayName,
            ParentFolderId = folder.ParentFolderId?.ToString(),
        };

    private static ResourceResponse BuildResponse(string type, string? apiVersion, FabricFolder properties)
        => new()
        {
            Type = type,
            ApiVersion = apiVersion,
            Properties = properties,
            Identifiers = new FolderIdentifiers { WorkspaceId = properties.WorkspaceId, Id = properties.Id },
        };

    private static Guid RequireId(string? id)
        => id is null
            ? throw new ResourceErrorException("MissingIdentifier", "The Folder ID is required for this operation.")
            : ParseId(id, "id");

    private static Guid ParseId(string value, string target)
        => Guid.TryParse(value, out var result)
            ? result
            : throw new ResourceErrorException("InvalidIdentifier", $"'{value}' is not a valid GUID.", target);

    private static Guid? ParseOptionalId(string? value, string target)
        => value is null ? null : ParseId(value, target);
}
