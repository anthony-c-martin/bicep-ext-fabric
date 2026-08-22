using CoreModels = Microsoft.Fabric.Api.Core.Models;

namespace Bicep.Extension.Fabric.Handlers;

// The Fabric API deletes deployment pipeline role assignments by principal ID (there is no
// dedicated get/update endpoint), so this handler tracks the assignment's `id` as the principal ID
// and treats principal/role changes as a delete-then-recreate (matching Terraform's ForceNew semantics).
public sealed class DeploymentPipelineRoleAssignmentHandler : FabricResourceHandlerBase<DeploymentPipelineRoleAssignment, DeploymentPipelineRoleAssignmentIdentifiers>
{
    protected override Task<ResourceResponse> Preview(ResourceRequest request, CancellationToken cancellationToken)
        => Task.FromResult(GetResponse(request));

    protected override Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            var pipelineId = ParseId(request.Properties.DeploymentPipelineId, "deploymentPipelineId");

            if (request.Properties.Id is { } principalIdValue && principalIdValue == request.Properties.Principal.Id)
            {
                // Principal/role are immutable; nothing to change server-side if the principal is unchanged.
                var existing = await FindAssignmentAsync(client, pipelineId, ParseId(principalIdValue, "id"), cancellationToken)
                    ?? throw new ResourceErrorException("NotFound", $"Deployment Pipeline Role Assignment '{principalIdValue}' was not found.", "id");

                return BuildResponse(request.Type, request.ApiVersion, ToProperties(existing, pipelineId));
            }

            var create = new CoreModels.AddDeploymentPipelineRoleAssignmentRequest(
                ToSdkPrincipal(request.Properties.Principal),
                ToSdkRole(request.Properties.Role));

            var result = (await client.Core.DeploymentPipelines.AddDeploymentPipelineRoleAssignmentAsync(pipelineId, create, cancellationToken)).Value;

            return BuildResponse(request.Type, request.ApiVersion, ToProperties(result, pipelineId));
        });

    protected override Task<ResourceResponse> Get(ReferenceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            var pipelineId = ParseId(request.Identifiers.DeploymentPipelineId, "deploymentPipelineId");
            var principalId = RequireId(request.Identifiers.Id);

            var assignment = await FindAssignmentAsync(client, pipelineId, principalId, cancellationToken)
                ?? throw new ResourceErrorException("NotFound", $"Deployment Pipeline Role Assignment '{principalId}' was not found.", "id");

            return BuildResponse(request.Type, request.ApiVersion, ToProperties(assignment, pipelineId));
        });

    protected override Task<ResourceResponse> Delete(ReferenceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            await client.Core.DeploymentPipelines.DeleteDeploymentPipelineRoleAssignmentAsync(
                ParseId(request.Identifiers.DeploymentPipelineId, "deploymentPipelineId"),
                RequireId(request.Identifiers.Id),
                cancellationToken);

            return GetResponse(request, properties: null);
        });

    protected override DeploymentPipelineRoleAssignmentIdentifiers GetIdentifiers(DeploymentPipelineRoleAssignment properties)
        => new() { DeploymentPipelineId = properties.DeploymentPipelineId, Id = properties.Id };

    private static async Task<CoreModels.DeploymentPipelineRoleAssignment?> FindAssignmentAsync(
        Microsoft.Fabric.Api.FabricClient client,
        Guid pipelineId,
        Guid principalId,
        CancellationToken cancellationToken)
    {
        await foreach (var assignment in client.Core.DeploymentPipelines.ListDeploymentPipelineRoleAssignmentsAsync(pipelineId, cancellationToken: cancellationToken))
        {
            if (assignment.Principal.Id == principalId)
            {
                return assignment;
            }
        }

        return null;
    }

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

    private static CoreModels.DeploymentPipelineRole ToSdkRole(DeploymentPipelineRole role)
        => role switch
        {
            DeploymentPipelineRole.Admin => CoreModels.DeploymentPipelineRole.Admin,
            _ => throw new ResourceErrorException("InvalidProperty", $"Unsupported deployment pipeline role '{role}'.", "role"),
        };

    private static DeploymentPipelineRole ToModelRole(CoreModels.DeploymentPipelineRole role)
        => role.ToString() switch
        {
            nameof(DeploymentPipelineRole.Admin) => DeploymentPipelineRole.Admin,
            var other => throw new ResourceErrorException("InvalidResponse", $"Unsupported deployment pipeline role '{other}' returned by the Fabric API."),
        };

    private static DeploymentPipelineRoleAssignment ToProperties(CoreModels.DeploymentPipelineRoleAssignment assignment, Guid pipelineId)
        => new()
        {
            DeploymentPipelineId = pipelineId.ToString(),
            Id = assignment.Principal.Id.ToString(),
            Principal = ToModelPrincipal(assignment.Principal),
            Role = ToModelRole(assignment.Role),
        };

    private static ResourceResponse BuildResponse(string type, string? apiVersion, DeploymentPipelineRoleAssignment properties)
        => new()
        {
            Type = type,
            ApiVersion = apiVersion,
            Properties = properties,
            Identifiers = new DeploymentPipelineRoleAssignmentIdentifiers { DeploymentPipelineId = properties.DeploymentPipelineId, Id = properties.Id },
        };

    private static Guid RequireId(string? id)
        => id is null
            ? throw new ResourceErrorException("MissingIdentifier", "The Deployment Pipeline Role Assignment ID is required for this operation.")
            : ParseId(id, "id");

    private static Guid ParseId(string value, string target)
        => Guid.TryParse(value, out var result)
            ? result
            : throw new ResourceErrorException("InvalidIdentifier", $"'{value}' is not a valid GUID.", target);
}
