using SparkModels = Microsoft.Fabric.Api.Spark.Models;

namespace Bicep.Extension.Fabric.Handlers;

public sealed class SparkCustomPoolHandler : FabricResourceHandlerBase<SparkCustomPool, SparkCustomPoolIdentifiers>
{
    protected override Task<ResourceResponse> Preview(ResourceRequest request, CancellationToken cancellationToken)
        => Task.FromResult(GetResponse(request));

    protected override Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            var workspaceId = ParseId(request.Properties.WorkspaceId, "workspaceId");
            SparkModels.CustomPool result;

            if (request.Properties.Id is { } poolIdValue)
            {
                var poolId = ParseId(poolIdValue, "id");
                var update = new SparkModels.UpdateCustomPoolRequest
                {
                    Name = request.Properties.Name,
                    NodeFamily = ToSdkNodeFamily(request.Properties.NodeFamily),
                    NodeSize = ToSdkNodeSize(request.Properties.NodeSize),
                    AutoScale = ToSdkAutoScale(request.Properties.AutoScale),
                    DynamicExecutorAllocation = ToSdkDynamicExecutorAllocation(request.Properties.DynamicExecutorAllocation),
                };

                result = (await client.Spark.CustomPools.UpdateWorkspaceCustomPoolAsync(workspaceId, poolId, update, cancellationToken)).Value;
            }
            else
            {
                var create = new SparkModels.CreateCustomPoolRequest(
                    request.Properties.Name,
                    ToSdkNodeFamily(request.Properties.NodeFamily),
                    ToSdkNodeSize(request.Properties.NodeSize),
                    ToSdkAutoScale(request.Properties.AutoScale),
                    ToSdkDynamicExecutorAllocation(request.Properties.DynamicExecutorAllocation));

                result = (await client.Spark.CustomPools.CreateWorkspaceCustomPoolAsync(workspaceId, create, cancellationToken)).Value;
            }

