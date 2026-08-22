using AdminModels = Microsoft.Fabric.Api.Admin.Models;

namespace Bicep.Extension.Fabric.Handlers;

public sealed class DomainWorkspaceAssignmentHandler : FabricResourceHandlerBase<DomainWorkspaceAssignment, DomainWorkspaceAssignmentIdentifiers>
{
    protected override Task<ResourceResponse> Preview(ResourceRequest request, CancellationToken cancellationToken)
        => Task.FromResult(GetResponse(request));

    protected override Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            var domainId = ParseId(request.Properties.DomainId, "domainId");
            var desired = request.Properties.WorkspaceIds.Select(id => ParseId(id, "workspaceIds")).ToHashSet();
            var current = await ListAssignedWorkspaceIdsAsync(client, domainId, cancellationToken);

            var toAdd = desired.Except(current).ToList();
            var toRemove = current.Except(desired).ToList();

            if (toAdd.Count > 0)
            {
                var assignRequest = new AdminModels.AssignDomainWorkspacesByIdsRequest();
                foreach (var id in toAdd)
                {
                    assignRequest.WorkspacesIds.Add(id);
                }

                await client.Admin.Domains.AssignDomainWorkspacesByIdsAsync(domainId, assignRequest, cancellationToken);
            }

            if (toRemove.Count > 0)
            {
                var unassignRequest = new AdminModels.UnassignDomainWorkspacesByIdsRequest();
                foreach (var id in toRemove)
                {
                    unassignRequest.WorkspacesIds.Add(id);
                }

                await client.Admin.Domains.UnassignDomainWorkspacesByIdsAsync(domainId, unassignRequest, cancellationToken);
            }

            return BuildResponse(request.Type, request.ApiVersion, new DomainWorkspaceAssignment
            {
                DomainId = domainId.ToString(),
                WorkspaceIds = [.. desired.Select(id => id.ToString())],
            });
        });

    protected override Task<ResourceResponse> Get(ReferenceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            var domainId = ParseId(request.Identifiers.DomainId, "domainId");
            var current = await ListAssignedWorkspaceIdsAsync(client, domainId, cancellationToken);

            return BuildResponse(request.Type, request.ApiVersion, new DomainWorkspaceAssignment
            {
                DomainId = domainId.ToString(),
                WorkspaceIds = [.. current.Select(id => id.ToString())],
            });
        });

    protected override Task<ResourceResponse> Delete(ReferenceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            await client.Admin.Domains.UnassignAllDomainWorkspacesAsync(ParseId(request.Identifiers.DomainId, "domainId"), cancellationToken);
            return GetResponse(request, properties: null);
        });

    protected override DomainWorkspaceAssignmentIdentifiers GetIdentifiers(DomainWorkspaceAssignment properties)
        => new() { DomainId = properties.DomainId };

    private static async Task<HashSet<Guid>> ListAssignedWorkspaceIdsAsync(
        Microsoft.Fabric.Api.FabricClient client,
        Guid domainId,
        CancellationToken cancellationToken)
    {
        var ids = new HashSet<Guid>();
        await foreach (var workspace in client.Admin.Domains.ListDomainWorkspacesAsync(domainId, cancellationToken: cancellationToken))
        {
            ids.Add(workspace.Id);
        }

        return ids;
    }

    private static ResourceResponse BuildResponse(string type, string? apiVersion, DomainWorkspaceAssignment properties)
        => new()
        {
            Type = type,
            ApiVersion = apiVersion,
            Properties = properties,
            Identifiers = new DomainWorkspaceAssignmentIdentifiers { DomainId = properties.DomainId },
        };

    private static Guid ParseId(string value, string target)
        => Guid.TryParse(value, out var result)
            ? result
            : throw new ResourceErrorException("InvalidIdentifier", $"'{value}' is not a valid GUID.", target);
}
