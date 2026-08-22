using EnvironmentModels = Microsoft.Fabric.Api.Environment.Models;

namespace Bicep.Extension.Fabric.Handlers;

public sealed class SparkEnvironmentSettingsHandler : FabricResourceHandlerBase<SparkEnvironmentSettings, SparkEnvironmentSettingsIdentifiers>
{
    protected override Task<ResourceResponse> Preview(ResourceRequest request, CancellationToken cancellationToken)
        => Task.FromResult(GetResponse(request));

    protected override Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            var workspaceId = ParseId(request.Properties.WorkspaceId, "workspaceId");
            var environmentId = ParseId(request.Properties.EnvironmentId, "environmentId");

            // Only the staging Spark compute settings are directly writable; publishing promotes
            // the staged settings to the published environment (there is no separate "publish"
            // request body - it always publishes whatever is currently staged).
            await client.Environment.Staging.UpdateSparkComputeAsync(workspaceId, environmentId, ToSdkUpdateRequest(request.Properties), cancellationToken);

            if (request.Properties.PublicationStatus == SparkEnvironmentPublicationStatus.Published)
            {
                await client.Environment.Items.PublishEnvironmentAsync(workspaceId, environmentId, cancellationToken);
            }

            var result = await GetSparkComputeAsync(client, workspaceId, environmentId, request.Properties.PublicationStatus, cancellationToken);
            return BuildResponse(request.Type, request.ApiVersion, ToProperties(result, workspaceId, environmentId, request.Properties.PublicationStatus));
        });

    protected override Task<ResourceResponse> Get(ReferenceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            var workspaceId = ParseId(request.Identifiers.WorkspaceId, "workspaceId");
            var environmentId = ParseId(request.Identifiers.EnvironmentId, "environmentId");

            var result = await GetSparkComputeAsync(client, workspaceId, environmentId, request.Identifiers.PublicationStatus, cancellationToken);
            return BuildResponse(request.Type, request.ApiVersion, ToProperties(result, workspaceId, environmentId, request.Identifiers.PublicationStatus));
        });

    protected override Task<ResourceResponse> Delete(ReferenceRequest request, CancellationToken cancellationToken)
        // The Fabric API has no operation to reset Spark environment settings; they remain unchanged
        // in Fabric when this resource is removed from a Bicep deployment.
        => Task.FromResult(GetResponse(request, properties: null));

    protected override SparkEnvironmentSettingsIdentifiers GetIdentifiers(SparkEnvironmentSettings properties)
        => new() { WorkspaceId = properties.WorkspaceId, EnvironmentId = properties.EnvironmentId, PublicationStatus = properties.PublicationStatus };

    private static Task<Azure.Response<EnvironmentModels.EnvironmentSparkCompute>> GetSparkComputeAsync(
        Microsoft.Fabric.Api.FabricClient client,
        Guid workspaceId,
        Guid environmentId,
        SparkEnvironmentPublicationStatus publicationStatus,
        CancellationToken cancellationToken)
        => publicationStatus switch
        {
            SparkEnvironmentPublicationStatus.Published => client.Environment.Published.GetSparkComputeAsync(workspaceId, environmentId, cancellationToken),
            SparkEnvironmentPublicationStatus.Staging => client.Environment.Staging.GetSparkComputeAsync(workspaceId, environmentId, cancellationToken),
            _ => throw new ResourceErrorException("InvalidProperty", $"Unsupported publication status '{publicationStatus}'.", "publicationStatus"),
        };

    private static EnvironmentModels.UpdateEnvironmentSparkComputeRequest ToSdkUpdateRequest(SparkEnvironmentSettings properties)
    {
        var request = new EnvironmentModels.UpdateEnvironmentSparkComputeRequest
        {
            DriverCores = properties.DriverCores,
            DriverMemory = properties.DriverMemory is { } driverMemory ? new EnvironmentModels.CustomPoolMemory(driverMemory) : null,
            ExecutorCores = properties.ExecutorCores,
            ExecutorMemory = properties.ExecutorMemory is { } executorMemory ? new EnvironmentModels.CustomPoolMemory(executorMemory) : null,
            RuntimeVersion = properties.RuntimeVersion,
        };

        if (properties.DynamicExecutorAllocation is { } allocation)
        {
            request.DynamicExecutorAllocation = new EnvironmentModels.DynamicExecutorAllocationProperties(
                allocation.Enabled ?? false,
                allocation.MinExecutors ?? 0,
                allocation.MaxExecutors ?? 0);
        }

        if (properties.Pool is { } pool)
        {
            request.InstancePool = new EnvironmentModels.InstancePool
            {
                Name = pool.Name,
                Type = pool.Type is { } type ? ToSdkPoolType(type) : null,
            };
        }

        foreach (var (key, value) in properties.SparkProperties ?? [])
        {
            request.SparkProperties.Add(new EnvironmentModels.SparkProperty { Key = key, Value = value });
        }

        return request;
    }

    private static EnvironmentModels.CustomPoolType ToSdkPoolType(SparkEnvironmentPoolType value)
        => value switch
        {
            SparkEnvironmentPoolType.Capacity => EnvironmentModels.CustomPoolType.Capacity,
            SparkEnvironmentPoolType.Workspace => EnvironmentModels.CustomPoolType.Workspace,
            _ => throw new ResourceErrorException("InvalidProperty", $"Unsupported pool type '{value}'.", "pool.type"),
        };

    private static SparkEnvironmentPoolType ToModelPoolType(EnvironmentModels.CustomPoolType? value)
        => value?.ToString() switch
        {
            nameof(SparkEnvironmentPoolType.Capacity) => SparkEnvironmentPoolType.Capacity,
            nameof(SparkEnvironmentPoolType.Workspace) => SparkEnvironmentPoolType.Workspace,
            var other => throw new ResourceErrorException("InvalidResponse", $"Unsupported pool type '{other}' returned by the Fabric API."),
        };

    private static SparkEnvironmentSettings ToProperties(EnvironmentModels.EnvironmentSparkCompute compute, Guid workspaceId, Guid environmentId, SparkEnvironmentPublicationStatus publicationStatus)
        => new()
        {
            WorkspaceId = workspaceId.ToString(),
            EnvironmentId = environmentId.ToString(),
            PublicationStatus = publicationStatus,
            DriverCores = compute.DriverCores,
            DriverMemory = compute.DriverMemory?.ToString(),
            ExecutorCores = compute.ExecutorCores,
            ExecutorMemory = compute.ExecutorMemory?.ToString(),
            RuntimeVersion = compute.RuntimeVersion,
            DynamicExecutorAllocation = compute.DynamicExecutorAllocation is { } allocation
                ? new SparkEnvironmentDynamicExecutorAllocation { Enabled = allocation.Enabled, MinExecutors = allocation.MinExecutors, MaxExecutors = allocation.MaxExecutors }
                : null,
            Pool = compute.InstancePool is { } pool
                ? new SparkEnvironmentPool { Id = pool.Id?.ToString(), Name = pool.Name, Type = pool.Type is { } type ? ToModelPoolType(type) : null }
                : null,
            SparkProperties = compute.SparkProperties.Count > 0
                ? compute.SparkProperties.ToDictionary(property => property.Key ?? string.Empty, property => property.Value ?? string.Empty)
                : null,
        };

    private static ResourceResponse BuildResponse(string type, string? apiVersion, SparkEnvironmentSettings properties)
        => new()
        {
            Type = type,
            ApiVersion = apiVersion,
            Properties = properties,
            Identifiers = new SparkEnvironmentSettingsIdentifiers { WorkspaceId = properties.WorkspaceId, EnvironmentId = properties.EnvironmentId, PublicationStatus = properties.PublicationStatus },
        };

    private static Guid ParseId(string value, string target)
        => Guid.TryParse(value, out var result)
            ? result
            : throw new ResourceErrorException("InvalidIdentifier", $"'{value}' is not a valid GUID.", target);
}
