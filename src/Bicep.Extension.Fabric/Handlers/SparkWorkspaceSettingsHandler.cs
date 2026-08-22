using SparkModels = Microsoft.Fabric.Api.Spark.Models;

namespace Bicep.Extension.Fabric.Handlers;

public sealed class SparkWorkspaceSettingsHandler : FabricResourceHandlerBase<SparkWorkspaceSettings, SparkWorkspaceSettingsIdentifiers>
{
    protected override Task<ResourceResponse> Preview(ResourceRequest request, CancellationToken cancellationToken)
        => Task.FromResult(GetResponse(request));

    protected override Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            var workspaceId = ParseId(request.Properties.WorkspaceId, "workspaceId");

            var result = (await client.Spark.WorkspaceSettings.UpdateSparkSettingsAsync(
                workspaceId,
                ToSdkUpdateRequest(request.Properties),
                cancellationToken)).Value;

            return BuildResponse(request.Type, request.ApiVersion, ToProperties(result, workspaceId));
        });

    protected override Task<ResourceResponse> Get(ReferenceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            var workspaceId = ParseId(request.Identifiers.WorkspaceId, "workspaceId");
            var result = (await client.Spark.WorkspaceSettings.GetSparkSettingsAsync(workspaceId, cancellationToken)).Value;

            return BuildResponse(request.Type, request.ApiVersion, ToProperties(result, workspaceId));
        });

    protected override Task<ResourceResponse> Delete(ReferenceRequest request, CancellationToken cancellationToken)
        // The Fabric API has no delete operation for workspace Spark settings; they remain unchanged in Fabric
        // when this resource is removed from a Bicep deployment, matching the Terraform provider's behavior.
        => Task.FromResult(GetResponse(request, properties: null));

    protected override SparkWorkspaceSettingsIdentifiers GetIdentifiers(SparkWorkspaceSettings properties)
        => new() { WorkspaceId = properties.WorkspaceId };

    private static SparkModels.UpdateWorkspaceSparkSettingsRequest ToSdkUpdateRequest(SparkWorkspaceSettings properties)
    {
        var request = new SparkModels.UpdateWorkspaceSparkSettingsRequest();

        if (properties.AutomaticLog?.Enabled is { } enabled)
        {
            request.AutomaticLog = new SparkModels.AutomaticLogProperties(enabled);
        }

        if (properties.Environment is { } environment)
        {
            request.Environment = new SparkModels.EnvironmentProperties
            {
                Name = environment.Name,
                RuntimeVersion = environment.RuntimeVersion,
            };
        }

        if (properties.HighConcurrency is { } highConcurrency)
        {
            request.HighConcurrency = new SparkModels.HighConcurrencyProperties
            {
                NotebookInteractiveRunEnabled = highConcurrency.NotebookInteractiveRunEnabled,
                NotebookPipelineRunEnabled = highConcurrency.NotebookPipelineRunEnabled,
            };
        }

        if (properties.Job is { } job)
        {
            request.Job = new SparkModels.SparkJobsProperties
            {
                ConservativeJobAdmissionEnabled = job.ConservativeJobAdmissionEnabled,
                SessionTimeoutInMinutes = job.SessionTimeoutInMinutes,
            };
        }

        if (properties.Pool is { } pool)
        {
            request.Pool = ToSdkPool(pool);
        }

        return request;
    }

    private static SparkModels.PoolProperties ToSdkPool(SparkPoolProperties pool)
    {
        var sdkPool = new SparkModels.PoolProperties { CustomizeComputeEnabled = pool.CustomizeComputeEnabled };

        if (pool.DefaultPool is { } defaultPool)
        {
            sdkPool.DefaultPool = new SparkModels.InstancePool
            {
                Id = ParseOptionalId(defaultPool.Id, "pool.defaultPool.id"),
                Name = defaultPool.Name,
                Type = defaultPool.Type is { } type ? ToSdkPoolType(type) : null,
            };
        }

        if (pool.StarterPool is { } starterPool)
        {
            sdkPool.StarterPool = new SparkModels.StarterPoolProperties
            {
                MaxNodeCount = starterPool.MaxNodeCount,
                MaxExecutors = starterPool.MaxExecutors,
            };
        }

        return sdkPool;
    }

    private static SparkModels.CustomPoolType ToSdkPoolType(SparkCustomPoolType value)
        => value switch
        {
            SparkCustomPoolType.Workspace => SparkModels.CustomPoolType.Workspace,
            SparkCustomPoolType.Capacity => SparkModels.CustomPoolType.Capacity,
            _ => throw new ResourceErrorException("InvalidProperty", $"Unsupported pool type '{value}'.", "pool.defaultPool.type"),
        };

    private static SparkCustomPoolType ToModelPoolType(SparkModels.CustomPoolType? value)
        => value?.ToString() switch
        {
            nameof(SparkCustomPoolType.Workspace) => SparkCustomPoolType.Workspace,
            nameof(SparkCustomPoolType.Capacity) => SparkCustomPoolType.Capacity,
            var other => throw new ResourceErrorException("InvalidResponse", $"Unsupported pool type '{other}' returned by the Fabric API."),
        };

    private static SparkWorkspaceSettings ToProperties(SparkModels.WorkspaceSparkSettings settings, Guid workspaceId)
        => new()
        {
            WorkspaceId = workspaceId.ToString(),
            AutomaticLog = settings.AutomaticLog is { } automaticLog ? new SparkAutomaticLogProperties { Enabled = automaticLog.Enabled } : null,
            Environment = settings.Environment is { } environment
                ? new SparkEnvironmentProperties { Name = environment.Name, RuntimeVersion = environment.RuntimeVersion }
                : null,
            HighConcurrency = settings.HighConcurrency is { } highConcurrency
                ? new SparkHighConcurrencyProperties
                {
                    NotebookInteractiveRunEnabled = highConcurrency.NotebookInteractiveRunEnabled,
                    NotebookPipelineRunEnabled = highConcurrency.NotebookPipelineRunEnabled,
                }
                : null,
            Job = settings.Job is { } job
                ? new SparkJobProperties
                {
                    ConservativeJobAdmissionEnabled = job.ConservativeJobAdmissionEnabled,
                    SessionTimeoutInMinutes = job.SessionTimeoutInMinutes,
                }
                : null,
            Pool = settings.Pool is { } pool
                ? new SparkPoolProperties
                {
                    CustomizeComputeEnabled = pool.CustomizeComputeEnabled,
                    DefaultPool = pool.DefaultPool is { } defaultPool
                        ? new SparkInstancePool { Id = defaultPool.Id?.ToString(), Name = defaultPool.Name, Type = defaultPool.Type is { } type ? ToModelPoolType(type) : null }
                        : null,
                    StarterPool = pool.StarterPool is { } starterPool
                        ? new SparkStarterPoolProperties { MaxNodeCount = starterPool.MaxNodeCount, MaxExecutors = starterPool.MaxExecutors }
                        : null,
                }
                : null,
        };

    private static ResourceResponse BuildResponse(string type, string? apiVersion, SparkWorkspaceSettings properties)
        => new()
        {
            Type = type,
            ApiVersion = apiVersion,
            Properties = properties,
            Identifiers = new SparkWorkspaceSettingsIdentifiers { WorkspaceId = properties.WorkspaceId },
        };

    private static Guid ParseId(string value, string target)
        => Guid.TryParse(value, out var result)
            ? result
            : throw new ResourceErrorException("InvalidIdentifier", $"'{value}' is not a valid GUID.", target);

    private static Guid? ParseOptionalId(string? value, string target)
        => value is null ? null : ParseId(value, target);
}
