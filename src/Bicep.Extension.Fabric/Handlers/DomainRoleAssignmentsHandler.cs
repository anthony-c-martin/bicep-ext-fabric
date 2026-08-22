using AdminModels = Microsoft.Fabric.Api.Admin.Models;

namespace Bicep.Extension.Fabric.Handlers;

public sealed class DomainRoleAssignmentsHandler : FabricResourceHandlerBase<DomainRoleAssignments, DomainRoleAssignmentsIdentifiers>
{
    protected override Task<ResourceResponse> Preview(ResourceRequest request, CancellationToken cancellationToken)
        => Task.FromResult(GetResponse(request));

    protected override Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            var domainId = ParseGuid(request.Properties.DomainId, "domainId");
            var role = ToSdkRole(request.Properties.Role);

            var desired = request.Properties.Principals.ToDictionary(ToPrincipalKey, principal => principal, StringComparer.OrdinalIgnoreCase);
            var current = await ListPrincipalsAsync(client, domainId, request.Properties.Role, cancellationToken);

            var removed = current.Where(entry => !desired.ContainsKey(entry.Key)).Select(entry => entry.Value).ToList();
            if (removed.Count > 0)
            {
                var unassign = new AdminModels.DomainRoleUnassignmentRequest(role);
                foreach (var principal in removed)
                {
                    unassign.Principals.Add(principal);
                }

                await client.Admin.Domains.RoleAssignmentsBulkUnassignAsync(domainId, unassign, cancellationToken);
            }

            var added = desired.Where(entry => !current.ContainsKey(entry.Key)).Select(entry => ToSdkPrincipal(entry.Value)).ToList();
            if (added.Count > 0)
            {
                var assign = new AdminModels.DomainRoleAssignmentRequest(role);
                foreach (var principal in added)
                {
                    assign.Principals.Add(principal);
                }

                await client.Admin.Domains.RoleAssignmentsBulkAssignAsync(domainId, assign, cancellationToken);
            }

