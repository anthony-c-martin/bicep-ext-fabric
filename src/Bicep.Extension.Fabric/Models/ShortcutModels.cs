using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace Bicep.Extension.Fabric.Models;

public enum ShortcutConflictPolicy
{
    Abort,
    CreateOrOverwrite,
    OverwriteOnly,
    GenerateUniqueName,
}

public class ShortcutOneLakeTarget
{
    [TypeProperty("The ID of the target workspace", ObjectTypePropertyFlags.Required)]
    public required string WorkspaceId { get; set; }

    [TypeProperty("The ID of the target item in OneLake. The target can be an item of Lakehouse, KQLDatabase, or Warehouse", ObjectTypePropertyFlags.Required)]
    public required string ItemId { get; set; }

    [TypeProperty("The full path to the target folder within the item, relative to the root of the OneLake directory structure", ObjectTypePropertyFlags.Required)]
    public required string Path { get; set; }
}

public class ShortcutAdlsGen2Target
{
    [TypeProperty("The location of the target ADLS container, in the format https://[account-name].dfs.core.windows.net", ObjectTypePropertyFlags.Required)]
    public required string Location { get; set; }

    [TypeProperty("The container and subfolder within the ADLS account where the target folder is located, in the format [container]/[subfolder]", ObjectTypePropertyFlags.Required)]
    public required string Subpath { get; set; }

    [TypeProperty("The ID of the connection bound with the shortcut", ObjectTypePropertyFlags.Required)]
    public required string ConnectionId { get; set; }
}

public class ShortcutAmazonS3Target
{
    [TypeProperty("The HTTP URL that points to the target bucket in S3, in the format https://[bucket-name].s3.[region-code].amazonaws.com", ObjectTypePropertyFlags.Required)]
    public required string Location { get; set; }

    [TypeProperty("The target folder or subfolder within the S3 bucket", ObjectTypePropertyFlags.Required)]
    public required string Subpath { get; set; }

    [TypeProperty("The ID of the connection bound with the shortcut", ObjectTypePropertyFlags.Required)]
    public required string ConnectionId { get; set; }
}

public class ShortcutGoogleCloudStorageTarget
{
    [TypeProperty("The HTTP URL that points to the target bucket in GCS, in the format https://[bucket-name].storage.googleapis.com", ObjectTypePropertyFlags.Required)]
    public required string Location { get; set; }

    [TypeProperty("The target folder or subfolder within the GCS bucket", ObjectTypePropertyFlags.Required)]
    public required string Subpath { get; set; }

    [TypeProperty("The ID of the connection bound with the shortcut", ObjectTypePropertyFlags.Required)]
    public required string ConnectionId { get; set; }
}

public class ShortcutS3CompatibleTarget
{
    [TypeProperty("The HTTP URL of the S3 compatible endpoint, without a bucket specified", ObjectTypePropertyFlags.Required)]
    public required string Location { get; set; }

    [TypeProperty("The target folder or subfolder within the S3 compatible bucket", ObjectTypePropertyFlags.Required)]
    public required string Subpath { get; set; }

    [TypeProperty("The target bucket within the S3 compatible location", ObjectTypePropertyFlags.Required)]
    public required string Bucket { get; set; }

    [TypeProperty("The ID of the connection bound with the shortcut", ObjectTypePropertyFlags.Required)]
    public required string ConnectionId { get; set; }
}

public class ShortcutDataverseTarget
{
    [TypeProperty("The URI of the Dataverse target environment's domain, in the format https://[orgname].crm[xx].dynamics.com", ObjectTypePropertyFlags.Required)]
    public required string EnvironmentDomain { get; set; }

    [TypeProperty("The name of the target table in Dataverse", ObjectTypePropertyFlags.Required)]
    public required string TableName { get; set; }

    [TypeProperty("The DeltaLake folder path where the target data is stored", ObjectTypePropertyFlags.Required)]
    public required string DeltalakeFolder { get; set; }

    [TypeProperty("The ID of the connection bound with the shortcut", ObjectTypePropertyFlags.Required)]
    public required string ConnectionId { get; set; }
}

