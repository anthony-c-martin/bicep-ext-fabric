using CoreModels = Microsoft.Fabric.Api.Core.Models;

namespace Bicep.Extension.Fabric.Handlers;

public sealed class WorkspaceManagedPrivateEndpointHandler : FabricResourceHandlerBase<WorkspaceManagedPrivateEndpoint, WorkspaceManagedPrivateEndpointIdentifiers>
{
    protected override Task<ResourceResponse> Preview(ResourceRequest request, CancellationToken cancellationToken)
        => Task.FromResult(GetResponse(request));

    protected override Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            if (request.Properties.Id is not null)
            {
                // The Fabric API has no update operation for Managed Private Endpoints; any property change requires delete + recreate.
                throw new ResourceErrorException(
                    "UpdateNotSupported",
                    "Updating a Workspace Managed Private Endpoint is not supported by the Fabric API. Delete and recreate the resource instead.");
            }

            var workspaceId = ParseId(request.Properties.WorkspaceId, "workspaceId");

            var create = new CoreModels.CreateManagedPrivateEndpointRequest(request.Properties.Name, request.Properties.TargetPrivateLinkResourceId)
            {
                TargetSubresourceType = request.Properties.TargetSubresourceType,
                RequestMessage = request.Properties.RequestMessage,
            };

            var result = (await client.Core.ManagedPrivateEndpoints.CreateWorkspaceManagedPrivateEndpointAsync(workspaceId, create, cancellationToken)).Value;

            return BuildResponse(request.Type, request.ApiVersion, ToProperties(result, workspaceId, request.Properties.RequestMessage));
        });

    protected override Task<ResourceResponse> Get(ReferenceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            var workspaceId = ParseId(request.Identifiers.WorkspaceId, "workspaceId");
            var result = (await client.Core.ManagedPrivateEndpoints.GetWorkspaceManagedPrivateEndpointAsync(
                workspaceId,
                RequireId(request.Identifiers.Id),
                cancellationToken)).Value;

            return BuildResponse(request.Type, request.ApiVersion, ToProperties(result, workspaceId, requestMessage: null));
        });

    protected override Task<ResourceResponse> Delete(ReferenceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            await client.Core.ManagedPrivateEndpoints.DeleteWorkspaceManagedPrivateEndpointAsync(
                ParseId(request.Identifiers.WorkspaceId, "workspaceId"),
                RequireId(request.Identifiers.Id),
                cancellationToken);

            return GetResponse(request, properties: null);
        });

    protected override WorkspaceManagedPrivateEndpointIdentifiers GetIdentifiers(WorkspaceManagedPrivateEndpoint properties)
        => new() { WorkspaceId = properties.WorkspaceId, Id = properties.Id };

    private static PrivateEndpointProvisioningState ToModelProvisioningState(CoreModels.PrivateEndpointProvisioningState? value)
        => value?.ToString() switch
        {
            nameof(PrivateEndpointProvisioningState.Provisioning) => PrivateEndpointProvisioningState.Provisioning,
            nameof(PrivateEndpointProvisioningState.Succeeded) => PrivateEndpointProvisioningState.Succeeded,
            nameof(PrivateEndpointProvisioningState.Updating) => PrivateEndpointProvisioningState.Updating,
            nameof(PrivateEndpointProvisioningState.Deleting) => PrivateEndpointProvisioningState.Deleting,
            nameof(PrivateEndpointProvisioningState.Failed) => PrivateEndpointProvisioningState.Failed,
            var other => throw new ResourceErrorException("InvalidResponse", $"Unsupported provisioning state '{other}' returned by the Fabric API."),
        };

    private static ManagedPrivateEndpointConnectionStatus ToModelConnectionStatus(CoreModels.ConnectionStatus? value)
        => value?.ToString() switch
        {
            nameof(ManagedPrivateEndpointConnectionStatus.Pending) => ManagedPrivateEndpointConnectionStatus.Pending,
            nameof(ManagedPrivateEndpointConnectionStatus.Approved) => ManagedPrivateEndpointConnectionStatus.Approved,
            nameof(ManagedPrivateEndpointConnectionStatus.Rejected) => ManagedPrivateEndpointConnectionStatus.Rejected,
            nameof(ManagedPrivateEndpointConnectionStatus.Disconnected) => ManagedPrivateEndpointConnectionStatus.Disconnected,
            var other => throw new ResourceErrorException("InvalidResponse", $"Unsupported connection status '{other}' returned by the Fabric API."),
        };

    private static WorkspaceManagedPrivateEndpoint ToProperties(CoreModels.ManagedPrivateEndpoint endpoint, Guid workspaceId, string? requestMessage)
        => new()
        {
            WorkspaceId = workspaceId.ToString(),
            Id = endpoint.Id?.ToString(),
            Name = endpoint.Name,
            TargetPrivateLinkResourceId = endpoint.TargetPrivateLinkResourceId,
            TargetSubresourceType = endpoint.TargetSubresourceType,
            RequestMessage = requestMessage,
            ProvisioningState = endpoint.ProvisioningState is { } state ? ToModelProvisioningState(state) : null,
            ConnectionState = endpoint.ConnectionState is { } connectionState
                ? new PrivateEndpointConnectionState
                {
                    Status = connectionState.Status is { } status ? ToModelConnectionStatus(status) : null,
                    Description = connectionState.Description,
                    ActionsRequired = connectionState.ActionsRequired,
                }
                : null,
        };

    private static ResourceResponse BuildResponse(string type, string? apiVersion, WorkspaceManagedPrivateEndpoint properties)
        => new()
        {
            Type = type,
            ApiVersion = apiVersion,
            Properties = properties,
            Identifiers = new WorkspaceManagedPrivateEndpointIdentifiers { WorkspaceId = properties.WorkspaceId, Id = properties.Id },
        };

    private static Guid RequireId(string? id)
        => id is null
            ? throw new ResourceErrorException("MissingIdentifier", "The Workspace Managed Private Endpoint ID is required for this operation.")
            : ParseId(id, "id");

    private static Guid ParseId(string value, string target)
        => Guid.TryParse(value, out var result)
            ? result
            : throw new ResourceErrorException("InvalidIdentifier", $"'{value}' is not a valid GUID.", target);
}
