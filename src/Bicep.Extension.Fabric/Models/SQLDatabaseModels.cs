using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace Bicep.Extension.Fabric.Models;

[ResourceType("SQLDatabase")]
public class SQLDatabase : FabricItem
{
    [TypeProperty("The SQL Database creation configuration. Changing this forces recreation of the SQL Database.")]
    public SQLDatabaseConfiguration? Configuration { get; set; }

    [TypeProperty("The connection string of the database", ObjectTypePropertyFlags.ReadOnly)]
    public string? ConnectionString { get; set; }

    [TypeProperty("The database name", ObjectTypePropertyFlags.ReadOnly)]
    public string? DatabaseName { get; set; }

    [TypeProperty("The server fully qualified domain name (FQDN)", ObjectTypePropertyFlags.ReadOnly)]
    public string? ServerFqdn { get; set; }

    [TypeProperty("The earliest point in time the database can be restored to", ObjectTypePropertyFlags.ReadOnly)]
    public string? EarliestRestorePoint { get; set; }

    [TypeProperty("The latest point in time the database can be restored to", ObjectTypePropertyFlags.ReadOnly)]
    public string? LatestRestorePoint { get; set; }

    [TypeProperty("The backup retention period in days", ObjectTypePropertyFlags.ReadOnly)]
    public int? BackupRetentionDays { get; set; }

    [TypeProperty("The collation of the database", ObjectTypePropertyFlags.ReadOnly)]
    public string? Collation { get; set; }
}

public class SQLDatabaseConfiguration
{
    [TypeProperty("The creation mode of the SQL database", ObjectTypePropertyFlags.Required)]
    public SQLDatabaseCreationMode CreationMode { get; set; }

    [TypeProperty("The collation of the SQL database. Only applies when creationMode is 'New'.")]
    public string? Collation { get; set; }

    [TypeProperty("The backup retention period in days. Minimum 1, maximum 35.")]
    public int? BackupRetentionDays { get; set; }

    [TypeProperty("The point in time (UTC, YYYY-MM-DDTHH:mm:ssZ) to restore the source database from. Applies when creationMode is 'Restore' or 'RestoreDeletedDatabase'.")]
    public string? RestorePointInTime { get; set; }

    [TypeProperty("The name of the restorable deleted database to restore. Required when creationMode is 'RestoreDeletedDatabase'.")]
    public string? RestorableDeletedDatabaseName { get; set; }

    [TypeProperty("The source database to restore from. Applies when creationMode is 'Restore'.")]
    public SQLDatabaseSourceReference? SourceDatabaseReference { get; set; }
}

public class SQLDatabaseSourceReference
{
    [TypeProperty("The ID of the source item", ObjectTypePropertyFlags.Required)]
    public required string ItemId { get; set; }

    [TypeProperty("The workspace ID of the source item", ObjectTypePropertyFlags.Required)]
    public required string WorkspaceId { get; set; }
}

public enum SQLDatabaseCreationMode
{
    New,
    Restore,
    RestoreDeletedDatabase,
}
