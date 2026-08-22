using CoreModels = Microsoft.Fabric.Api.Core.Models;

namespace Bicep.Extension.Fabric.Handlers;

public sealed class DeploymentPipelineHandler : FabricResourceHandlerBase<DeploymentPipeline, DeploymentPipelineIdentifiers>
{
    protected override Task<ResourceResponse> Preview(ResourceRequest request, CancellationToken cancellationToken)
        => Task.FromResult(GetResponse(request));

    protected override Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            CoreModels.DeploymentPipeline result;

            if (request.Properties.Id is { } pipelineIdValue)
            {
                var pipelineId = ParseId(pipelineIdValue, "id");

                result = (await client.Core.DeploymentPipelines.UpdateDeploymentPipelineAsync(
                    pipelineId,
                    new CoreModels.UpdateDeploymentPipelineRequest
                    {
                        DisplayName = request.Properties.DisplayName,
                        Description = request.Properties.Description,
                    },
                    cancellationToken)).Value;

                await SyncStagesAsync(client, pipelineId, request.Properties.Stages, cancellationToken);
            }
            else
            {
                var create = new CoreModels.CreateDeploymentPipelineRequest(
                    request.Properties.DisplayName,
                    request.Properties.Stages.Select(stage => new CoreModels.DeploymentPipelineStageRequest(stage.DisplayName)
                    {
                        Description = stage.Description,
                        IsPublic = stage.IsPublic,
                    }))
                {
                    Description = request.Properties.Description,
                };

                result = (await client.Core.DeploymentPipelines.CreateDeploymentPipelineAsync(create, cancellationToken)).Value;

                await AssignStageWorkspacesAsync(client, result.Id, request.Properties.Stages, cancellationToken);
            }

            return BuildResponse(request.Type, request.ApiVersion, await ToPropertiesAsync(client, result, cancellationToken));
        });

    protected override Task<ResourceResponse> Get(ReferenceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            var pipelineId = RequireId(request.Identifiers.Id);
            var result = (await client.Core.DeploymentPipelines.GetDeploymentPipelineAsync(pipelineId, cancellationToken)).Value;

            return BuildResponse(request.Type, request.ApiVersion, await ToPropertiesAsync(client, result, cancellationToken));
        });

    protected override Task<ResourceResponse> Delete(ReferenceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            await client.Core.DeploymentPipelines.DeleteDeploymentPipelineAsync(RequireId(request.Identifiers.Id), cancellationToken);
            return GetResponse(request, properties: null);
        });

    protected override DeploymentPipelineIdentifiers GetIdentifiers(DeploymentPipeline properties)
        => new() { Id = properties.Id };

    private static async Task AssignStageWorkspacesAsync(
        Microsoft.Fabric.Api.FabricClient client,
        Guid pipelineId,
        DeploymentPipelineStage[] inputStages,
        CancellationToken cancellationToken)
    {
        var createdStages = await ListStagesOrderedAsync(client, pipelineId, cancellationToken);

        for (var i = 0; i < inputStages.Length && i < createdStages.Count; i++)
        {
            if (inputStages[i].WorkspaceId is { } workspaceId)
            {
                await client.Core.DeploymentPipelines.AssignWorkspaceToStageAsync(
                    pipelineId,
                    createdStages[i].Id,
                    new CoreModels.DeploymentPipelineAssignWorkspaceRequest(ParseId(workspaceId, "stages.workspaceId")),
                    cancellationToken);
            }
        }
    }

    private static async Task SyncStagesAsync(
        Microsoft.Fabric.Api.FabricClient client,
        Guid pipelineId,
        DeploymentPipelineStage[] inputStages,
        CancellationToken cancellationToken)
    {
        var currentStages = await ListStagesOrderedAsync(client, pipelineId, cancellationToken);

        for (var i = 0; i < inputStages.Length && i < currentStages.Count; i++)
        {
            var input = inputStages[i];
            var current = currentStages[i];

            if (input.DisplayName != current.DisplayName || input.Description != current.Description || input.IsPublic != current.IsPublic)
            {
                await client.Core.DeploymentPipelines.UpdateDeploymentPipelineStageAsync(
                    pipelineId,
                    current.Id,
                    new CoreModels.DeploymentPipelineStageRequest(input.DisplayName)
                    {
                        Description = input.Description,
                        IsPublic = input.IsPublic,
                    },
                    cancellationToken);
            }

            var currentWorkspaceId = current.WorkspaceId?.ToString();
            if (input.WorkspaceId != currentWorkspaceId)
            {
                if (input.WorkspaceId is { } workspaceId)
                {
                    await client.Core.DeploymentPipelines.AssignWorkspaceToStageAsync(
                        pipelineId,
                        current.Id,
                        new CoreModels.DeploymentPipelineAssignWorkspaceRequest(ParseId(workspaceId, "stages.workspaceId")),
                        cancellationToken);
                }
                else
                {
                    await client.Core.DeploymentPipelines.UnassignWorkspaceFromStageAsync(pipelineId, current.Id, cancellationToken);
                }
            }
        }
    }

    private static async Task<List<CoreModels.DeploymentPipelineStage>> ListStagesOrderedAsync(
        Microsoft.Fabric.Api.FabricClient client,
        Guid pipelineId,
        CancellationToken cancellationToken)
    {
        var stages = new List<CoreModels.DeploymentPipelineStage>();
        await foreach (var stage in client.Core.DeploymentPipelines.ListDeploymentPipelineStagesAsync(pipelineId, cancellationToken: cancellationToken))
        {
            stages.Add(stage);
        }

        return [.. stages.OrderBy(stage => stage.Order)];
    }

    private static async Task<DeploymentPipeline> ToPropertiesAsync(
        Microsoft.Fabric.Api.FabricClient client,
        CoreModels.DeploymentPipeline pipeline,
        CancellationToken cancellationToken)
    {
        var stages = await ListStagesOrderedAsync(client, pipeline.Id, cancellationToken);

        return new DeploymentPipeline
        {
            Id = pipeline.Id.ToString(),
            DisplayName = pipeline.DisplayName,
            Description = pipeline.Description,
            Stages = [.. stages.Select(stage => new DeploymentPipelineStage
            {
                Id = stage.Id.ToString(),
                DisplayName = stage.DisplayName,
                Description = stage.Description,
                IsPublic = stage.IsPublic ?? false,
                WorkspaceId = stage.WorkspaceId?.ToString(),
            })],
        };
    }

    private static ResourceResponse BuildResponse(string type, string? apiVersion, DeploymentPipeline properties)
        => new()
        {
            Type = type,
            ApiVersion = apiVersion,
            Properties = properties,
            Identifiers = new DeploymentPipelineIdentifiers { Id = properties.Id },
        };

    private static Guid RequireId(string? id)
        => id is null
            ? throw new ResourceErrorException("MissingIdentifier", "The Deployment Pipeline ID is required for this operation.")
            : ParseId(id, "id");

    private static Guid ParseId(string value, string target)
        => Guid.TryParse(value, out var result)
            ? result
            : throw new ResourceErrorException("InvalidIdentifier", $"'{value}' is not a valid GUID.", target);
}
