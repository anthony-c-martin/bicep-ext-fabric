using Microsoft.Fabric.Api;
using Microsoft.Fabric.Api.Core.Models;
using CoreModels = Microsoft.Fabric.Api.Core.Models;

namespace Bicep.Extension.Fabric.Handlers;

public class WorkspaceHandler : FabricResourceHandlerBase<FabricWorkspace, WorkspaceIdentifiers>
{
    protected override Task<ResourceResponse> Preview(ResourceRequest request, CancellationToken cancellationToken)
        => Task.FromResult(GetResponse(request));

    protected override Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            Guid workspaceId;

            if (request.Properties.Id is { } workspaceIdValue)
            {
                workspaceId = ParseGuid(workspaceIdValue, "id");
                var update = new UpdateWorkspaceRequest
                {
                    DisplayName = request.Properties.DisplayName,
                    Description = request.Properties.Description,
                };

                await client.Core.Workspaces.UpdateWorkspaceAsync(workspaceId, update, cancellationToken);

                if (request.Properties.CapacityId is { } capacityId)
                {
                    await client.Core.Workspaces.AssignToCapacityAsync(
                        workspaceId,
                        new AssignWorkspaceToCapacityRequest(ParseGuid(capacityId, "capacityId")),
                        cancellationToken);
                }

                if (request.Properties.DomainId is { } domainId)
                {
                    await client.Core.Workspaces.AssignToDomainAsync(
                        workspaceId,
                        new AssignWorkspaceToDomainRequest(ParseGuid(domainId, "domainId")),
                        cancellationToken);
                }
            }
            else
            {
                var create = new CreateWorkspaceRequest(request.Properties.DisplayName)
                {
                    Description = request.Properties.Description,
                    CapacityId = ParseOptionalGuid(request.Properties.CapacityId, "capacityId"),
                    DomainId = ParseOptionalGuid(request.Properties.DomainId, "domainId"),
                };

                workspaceId = (await client.Core.Workspaces.CreateWorkspaceAsync(create, cancellationToken)).Value.Id;
            }

            if (request.Properties.Identity is not null)
            {
                await ProvisionIdentityAsync(client, workspaceId, cancellationToken);
            }

            // Re-read the workspace so that the identity, capacity region and OneLake endpoints,
            // which are only returned by the get operation, are reflected in the output.
            var result = (await client.Core.Workspaces.GetWorkspaceAsync(workspaceId, cancellationToken: cancellationToken)).Value;

            return BuildResponse(request.Type, request.ApiVersion, ToProperties(result));
        });

    protected override Task<ResourceResponse> Get(ReferenceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            var result = (await client.Core.Workspaces.GetWorkspaceAsync(
                RequireGuid(request.Identifiers.Id, "workspace ID"),
                cancellationToken: cancellationToken)).Value;

            return BuildResponse(request.Type, request.ApiVersion, ToProperties(result));
        });

    protected override Task<ResourceResponse> Delete(ReferenceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            await client.Core.Workspaces.DeleteWorkspaceAsync(RequireGuid(request.Identifiers.Id, "workspace ID"), cancellationToken);
            return GetResponse(request, properties: null);
        });

    protected override WorkspaceIdentifiers GetIdentifiers(FabricWorkspace properties)
        => new() { Id = properties.Id };

    /// <summary>
    /// Provisioning a workspace identity is not idempotent: the API fails when one already exists,
    /// so an existing identity is treated as success.
    /// </summary>
    private static async Task ProvisionIdentityAsync(FabricClient client, Guid workspaceId, CancellationToken cancellationToken)
    {
        try
        {
            await client.Core.Workspaces.ProvisionIdentityAsync(workspaceId, cancellationToken);
        }
        catch (Azure.RequestFailedException exception) when (exception.Status == 409)
        {
            // The workspace identity already exists.
        }
    }

    private static FabricWorkspace ToProperties(CoreModels.Workspace workspace)
        => new()
        {
            Id = workspace.Id.ToString(),
            DisplayName = workspace.DisplayName,
            Description = workspace.Description,
            CapacityId = workspace.CapacityId?.ToString(),
            DomainId = workspace.DomainId?.ToString(),
            Type = workspace.Type?.ToString(),
            ApiEndpoint = workspace.ApiEndpoint?.ToString(),
            CapacityRegion = workspace.CapacityRegion?.ToString(),
        };

    private static FabricWorkspace ToProperties(WorkspaceInfo workspace)
    {
        var properties = ToProperties((CoreModels.Workspace)workspace);

        properties.CapacityAssignmentProgress = workspace.CapacityAssignmentProgress?.ToString();
        properties.Identity = workspace.WorkspaceIdentity is { } identity
            ? new WorkspaceManagedIdentity
            {
                Type = WorkspaceIdentityType.SystemAssigned,
                ApplicationId = identity.ApplicationId.ToString(),
                ServicePrincipalId = identity.ServicePrincipalId.ToString(),
            }
            : null;
        properties.OneLakeEndpoints = workspace.OneLakeEndpoints is { } endpoints
            ? new WorkspaceOneLakeEndpoints
            {
                BlobEndpoint = endpoints.BlobEndpoint,
                DfsEndpoint = endpoints.DfsEndpoint,
            }
            : null;

        return properties;
    }

    private static ResourceResponse BuildResponse(string type, string? apiVersion, FabricWorkspace properties)
        => new()
        {
            Type = type,
            ApiVersion = apiVersion,
            Properties = properties,
            Identifiers = new WorkspaceIdentifiers { Id = properties.Id },
        };
}