            return BuildResponse(request.Type, request.ApiVersion, request.Properties);
        });

    protected override Task<ResourceResponse> Get(ReferenceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            var domainId = ParseGuid(request.Identifiers.DomainId, "domainId");
            var current = await ListPrincipalsAsync(client, domainId, request.Identifiers.Role, cancellationToken);

            var properties = new DomainRoleAssignments
            {
                DomainId = request.Identifiers.DomainId,
                Role = request.Identifiers.Role,
                Principals = current.Values.Select(ToModelPrincipal).ToArray(),
            };

            return BuildResponse(request.Type, request.ApiVersion, properties);
        });

    protected override Task<ResourceResponse> Delete(ReferenceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            var domainId = ParseGuid(request.Identifiers.DomainId, "domainId");
            var current = await ListPrincipalsAsync(client, domainId, request.Identifiers.Role, cancellationToken);

            if (current.Count > 0)
            {
                var unassign = new AdminModels.DomainRoleUnassignmentRequest(ToSdkRole(request.Identifiers.Role));
                foreach (var principal in current.Values)
                {
                    unassign.Principals.Add(principal);
                }

                await client.Admin.Domains.RoleAssignmentsBulkUnassignAsync(domainId, unassign, cancellationToken);
            }

            return GetResponse(request, properties: null);
        });

    protected override DomainRoleAssignmentsIdentifiers GetIdentifiers(DomainRoleAssignments properties)
        => new()
        {
            DomainId = properties.DomainId,
            Role = properties.Role,
        };

    private static async Task<Dictionary<string, AdminModels.Principal>> ListPrincipalsAsync(
        Microsoft.Fabric.Api.FabricClient client,
        Guid domainId,
        DomainRole role,
        CancellationToken cancellationToken)
    {
        var sdkRole = ToSdkRole(role);
        var result = new Dictionary<string, AdminModels.Principal>(StringComparer.OrdinalIgnoreCase);

        await foreach (var assignment in client.Admin.Domains.ListRoleAssignmentsAsync(domainId, cancellationToken: cancellationToken))
        {
            if (assignment.Role == sdkRole && assignment.Principal is { } principal)
            {
                result[ToPrincipalKey(principal)] = principal;
            }
        }

        return result;
    }

    // Both sides of the diff must derive their key from a parsed Guid, so that principal IDs written
    // in any format Guid.TryParse accepts still match the canonical form returned by the Fabric API.
    internal static string ToPrincipalKey(DomainPrincipal principal)
    {
        if (principal.Type == DomainPrincipalType.EntireTenant)
        {
            return nameof(DomainPrincipalType.EntireTenant);
        }

        var id = ParseGuid(
            principal.Id ?? throw new ResourceErrorException(
                "MissingProperty",
                $"'principals.id' is required when 'type' is '{principal.Type}'.",
                "principals.id"),
            "principals.id");

        return $"{principal.Type}:{id}";
    }

    internal static string ToPrincipalKey(AdminModels.Principal principal)
        => principal is AdminModels.EntireTenantPrincipal
            ? nameof(DomainPrincipalType.EntireTenant)
            : $"{ToModelPrincipalType(principal)}:{principal.Id}";

    private static AdminModels.DomainRole ToSdkRole(DomainRole role)
        => role switch
        {
            DomainRole.Admin => AdminModels.DomainRole.Admin,
            DomainRole.Contributor => AdminModels.DomainRole.Contributor,
            _ => throw new ResourceErrorException("InvalidProperty", $"Unsupported domain role '{role}'.", "role"),
        };

    private static AdminModels.Principal ToSdkPrincipal(DomainPrincipal principal)
    {
        // The Fabric API models the whole-tenant assignment as a principal with an empty ID.
        if (principal.Type == DomainPrincipalType.EntireTenant)
        {
            return new AdminModels.EntireTenantPrincipal(Guid.Empty);
        }

        var id = ParseGuid(
            principal.Id ?? throw new ResourceErrorException(
                "MissingProperty",
                $"'principals.id' is required when 'type' is '{principal.Type}'.",
                "principals.id"),
            "principals.id");

        return principal.Type switch
        {
            DomainPrincipalType.User => new AdminModels.UserPrincipal(id),
            DomainPrincipalType.Group => new AdminModels.GroupPrincipal(id),
            DomainPrincipalType.ServicePrincipal => new AdminModels.ServicePrincipal(id),
            DomainPrincipalType.ServicePrincipalProfile => new AdminModels.ServicePrincipalProfilePrincipal(id),
            _ => throw new ResourceErrorException("InvalidProperty", $"Unsupported principal type '{principal.Type}'.", "principals.type"),
        };
    }

    private static DomainPrincipalType ToModelPrincipalType(AdminModels.Principal principal)
        => principal switch
        {
            AdminModels.UserPrincipal => DomainPrincipalType.User,
            AdminModels.GroupPrincipal => DomainPrincipalType.Group,
            AdminModels.ServicePrincipal => DomainPrincipalType.ServicePrincipal,
            AdminModels.ServicePrincipalProfilePrincipal => DomainPrincipalType.ServicePrincipalProfile,
            AdminModels.EntireTenantPrincipal => DomainPrincipalType.EntireTenant,
            _ => throw new ResourceErrorException("InvalidResponse", $"Unsupported principal type '{principal.GetType().Name}' returned by the Fabric API."),
        };

    private static DomainPrincipal ToModelPrincipal(AdminModels.Principal principal)
        => new()
        {
            Type = ToModelPrincipalType(principal),
            Id = principal is AdminModels.EntireTenantPrincipal ? null : principal.Id.ToString(),
        };

    private ResourceResponse BuildResponse(string type, string? apiVersion, DomainRoleAssignments properties)
        => new()
        {
            Type = type,
            ApiVersion = apiVersion,
            Properties = properties,
            Identifiers = GetIdentifiers(properties),
        };
}