public class ShortcutAzureBlobStorageTarget
{
    [TypeProperty("The location of the target Azure Blob Storage container, in the format https://[account-name].blob.core.windows.net", ObjectTypePropertyFlags.Required)]
    public required string Location { get; set; }

    [TypeProperty("The container and subfolder within the Azure Blob Storage account where the target folder is located, in the format [container]/[subfolder]", ObjectTypePropertyFlags.Required)]
    public required string Subpath { get; set; }

    [TypeProperty("The ID of the connection bound with the shortcut", ObjectTypePropertyFlags.Required)]
    public required string ConnectionId { get; set; }
}

public class ShortcutOneDriveSharePointTarget
{
    [TypeProperty("The location of the target OneDrive SharePoint container, in the format https://microsoft.sharepoint.com", ObjectTypePropertyFlags.Required)]
    public required string Location { get; set; }

    [TypeProperty("The container and subfolder within the OneDrive SharePoint account where the target folder is located, in the format [container]/[subfolder]", ObjectTypePropertyFlags.Required)]
    public required string Subpath { get; set; }

    [TypeProperty("The ID of the connection bound with the shortcut", ObjectTypePropertyFlags.Required)]
    public required string ConnectionId { get; set; }

    [TypeProperty("Whether the Fabric item sensitivity label should be kept consistent with the SharePoint site level label. Defaults to false")]
    public bool? UpdateFabricItemSensitivity { get; set; }
}

public class ShortcutExternalDataShareTarget
{
    [TypeProperty("The ID of the connection bound with the shortcut", ObjectTypePropertyFlags.ReadOnly)]
    public string? ConnectionId { get; set; }
}

public class ShortcutTarget
{
    [TypeProperty("An object containing the properties of the target OneLake data source")]
    public ShortcutOneLakeTarget? OneLake { get; set; }

    [TypeProperty("An object containing the properties of the target ADLS Gen2 data source")]
    public ShortcutAdlsGen2Target? AdlsGen2 { get; set; }

    [TypeProperty("An object containing the properties of the target Amazon S3 data source")]
    public ShortcutAmazonS3Target? AmazonS3 { get; set; }

    [TypeProperty("An object containing the properties of the target Google Cloud Storage data source")]
    public ShortcutGoogleCloudStorageTarget? GoogleCloudStorage { get; set; }

    [TypeProperty("An object containing the properties of the target S3 compatible data source")]
    public ShortcutS3CompatibleTarget? S3Compatible { get; set; }

    [TypeProperty("An object containing the properties of the target Dataverse data source")]
    public ShortcutDataverseTarget? Dataverse { get; set; }

    [TypeProperty("An object containing the properties of the target Azure Blob Storage data source")]
    public ShortcutAzureBlobStorageTarget? AzureBlobStorage { get; set; }

    [TypeProperty("An object containing the properties of the target OneDrive for Business & SharePoint Online data source")]
    public ShortcutOneDriveSharePointTarget? OneDriveSharePoint { get; set; }

    [TypeProperty("An object containing the properties of the target external data share", ObjectTypePropertyFlags.ReadOnly)]
    public ShortcutExternalDataShareTarget? ExternalDataShare { get; set; }

    [TypeProperty("The type of the target shortcut account", ObjectTypePropertyFlags.ReadOnly)]
    public string? Type { get; set; }
}

public class ShortcutIdentifiers
{
    [TypeProperty("The Workspace ID", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public string WorkspaceId { get; set; } = string.Empty;

    [TypeProperty("The item ID", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public string ItemId { get; set; } = string.Empty;

    [TypeProperty("The full path where the shortcut is created, including either 'Files' or 'Tables'", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public string Path { get; set; } = string.Empty;

    [TypeProperty("The name of the shortcut", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public string Name { get; set; } = string.Empty;
}

[ResourceType("Shortcut")]
public class Shortcut : ShortcutIdentifiers
{
    [TypeProperty("The action to take when a shortcut with the same name and path already exists. Defaults to Abort")]
    public ShortcutConflictPolicy? ShortcutConflictPolicy { get; set; }

    [TypeProperty("The target datasource. Must specify exactly one of the supported destinations", ObjectTypePropertyFlags.Required)]
    public required ShortcutTarget Target { get; set; }
}