            return BuildResponse(request.Type, request.ApiVersion, ToProperties(result, workspaceId));
        });

    protected override Task<ResourceResponse> Get(ReferenceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            var workspaceId = ParseId(request.Identifiers.WorkspaceId, "workspaceId");
            var result = (await client.Spark.CustomPools.GetWorkspaceCustomPoolAsync(workspaceId, RequireId(request.Identifiers.Id), cancellationToken)).Value;
            return BuildResponse(request.Type, request.ApiVersion, ToProperties(result, workspaceId));
        });

    protected override Task<ResourceResponse> Delete(ReferenceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            await client.Spark.CustomPools.DeleteWorkspaceCustomPoolAsync(
                ParseId(request.Identifiers.WorkspaceId, "workspaceId"),
                RequireId(request.Identifiers.Id),
                cancellationToken);

            return GetResponse(request, properties: null);
        });

    protected override SparkCustomPoolIdentifiers GetIdentifiers(SparkCustomPool properties)
        => new() { WorkspaceId = properties.WorkspaceId, Id = properties.Id };

    private static SparkModels.AutoScaleProperties ToSdkAutoScale(SparkCustomPoolAutoScale autoScale)
        => new(autoScale.Enabled, autoScale.MinNodeCount, autoScale.MaxNodeCount);

    private static SparkModels.DynamicExecutorAllocationProperties ToSdkDynamicExecutorAllocation(SparkCustomPoolDynamicExecutorAllocation allocation)
        => new(
            allocation.Enabled,
            allocation.MinExecutors ?? throw new ResourceErrorException("MissingProperty", "dynamicExecutorAllocation.minExecutors is required when dynamicExecutorAllocation.enabled is true.", "dynamicExecutorAllocation.minExecutors"),
            allocation.MaxExecutors ?? throw new ResourceErrorException("MissingProperty", "dynamicExecutorAllocation.maxExecutors is required when dynamicExecutorAllocation.enabled is true.", "dynamicExecutorAllocation.maxExecutors"));

    private static SparkModels.NodeFamily ToSdkNodeFamily(SparkCustomPoolNodeFamily value)
        => value switch
        {
            SparkCustomPoolNodeFamily.MemoryOptimized => SparkModels.NodeFamily.MemoryOptimized,
            _ => throw new ResourceErrorException("InvalidProperty", $"Unsupported node family '{value}'.", "nodeFamily"),
        };

    private static SparkCustomPoolNodeFamily ToModelNodeFamily(SparkModels.NodeFamily? value)
        => value?.ToString() switch
        {
            nameof(SparkCustomPoolNodeFamily.MemoryOptimized) => SparkCustomPoolNodeFamily.MemoryOptimized,
            var other => throw new ResourceErrorException("InvalidResponse", $"Unsupported node family '{other}' returned by the Fabric API."),
        };

    private static SparkModels.NodeSize ToSdkNodeSize(SparkCustomPoolNodeSize value)
        => value switch
        {
            SparkCustomPoolNodeSize.Small => SparkModels.NodeSize.Small,
            SparkCustomPoolNodeSize.Medium => SparkModels.NodeSize.Medium,
            SparkCustomPoolNodeSize.Large => SparkModels.NodeSize.Large,
            SparkCustomPoolNodeSize.XLarge => SparkModels.NodeSize.XLarge,
            SparkCustomPoolNodeSize.XXLarge => SparkModels.NodeSize.XXLarge,
            _ => throw new ResourceErrorException("InvalidProperty", $"Unsupported node size '{value}'.", "nodeSize"),
        };

    private static SparkCustomPoolNodeSize ToModelNodeSize(SparkModels.NodeSize? value)
        => value?.ToString() switch
        {
            nameof(SparkCustomPoolNodeSize.Small) => SparkCustomPoolNodeSize.Small,
            nameof(SparkCustomPoolNodeSize.Medium) => SparkCustomPoolNodeSize.Medium,
            nameof(SparkCustomPoolNodeSize.Large) => SparkCustomPoolNodeSize.Large,
            nameof(SparkCustomPoolNodeSize.XLarge) => SparkCustomPoolNodeSize.XLarge,
            nameof(SparkCustomPoolNodeSize.XXLarge) => SparkCustomPoolNodeSize.XXLarge,
            var other => throw new ResourceErrorException("InvalidResponse", $"Unsupported node size '{other}' returned by the Fabric API."),
        };

    private static SparkCustomPoolResourceType ToModelResourceType(SparkModels.CustomPoolType? value)
        => value?.ToString() switch
        {
            nameof(SparkCustomPoolResourceType.Workspace) => SparkCustomPoolResourceType.Workspace,
            var other => throw new ResourceErrorException("InvalidResponse", $"Unsupported custom pool type '{other}' returned by the Fabric API. Only workspace-scoped custom pools are supported by this resource."),
        };

    private static SparkCustomPool ToProperties(SparkModels.CustomPool pool, Guid workspaceId)
        => new()
        {
            WorkspaceId = workspaceId.ToString(),
            Id = pool.Id?.ToString(),
            Name = pool.Name,
            Type = ToModelResourceType(pool.Type),
            NodeFamily = ToModelNodeFamily(pool.NodeFamily),
            NodeSize = ToModelNodeSize(pool.NodeSize),
            AutoScale = pool.AutoScale is { } autoScale
                ? new SparkCustomPoolAutoScale { Enabled = autoScale.Enabled, MinNodeCount = autoScale.MinNodeCount, MaxNodeCount = autoScale.MaxNodeCount }
                : throw new ResourceErrorException("InvalidResponse", "The Fabric API did not return auto-scale properties for this custom pool."),
            DynamicExecutorAllocation = pool.DynamicExecutorAllocation is { } allocation
                ? new SparkCustomPoolDynamicExecutorAllocation { Enabled = allocation.Enabled, MinExecutors = allocation.MinExecutors, MaxExecutors = allocation.MaxExecutors }
                : throw new ResourceErrorException("InvalidResponse", "The Fabric API did not return dynamic executor allocation properties for this custom pool."),
        };

    private static ResourceResponse BuildResponse(string type, string? apiVersion, SparkCustomPool properties)
        => new()
        {
            Type = type,
            ApiVersion = apiVersion,
            Properties = properties,
            Identifiers = new SparkCustomPoolIdentifiers { WorkspaceId = properties.WorkspaceId, Id = properties.Id },
        };

    private static Guid RequireId(string? id)
        => id is null
            ? throw new ResourceErrorException("MissingIdentifier", "The Spark Custom Pool ID is required for this operation.")
            : ParseId(id, "id");

    private static Guid ParseId(string value, string target)
        => Guid.TryParse(value, out var result)
            ? result
            : throw new ResourceErrorException("InvalidIdentifier", $"'{value}' is not a valid GUID.", target);
}
