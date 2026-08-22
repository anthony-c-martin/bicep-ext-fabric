using CoreModels = Microsoft.Fabric.Api.Core.Models;

namespace Bicep.Extension.Fabric.Handlers;

public sealed class GatewayRoleAssignmentHandler : FabricResourceHandlerBase<GatewayRoleAssignment, GatewayRoleAssignmentIdentifiers>
{
    protected override Task<ResourceResponse> Preview(ResourceRequest request, CancellationToken cancellationToken)
        => Task.FromResult(GetResponse(request));

    protected override Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            var gatewayId = ParseId(request.Properties.GatewayId, "gatewayId");
            CoreModels.GatewayRoleAssignment result;

            if (request.Properties.Id is { } assignmentIdValue)
            {
                var assignmentId = ParseId(assignmentIdValue, "id");
                result = (await client.Core.Gateways.UpdateGatewayRoleAssignmentAsync(
                    gatewayId,
                    assignmentId,
                    new CoreModels.UpdateGatewayRoleAssignmentRequest(ToSdkRole(request.Properties.Role)),
                    cancellationToken)).Value;
            }
            else
            {
                var create = new CoreModels.AddGatewayRoleAssignmentRequest(
                    ToSdkPrincipal(request.Properties.Principal),
                    ToSdkRole(request.Properties.Role));

                result = (await client.Core.Gateways.AddGatewayRoleAssignmentAsync(gatewayId, create, cancellationToken)).Value;
            }

            return BuildResponse(request.Type, request.ApiVersion, ToProperties(result, gatewayId));
        });

    protected override Task<ResourceResponse> Get(ReferenceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            var gatewayId = ParseId(request.Identifiers.GatewayId, "gatewayId");
            var result = (await client.Core.Gateways.GetGatewayRoleAssignmentAsync(
                gatewayId,
                RequireId(request.Identifiers.Id),
                cancellationToken)).Value;

            return BuildResponse(request.Type, request.ApiVersion, ToProperties(result, gatewayId));
        });

    protected override Task<ResourceResponse> Delete(ReferenceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            await client.Core.Gateways.DeleteGatewayRoleAssignmentAsync(
                ParseId(request.Identifiers.GatewayId, "gatewayId"),
                RequireId(request.Identifiers.Id),
                cancellationToken);

            return GetResponse(request, properties: null);
        });

    protected override GatewayRoleAssignmentIdentifiers GetIdentifiers(GatewayRoleAssignment properties)
        => new() { GatewayId = properties.GatewayId, Id = properties.Id };

    private static CoreModels.Principal ToSdkPrincipal(Principal principal)
    {
        var id = ParseId(principal.Id, "principal.id");
        return principal.Type switch
        {
            PrincipalType.User => new CoreModels.UserPrincipal(id),
            PrincipalType.Group => new CoreModels.GroupPrincipal(id),
            PrincipalType.ServicePrincipal => new CoreModels.ServicePrincipal(id),
            PrincipalType.ServicePrincipalProfile => new CoreModels.ServicePrincipalProfilePrincipal(id),
            _ => throw new ResourceErrorException("InvalidProperty", $"Unsupported principal type '{principal.Type}'.", "principal.type"),
        };
    }

    private static Principal ToModelPrincipal(CoreModels.Principal principal)
        => new()
        {
            Id = principal.Id.ToString(),
            Type = principal switch
            {
                CoreModels.UserPrincipal => PrincipalType.User,
                CoreModels.GroupPrincipal => PrincipalType.Group,
                CoreModels.ServicePrincipal => PrincipalType.ServicePrincipal,
                CoreModels.ServicePrincipalProfilePrincipal => PrincipalType.ServicePrincipalProfile,
                _ => throw new ResourceErrorException("InvalidResponse", $"Unsupported principal type '{principal.GetType().Name}' returned by the Fabric API."),
            },
        };

    private static CoreModels.GatewayRole ToSdkRole(GatewayRole role)
        => role switch
        {
            GatewayRole.Admin => CoreModels.GatewayRole.Admin,
            GatewayRole.ConnectionCreator => CoreModels.GatewayRole.ConnectionCreator,
            GatewayRole.ConnectionCreatorWithResharing => CoreModels.GatewayRole.ConnectionCreatorWithResharing,
            _ => throw new ResourceErrorException("InvalidProperty", $"Unsupported gateway role '{role}'.", "role"),
        };

    private static GatewayRole ToModelRole(CoreModels.GatewayRole role)
        => role.ToString() switch
        {
            nameof(GatewayRole.Admin) => GatewayRole.Admin,
            nameof(GatewayRole.ConnectionCreator) => GatewayRole.ConnectionCreator,
            nameof(GatewayRole.ConnectionCreatorWithResharing) => GatewayRole.ConnectionCreatorWithResharing,
            var other => throw new ResourceErrorException("InvalidResponse", $"Unsupported gateway role '{other}' returned by the Fabric API."),
        };

    private static GatewayRoleAssignment ToProperties(CoreModels.GatewayRoleAssignment assignment, Guid gatewayId)
        => new()
        {
            GatewayId = gatewayId.ToString(),
            Id = assignment.Id.ToString(),
            Principal = ToModelPrincipal(assignment.Principal),
            Role = ToModelRole(assignment.Role),
        };

    private static ResourceResponse BuildResponse(string type, string? apiVersion, GatewayRoleAssignment properties)
        => new()
        {
            Type = type,
            ApiVersion = apiVersion,
            Properties = properties,
            Identifiers = new GatewayRoleAssignmentIdentifiers { GatewayId = properties.GatewayId, Id = properties.Id },
        };

    private static Guid RequireId(string? id)
        => id is null
            ? throw new ResourceErrorException("MissingIdentifier", "The Gateway Role Assignment ID is required for this operation.")
            : ParseId(id, "id");

    private static Guid ParseId(string value, string target)
        => Guid.TryParse(value, out var result)
            ? result
            : throw new ResourceErrorException("InvalidIdentifier", $"'{value}' is not a valid GUID.", target);
}
