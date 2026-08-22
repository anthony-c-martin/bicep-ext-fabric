using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace Bicep.Extension.Fabric.Models;

[ResourceType("Environment")]
public class Environment : FabricItem
{
    [TypeProperty("Details of the environment publish operation", ObjectTypePropertyFlags.ReadOnly)]
    public EnvironmentPublishDetails? PublishDetails { get; set; }
}

public class EnvironmentPublishDetails
{
    [TypeProperty("The publish state", ObjectTypePropertyFlags.ReadOnly)]
    public EnvironmentPublishState? State { get; set; }

    [TypeProperty("The target version to be published", ObjectTypePropertyFlags.ReadOnly)]
    public string? TargetVersion { get; set; }

    [TypeProperty("The start time of the publish operation", ObjectTypePropertyFlags.ReadOnly)]
    public string? StartTime { get; set; }

    [TypeProperty("The end time of the publish operation", ObjectTypePropertyFlags.ReadOnly)]
    public string? EndTime { get; set; }

    [TypeProperty("Component publish information", ObjectTypePropertyFlags.ReadOnly)]
    public EnvironmentComponentPublishInfo? ComponentPublishInfo { get; set; }
}

public class EnvironmentComponentPublishInfo
{
    [TypeProperty("Spark libraries publish information", ObjectTypePropertyFlags.ReadOnly)]
    public EnvironmentComponentPublishState? SparkLibraries { get; set; }

    [TypeProperty("Spark settings publish information", ObjectTypePropertyFlags.ReadOnly)]
    public EnvironmentComponentPublishState? SparkSettings { get; set; }
}

public class EnvironmentComponentPublishState
{
    [TypeProperty("The publish state", ObjectTypePropertyFlags.ReadOnly)]
    public EnvironmentPublishState? State { get; set; }
}

public enum EnvironmentPublishState
{
    Waiting,
    Running,
    Cancelling,
    Cancelled,
    Failed,
    Success,
}
