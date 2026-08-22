using CoreModels = Microsoft.Fabric.Api.Core.Models;

namespace Bicep.Extension.Fabric.Handlers;

// The Fabric API has no update operation for shortcuts (all fields are effectively immutable), so
// CreateOrUpdate always calls CreateShortcut; the user-specified conflict policy (default Abort,
// matching Terraform) governs what happens if the shortcut already exists.
public sealed class ShortcutHandler : FabricResourceHandlerBase<Shortcut, ShortcutIdentifiers>
{
    protected override Task<ResourceResponse> Preview(ResourceRequest request, CancellationToken cancellationToken)
        => Task.FromResult(GetResponse(request));

    protected override Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            var workspaceId = ParseId(request.Properties.WorkspaceId, "workspaceId");
            var itemId = ParseId(request.Properties.ItemId, "itemId");

            var create = new CoreModels.CreateShortcutRequest(request.Properties.Path, request.Properties.Name, ToSdkTarget(request.Properties.Target));
            var conflictPolicy = request.Properties.ShortcutConflictPolicy is { } policy ? ToSdkConflictPolicy(policy) : (CoreModels.ShortcutConflictPolicy?)null;

            var result = (await client.Core.OneLakeShortcuts.CreateShortcutAsync(workspaceId, itemId, create, conflictPolicy, cancellationToken)).Value;

