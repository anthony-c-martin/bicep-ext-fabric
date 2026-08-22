using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace Bicep.Extension.Fabric.Models;

public enum KQLDatabaseType
{
    ReadWrite,
    Shortcut,
}

[ResourceType("KQLDatabase")]
public class KQLDatabase : FabricItem
{
    [TypeProperty("The KQL Database creation configuration. Changing this forces recreation of the KQL Database. Required unless the KQL Database is created from a definition.")]
    public KQLDatabaseConfiguration? Configuration { get; set; }

    [TypeProperty("The type of the KQL Database", ObjectTypePropertyFlags.ReadOnly)]
    public KQLDatabaseType? DatabaseType { get; set; }

    [TypeProperty("The query service URI", ObjectTypePropertyFlags.ReadOnly)]
    public string? QueryServiceUri { get; set; }

    [TypeProperty("The ingestion service URI", ObjectTypePropertyFlags.ReadOnly)]
    public string? IngestionServiceUri { get; set; }
}

public class KQLDatabaseConfiguration
{
    [TypeProperty("The Eventhouse that contains the KQL Database", ObjectTypePropertyFlags.Required)]
    public required string EventhouseId { get; set; }

    [TypeProperty("The type of KQL Database to create. Defaults to ReadWrite.")]
    public KQLDatabaseType? DatabaseType { get; set; }

    [TypeProperty("The URI of the source Kusto cluster. Required when 'databaseType' is 'Shortcut' and 'invitationToken' is not set.")]
    public string? SourceClusterUri { get; set; }

    [TypeProperty("The name of the database in the source Kusto cluster. Required when 'databaseType' is 'Shortcut' and 'invitationToken' is not set.")]
    public string? SourceDatabaseName { get; set; }

    [TypeProperty("An invitation token for a data-share based shortcut database. Cannot be combined with 'sourceClusterUri' or 'sourceDatabaseName'.")]
    public string? InvitationToken { get; set; }
}
