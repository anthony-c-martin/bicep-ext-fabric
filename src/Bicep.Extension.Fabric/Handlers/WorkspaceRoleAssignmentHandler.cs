using CoreModels = Microsoft.Fabric.Api.Core.Models;

namespace Bicep.Extension.Fabric.Handlers;

public sealed class WorkspaceRoleAssignmentHandler : FabricResourceHandlerBase<WorkspaceRoleAssignment, WorkspaceRoleAssignmentIdentifiers>
{
    protected override Task<ResourceResponse> Preview(ResourceRequest request, CancellationToken cancellationToken)
        => Task.FromResult(GetResponse(request));

    protected override Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            var workspaceId = ParseId(request.Properties.WorkspaceId, "workspaceId");
            CoreModels.WorkspaceRoleAssignment result;

            if (request.Properties.Id is { } assignmentIdValue)
            {
                var assignmentId = ParseId(assignmentIdValue, "id");
                result = (await client.Core.Workspaces.UpdateWorkspaceRoleAssignmentAsync(
                    workspaceId,
                    assignmentId,
                    new CoreModels.UpdateWorkspaceRoleAssignmentRequest(ToSdkRole(request.Properties.Role)),
                    cancellationToken)).Value;
            }
            else
            {
                var create = new CoreModels.AddWorkspaceRoleAssignmentRequest(
                    ToSdkPrincipal(request.Properties.Principal),
                    ToSdkRole(request.Properties.Role));

                result = (await client.Core.Workspaces.AddWorkspaceRoleAssignmentAsync(workspaceId, create, cancellationToken)).Value;
            }

            return BuildResponse(request.Type, request.ApiVersion, ToProperties(result, workspaceId));
        });

    protected override Task<ResourceResponse> Get(ReferenceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            var workspaceId = ParseId(request.Identifiers.WorkspaceId, "workspaceId");
            var result = (await client.Core.Workspaces.GetWorkspaceRoleAssignmentAsync(
                workspaceId,
                RequireId(request.Identifiers.Id),
                cancellationToken)).Value;

            return BuildResponse(request.Type, request.ApiVersion, ToProperties(result, workspaceId));
        });

    protected override Task<ResourceResponse> Delete(ReferenceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            await client.Core.Workspaces.DeleteWorkspaceRoleAssignmentAsync(
                ParseId(request.Identifiers.WorkspaceId, "workspaceId"),
                RequireId(request.Identifiers.Id),
                cancellationToken);

            return GetResponse(request, properties: null);
        });

    protected override WorkspaceRoleAssignmentIdentifiers GetIdentifiers(WorkspaceRoleAssignment properties)
        => new() { WorkspaceId = properties.WorkspaceId, Id = properties.Id };

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

    private static CoreModels.WorkspaceRole ToSdkRole(WorkspaceRole role)
        => role switch
        {
            WorkspaceRole.Admin => CoreModels.WorkspaceRole.Admin,
            WorkspaceRole.Contributor => CoreModels.WorkspaceRole.Contributor,
            WorkspaceRole.Member => CoreModels.WorkspaceRole.Member,
            WorkspaceRole.Viewer => CoreModels.WorkspaceRole.Viewer,
            _ => throw new ResourceErrorException("InvalidProperty", $"Unsupported workspace role '{role}'.", "role"),
        };

    private static WorkspaceRole ToModelRole(CoreModels.WorkspaceRole role)
        => role.ToString() switch
        {
            nameof(WorkspaceRole.Admin) => WorkspaceRole.Admin,
            nameof(WorkspaceRole.Contributor) => WorkspaceRole.Contributor,
            nameof(WorkspaceRole.Member) => WorkspaceRole.Member,
            nameof(WorkspaceRole.Viewer) => WorkspaceRole.Viewer,
            var other => throw new ResourceErrorException("InvalidResponse", $"Unsupported workspace role '{other}' returned by the Fabric API."),
        };

    private static WorkspaceRoleAssignment ToProperties(CoreModels.WorkspaceRoleAssignment assignment, Guid workspaceId)
        => new()
        {
            WorkspaceId = workspaceId.ToString(),
            Id = assignment.Id.ToString(),
            Principal = ToModelPrincipal(assignment.Principal),
            Role = ToModelRole(assignment.Role),
        };

    private static ResourceResponse BuildResponse(string type, string? apiVersion, WorkspaceRoleAssignment properties)
        => new()
        {
            Type = type,
            ApiVersion = apiVersion,
            Properties = properties,
            Identifiers = new WorkspaceRoleAssignmentIdentifiers { WorkspaceId = properties.WorkspaceId, Id = properties.Id },
        };

    private static Guid RequireId(string? id)
        => id is null
            ? throw new ResourceErrorException("MissingIdentifier", "The Workspace Role Assignment ID is required for this operation.")
            : ParseId(id, "id");

    private static Guid ParseId(string value, string target)
        => Guid.TryParse(value, out var result)
            ? result
            : throw new ResourceErrorException("InvalidIdentifier", $"'{value}' is not a valid GUID.", target);
}
