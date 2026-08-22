using CoreModels = Microsoft.Fabric.Api.Core.Models;

namespace Bicep.Extension.Fabric.Handlers;

public sealed class ConnectionRoleAssignmentHandler : FabricResourceHandlerBase<ConnectionRoleAssignment, ConnectionRoleAssignmentIdentifiers>
{
    protected override Task<ResourceResponse> Preview(ResourceRequest request, CancellationToken cancellationToken)
        => Task.FromResult(GetResponse(request));

    protected override Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            var connectionId = ParseId(request.Properties.ConnectionId, "connectionId");
            CoreModels.ConnectionRoleAssignment result;

            if (request.Properties.Id is { } assignmentIdValue)
            {
                var assignmentId = ParseId(assignmentIdValue, "id");
                result = (await client.Core.Connections.UpdateConnectionRoleAssignmentAsync(
                    connectionId,
                    assignmentId,
                    new CoreModels.UpdateConnectionRoleAssignmentRequest(ToSdkRole(request.Properties.Role)),
                    cancellationToken)).Value;
            }
            else
            {
                var create = new CoreModels.AddConnectionRoleAssignmentRequest(
                    ToSdkPrincipal(request.Properties.Principal),
                    ToSdkRole(request.Properties.Role));

                result = (await client.Core.Connections.AddConnectionRoleAssignmentAsync(connectionId, create, cancellationToken)).Value;
            }

            return BuildResponse(request.Type, request.ApiVersion, ToProperties(result, connectionId));
        });

    protected override Task<ResourceResponse> Get(ReferenceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            var connectionId = ParseId(request.Identifiers.ConnectionId, "connectionId");
            var result = (await client.Core.Connections.GetConnectionRoleAssignmentAsync(
                connectionId,
                RequireId(request.Identifiers.Id),
                cancellationToken)).Value;

            return BuildResponse(request.Type, request.ApiVersion, ToProperties(result, connectionId));
        });

    protected override Task<ResourceResponse> Delete(ReferenceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            await client.Core.Connections.DeleteConnectionRoleAssignmentAsync(
                ParseId(request.Identifiers.ConnectionId, "connectionId"),
                RequireId(request.Identifiers.Id),
                cancellationToken);

            return GetResponse(request, properties: null);
        });

    protected override ConnectionRoleAssignmentIdentifiers GetIdentifiers(ConnectionRoleAssignment properties)
        => new() { ConnectionId = properties.ConnectionId, Id = properties.Id };

    private static CoreModels.Principal ToSdkPrincipal(ConnectionPrincipal principal)
    {
        var id = ParseId(principal.Id, "principal.id");
        return principal.Type switch
        {
            ConnectionPrincipalType.User => new CoreModels.UserPrincipal(id),
            ConnectionPrincipalType.Group => new CoreModels.GroupPrincipal(id),
            ConnectionPrincipalType.ServicePrincipal => new CoreModels.ServicePrincipal(id),
            ConnectionPrincipalType.ServicePrincipalProfile => new CoreModels.ServicePrincipalProfilePrincipal(id),
            ConnectionPrincipalType.EntireTenant => new CoreModels.EntireTenantPrincipal(id),
            _ => throw new ResourceErrorException("InvalidProperty", $"Unsupported principal type '{principal.Type}'.", "principal.type"),
        };
    }

    private static ConnectionPrincipal ToModelPrincipal(CoreModels.Principal principal)
        => new()
        {
            Id = principal.Id.ToString(),
            Type = principal switch
            {
                CoreModels.UserPrincipal => ConnectionPrincipalType.User,
                CoreModels.GroupPrincipal => ConnectionPrincipalType.Group,
                CoreModels.ServicePrincipal => ConnectionPrincipalType.ServicePrincipal,
                CoreModels.ServicePrincipalProfilePrincipal => ConnectionPrincipalType.ServicePrincipalProfile,
                CoreModels.EntireTenantPrincipal => ConnectionPrincipalType.EntireTenant,
                _ => throw new ResourceErrorException("InvalidResponse", $"Unsupported principal type '{principal.GetType().Name}' returned by the Fabric API."),
            },
        };

    private static CoreModels.ConnectionRole ToSdkRole(ConnectionRole role)
        => role switch
        {
            ConnectionRole.Owner => CoreModels.ConnectionRole.Owner,
            ConnectionRole.User => CoreModels.ConnectionRole.User,
            ConnectionRole.UserWithReshare => CoreModels.ConnectionRole.UserWithReshare,
            _ => throw new ResourceErrorException("InvalidProperty", $"Unsupported connection role '{role}'.", "role"),
        };

    private static ConnectionRole ToModelRole(CoreModels.ConnectionRole role)
        => role.ToString() switch
        {
            nameof(ConnectionRole.Owner) => ConnectionRole.Owner,
            nameof(ConnectionRole.User) => ConnectionRole.User,
            nameof(ConnectionRole.UserWithReshare) => ConnectionRole.UserWithReshare,
            var other => throw new ResourceErrorException("InvalidResponse", $"Unsupported connection role '{other}' returned by the Fabric API."),
        };

    private static ConnectionRoleAssignment ToProperties(CoreModels.ConnectionRoleAssignment assignment, Guid connectionId)
        => new()
        {
            ConnectionId = connectionId.ToString(),
            Id = assignment.Id.ToString(),
            Principal = ToModelPrincipal(assignment.Principal),
            Role = ToModelRole(assignment.Role),
        };

    private static ResourceResponse BuildResponse(string type, string? apiVersion, ConnectionRoleAssignment properties)
        => new()
        {
            Type = type,
            ApiVersion = apiVersion,
            Properties = properties,
            Identifiers = new ConnectionRoleAssignmentIdentifiers { ConnectionId = properties.ConnectionId, Id = properties.Id },
        };

    private static Guid RequireId(string? id)
        => id is null
            ? throw new ResourceErrorException("MissingIdentifier", "The Connection Role Assignment ID is required for this operation.")
            : ParseId(id, "id");

    private static Guid ParseId(string value, string target)
        => Guid.TryParse(value, out var result)
            ? result
            : throw new ResourceErrorException("InvalidIdentifier", $"'{value}' is not a valid GUID.", target);
}
