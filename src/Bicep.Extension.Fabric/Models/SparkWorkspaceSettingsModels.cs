using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace Bicep.Extension.Fabric.Models;

public enum SparkCustomPoolType
{
    Workspace,
    Capacity,
}

public class SparkAutomaticLogProperties
{
    [TypeProperty("Whether automatic log is enabled")]
    public bool? Enabled { get; set; }
}

public class SparkEnvironmentProperties
{
    [TypeProperty("The name of the default environment")]
    public string? Name { get; set; }

    [TypeProperty("The default runtime version")]
    public string? RuntimeVersion { get; set; }
}

public class SparkHighConcurrencyProperties
{
    [TypeProperty("Whether high concurrency for notebook interactive runs is enabled")]
    public bool? NotebookInteractiveRunEnabled { get; set; }

    [TypeProperty("Whether high concurrency for notebook pipeline runs is enabled")]
    public bool? NotebookPipelineRunEnabled { get; set; }
}

public class SparkJobProperties
{
    [TypeProperty("Whether the maximum number of cores needed for active Spark jobs is reserved")]
    public bool? ConservativeJobAdmissionEnabled { get; set; }

    [TypeProperty("The time, in minutes, to terminate inactive Spark sessions. The maximum is 20160 (14 days)")]
    public int? SessionTimeoutInMinutes { get; set; }
}

public class SparkInstancePool
{
    [TypeProperty("The pool ID. '00000000-0000-0000-0000-000000000000' means use the starter pool")]
    public string? Id { get; set; }

    [TypeProperty("The pool name. 'Starter Pool' means use the starter pool")]
    public string? Name { get; set; }

    [TypeProperty("The pool type")]
    public SparkCustomPoolType? Type { get; set; }
}

public class SparkStarterPoolProperties
{
    [TypeProperty("The maximum node count")]
    public int? MaxNodeCount { get; set; }

    [TypeProperty("The maximum executor count")]
    public int? MaxExecutors { get; set; }
}

public class SparkPoolProperties
{
    [TypeProperty("Whether compute configurations can be customized for items")]
    public bool? CustomizeComputeEnabled { get; set; }

    [TypeProperty("The default pool for the workspace")]
    public SparkInstancePool? DefaultPool { get; set; }

    [TypeProperty("The starter pool configuration for the workspace")]
    public SparkStarterPoolProperties? StarterPool { get; set; }
}

public class SparkWorkspaceSettingsIdentifiers
{
    [TypeProperty("The containing Fabric workspace ID", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public string WorkspaceId { get; set; } = string.Empty;
}

[ResourceType("SparkWorkspaceSettings")]
public class SparkWorkspaceSettings : SparkWorkspaceSettingsIdentifiers
{
    [TypeProperty("Automatic log properties")]
    public SparkAutomaticLogProperties? AutomaticLog { get; set; }

    [TypeProperty("Environment properties")]
    public SparkEnvironmentProperties? Environment { get; set; }

    [TypeProperty("High concurrency properties")]
    public SparkHighConcurrencyProperties? HighConcurrency { get; set; }

    [TypeProperty("Job properties")]
    public SparkJobProperties? Job { get; set; }

    [TypeProperty("Pool properties")]
    public SparkPoolProperties? Pool { get; set; }
}