            return BuildResponse(request.Type, request.ApiVersion, ToProperties(result, workspaceId, itemId, request.Properties.ShortcutConflictPolicy));
        });

    protected override Task<ResourceResponse> Get(ReferenceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            var workspaceId = ParseId(request.Identifiers.WorkspaceId, "workspaceId");
            var itemId = ParseId(request.Identifiers.ItemId, "itemId");

            var result = (await client.Core.OneLakeShortcuts.GetShortcutAsync(
                workspaceId,
                itemId,
                request.Identifiers.Path,
                request.Identifiers.Name,
                cancellationToken)).Value;

            return BuildResponse(request.Type, request.ApiVersion, ToProperties(result, workspaceId, itemId, shortcutConflictPolicy: null));
        });

    protected override Task<ResourceResponse> Delete(ReferenceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            await client.Core.OneLakeShortcuts.DeleteShortcutAsync(
                ParseId(request.Identifiers.WorkspaceId, "workspaceId"),
                ParseId(request.Identifiers.ItemId, "itemId"),
                request.Identifiers.Path,
                request.Identifiers.Name,
                cancellationToken);

            return GetResponse(request, properties: null);
        });

    protected override ShortcutIdentifiers GetIdentifiers(Shortcut properties)
        => new() { WorkspaceId = properties.WorkspaceId, ItemId = properties.ItemId, Path = properties.Path, Name = properties.Name };

    private static CoreModels.CreatableShortcutTarget ToSdkTarget(ShortcutTarget target)
    {
        var count = new[] { target.OneLake is not null, target.AdlsGen2 is not null, target.AmazonS3 is not null, target.GoogleCloudStorage is not null, target.S3Compatible is not null, target.Dataverse is not null, target.AzureBlobStorage is not null, target.OneDriveSharePoint is not null }.Count(x => x);
        if (count != 1)
        {
            throw new ResourceErrorException("InvalidProperty", "Exactly one of target.oneLake, target.adlsGen2, target.amazonS3, target.googleCloudStorage, target.s3Compatible, target.dataverse, target.azureBlobStorage, or target.oneDriveSharePoint must be set.", "target");
        }

        var result = new CoreModels.CreatableShortcutTarget();

        if (target.OneLake is { } oneLake)
        {
            result.OneLake = new CoreModels.OneLake(ParseId(oneLake.ItemId, "target.oneLake.itemId"), ParseId(oneLake.WorkspaceId, "target.oneLake.workspaceId"), oneLake.Path);
        }

        if (target.AdlsGen2 is { } adlsGen2)
        {
            result.AdlsGen2 = new CoreModels.AdlsGen2(new Uri(adlsGen2.Location), adlsGen2.Subpath, ParseId(adlsGen2.ConnectionId, "target.adlsGen2.connectionId"));
        }

        if (target.AmazonS3 is { } amazonS3)
        {
            result.AmazonS3 = new CoreModels.AmazonS3(new Uri(amazonS3.Location), ParseId(amazonS3.ConnectionId, "target.amazonS3.connectionId")) { Subpath = amazonS3.Subpath };
        }

        if (target.GoogleCloudStorage is { } googleCloudStorage)
        {
            result.GoogleCloudStorage = new CoreModels.GoogleCloudStorage(new Uri(googleCloudStorage.Location), googleCloudStorage.Subpath, ParseId(googleCloudStorage.ConnectionId, "target.googleCloudStorage.connectionId"));
        }

        if (target.S3Compatible is { } s3Compatible)
        {
            result.S3Compatible = new CoreModels.S3Compatible(new Uri(s3Compatible.Location), s3Compatible.Subpath, s3Compatible.Bucket, ParseId(s3Compatible.ConnectionId, "target.s3Compatible.connectionId"));
        }

        if (target.Dataverse is { } dataverse)
        {
            result.Dataverse = new CoreModels.Dataverse(new Uri(dataverse.EnvironmentDomain), ParseId(dataverse.ConnectionId, "target.dataverse.connectionId"), dataverse.DeltalakeFolder, dataverse.TableName);
        }

        if (target.AzureBlobStorage is { } azureBlobStorage)
        {
            result.AzureBlobStorage = new CoreModels.AzureBlobStorage(new Uri(azureBlobStorage.Location), azureBlobStorage.Subpath, ParseId(azureBlobStorage.ConnectionId, "target.azureBlobStorage.connectionId"));
        }

        if (target.OneDriveSharePoint is { } oneDriveSharePoint)
        {
            result.OneDriveSharePoint = new CoreModels.OneDriveSharePoint(new Uri(oneDriveSharePoint.Location), oneDriveSharePoint.Subpath, ParseId(oneDriveSharePoint.ConnectionId, "target.oneDriveSharePoint.connectionId"))
            {
                UpdateFabricItemSensitivity = oneDriveSharePoint.UpdateFabricItemSensitivity,
            };
        }

        return result;
    }

    private static ShortcutTarget ToModelTarget(CoreModels.Target target)
        => new()
        {
            Type = target.Type.ToString(),
            OneLake = target.OneLake is { } oneLake
                ? new ShortcutOneLakeTarget { WorkspaceId = oneLake.WorkspaceId.ToString(), ItemId = oneLake.ItemId.ToString(), Path = oneLake.Path }
                : null,
            AdlsGen2 = target.AdlsGen2 is { } adlsGen2
                ? new ShortcutAdlsGen2Target { Location = adlsGen2.Location.ToString(), Subpath = adlsGen2.Subpath, ConnectionId = adlsGen2.ConnectionId.ToString() }
                : null,
            AmazonS3 = target.AmazonS3 is { } amazonS3
                ? new ShortcutAmazonS3Target { Location = amazonS3.Location.ToString(), Subpath = amazonS3.Subpath, ConnectionId = amazonS3.ConnectionId.ToString() }
                : null,
            GoogleCloudStorage = target.GoogleCloudStorage is { } googleCloudStorage
                ? new ShortcutGoogleCloudStorageTarget { Location = googleCloudStorage.Location.ToString(), Subpath = googleCloudStorage.Subpath, ConnectionId = googleCloudStorage.ConnectionId.ToString() }
                : null,
            S3Compatible = target.S3Compatible is { } s3Compatible
                ? new ShortcutS3CompatibleTarget { Location = s3Compatible.Location.ToString(), Subpath = s3Compatible.Subpath, Bucket = s3Compatible.Bucket, ConnectionId = s3Compatible.ConnectionId.ToString() }
                : null,
            Dataverse = target.Dataverse is { } dataverse
                ? new ShortcutDataverseTarget { EnvironmentDomain = dataverse.EnvironmentDomain.ToString(), TableName = dataverse.TableName, DeltalakeFolder = dataverse.DeltaLakeFolder, ConnectionId = dataverse.ConnectionId.ToString() }
                : null,
            AzureBlobStorage = target.AzureBlobStorage is { } azureBlobStorage
                ? new ShortcutAzureBlobStorageTarget { Location = azureBlobStorage.Location.ToString(), Subpath = azureBlobStorage.Subpath, ConnectionId = azureBlobStorage.ConnectionId.ToString() }
                : null,
            OneDriveSharePoint = target.OneDriveSharePoint is { } oneDriveSharePoint
                ? new ShortcutOneDriveSharePointTarget
                {
                    Location = oneDriveSharePoint.Location.ToString(),
                    Subpath = oneDriveSharePoint.Subpath,
                    ConnectionId = oneDriveSharePoint.ConnectionId.ToString(),
                    UpdateFabricItemSensitivity = oneDriveSharePoint.UpdateFabricItemSensitivity,
                }
                : null,
            ExternalDataShare = target.ExternalDataShare is { } externalDataShare
                ? new ShortcutExternalDataShareTarget { ConnectionId = externalDataShare.ConnectionId.ToString() }
                : null,
        };

    private static CoreModels.ShortcutConflictPolicy ToSdkConflictPolicy(ShortcutConflictPolicy policy)
        => policy switch
        {
            ShortcutConflictPolicy.Abort => CoreModels.ShortcutConflictPolicy.Abort,
            ShortcutConflictPolicy.CreateOrOverwrite => CoreModels.ShortcutConflictPolicy.CreateOrOverwrite,
            ShortcutConflictPolicy.OverwriteOnly => CoreModels.ShortcutConflictPolicy.OverwriteOnly,
            ShortcutConflictPolicy.GenerateUniqueName => CoreModels.ShortcutConflictPolicy.GenerateUniqueName,
            _ => throw new ResourceErrorException("InvalidProperty", $"Unsupported shortcut conflict policy '{policy}'.", "shortcutConflictPolicy"),
        };

    private static Shortcut ToProperties(CoreModels.Shortcut shortcut, Guid workspaceId, Guid itemId, ShortcutConflictPolicy? shortcutConflictPolicy)
        => new()
        {
            WorkspaceId = workspaceId.ToString(),
            ItemId = itemId.ToString(),
            Path = shortcut.Path,
            Name = shortcut.Name,
            ShortcutConflictPolicy = shortcutConflictPolicy,
            Target = ToModelTarget(shortcut.Target),
        };

    private static ResourceResponse BuildResponse(string type, string? apiVersion, Shortcut properties)
        => new()
        {
            Type = type,
            ApiVersion = apiVersion,
            Properties = properties,
            Identifiers = new ShortcutIdentifiers { WorkspaceId = properties.WorkspaceId, ItemId = properties.ItemId, Path = properties.Path, Name = properties.Name },
        };

    private static Guid ParseId(string value, string target)
        => Guid.TryParse(value, out var result)
            ? result
            : throw new ResourceErrorException("InvalidIdentifier", $"'{value}' is not a valid GUID.", target);
}
