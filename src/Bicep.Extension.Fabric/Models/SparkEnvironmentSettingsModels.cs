using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace Bicep.Extension.Fabric.Models;

public enum SparkEnvironmentPublicationStatus
{
    Published,
    Staging,
}

public enum SparkEnvironmentPoolType
{
    Capacity,
    Workspace,
}

public class SparkEnvironmentDynamicExecutorAllocation
{
    [TypeProperty("The status of the dynamic executor allocation")]
    public bool? Enabled { get; set; }

    [TypeProperty("The minimum executors. Must be set together with maxExecutors")]
    public int? MinExecutors { get; set; }

    [TypeProperty("The maximum executors. Must be set together with minExecutors")]
    public int? MaxExecutors { get; set; }
}

public class SparkEnvironmentPool
{
    [TypeProperty("The pool ID", ObjectTypePropertyFlags.ReadOnly)]
    public string? Id { get; set; }

    [TypeProperty("The pool name. 'Starter Pool' means use the starting pool. Must be set together with type")]
    public string? Name { get; set; }

    [TypeProperty("The pool type. Must be set together with name")]
    public SparkEnvironmentPoolType? Type { get; set; }
}

public class SparkEnvironmentSettingsIdentifiers
{
    [TypeProperty("The Workspace ID", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public string WorkspaceId { get; set; } = string.Empty;

    [TypeProperty("The Environment ID", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public string EnvironmentId { get; set; } = string.Empty;

    // Published and Staging are distinct SDK-addressable targets (client.Environment.Published /
    // .Staging), so the publication status must be part of the resource's identity, not just a
    // regular property.
    [TypeProperty("The publication status", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public SparkEnvironmentPublicationStatus PublicationStatus { get; set; }
}

[ResourceType("SparkEnvironmentSettings")]
public class SparkEnvironmentSettings : SparkEnvironmentSettingsIdentifiers
{
    [TypeProperty("Spark driver cores. Must be one of: 4, 8, 16, 32, 64")]
    public int? DriverCores { get; set; }

    [TypeProperty("Spark driver memory. Must be one of: 28g, 56g, 112g, 224g, 400g")]
    public string? DriverMemory { get; set; }

    [TypeProperty("Spark executor cores. Must be one of: 4, 8, 16, 32, 64")]
    public int? ExecutorCores { get; set; }

    [TypeProperty("Spark executor memory. Must be one of: 28g, 56g, 112g, 224g, 400g")]
    public string? ExecutorMemory { get; set; }

    [TypeProperty("The runtime version")]
    public string? RuntimeVersion { get; set; }

    [TypeProperty("Dynamic Executor Allocation properties")]
    public SparkEnvironmentDynamicExecutorAllocation? DynamicExecutorAllocation { get; set; }

    [TypeProperty("The environment pool")]
    public SparkEnvironmentPool? Pool { get; set; }

    [TypeProperty("A map of key/value pairs of Spark properties. Keys must start with 'spark.' and contain no white spaces")]
    public Dictionary<string, string>? SparkProperties { get; set; }
}
