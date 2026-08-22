using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace Bicep.Extension.Fabric.Models;

public enum SparkCustomPoolNodeFamily
{
    MemoryOptimized,
}

public enum SparkCustomPoolNodeSize
{
    Small,
    Medium,
    Large,
    XLarge,
    XXLarge,
}

// The Fabric API creates custom pools scoped to a workspace only; a capacity-scoped pool type
// is not exposed through this creation endpoint, matching the Terraform provider which only
// supports the `Workspace` value for this field.
public enum SparkCustomPoolResourceType
{
    Workspace,
}

public class SparkCustomPoolAutoScale
{
    [TypeProperty("The status of the auto scale", ObjectTypePropertyFlags.Required)]
    public required bool Enabled { get; set; }

    [TypeProperty("The minimum node count", ObjectTypePropertyFlags.Required)]
    public required int MinNodeCount { get; set; }

    [TypeProperty("The maximum node count", ObjectTypePropertyFlags.Required)]
    public required int MaxNodeCount { get; set; }
}

public class SparkCustomPoolDynamicExecutorAllocation
{
    [TypeProperty("The status of the dynamic executor allocation", ObjectTypePropertyFlags.Required)]
    public required bool Enabled { get; set; }

    [TypeProperty("The minimum executors. Required when enabled is true")]
    public int? MinExecutors { get; set; }

    [TypeProperty("The maximum executors. Required when enabled is true")]
    public int? MaxExecutors { get; set; }
}

public class SparkCustomPoolIdentifiers
{
    [TypeProperty("The Workspace ID", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public string WorkspaceId { get; set; } = string.Empty;

    [TypeProperty("The Spark Custom Pool ID", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.ReadOnly)]
    public string? Id { get; set; }
}

[ResourceType("SparkCustomPool")]
public class SparkCustomPool : SparkCustomPoolIdentifiers
{
    [TypeProperty("The Spark Custom Pool name", ObjectTypePropertyFlags.Required)]
    public required string Name { get; set; }

    [TypeProperty("The Spark Custom Pool type", ObjectTypePropertyFlags.Required)]
    public required SparkCustomPoolResourceType Type { get; set; }

    [TypeProperty("The node family", ObjectTypePropertyFlags.Required)]
    public required SparkCustomPoolNodeFamily NodeFamily { get; set; }

    [TypeProperty("The node size", ObjectTypePropertyFlags.Required)]
    public required SparkCustomPoolNodeSize NodeSize { get; set; }

    [TypeProperty("Auto-scale properties", ObjectTypePropertyFlags.Required)]
    public required SparkCustomPoolAutoScale AutoScale { get; set; }

    [TypeProperty("Dynamic executor allocation properties", ObjectTypePropertyFlags.Required)]
    public required SparkCustomPoolDynamicExecutorAllocation DynamicExecutorAllocation { get; set; }
}
