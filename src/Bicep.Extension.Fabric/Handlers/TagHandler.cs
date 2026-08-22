using AdminModels = Microsoft.Fabric.Api.Admin.Models;

namespace Bicep.Extension.Fabric.Handlers;

public sealed class TagHandler : FabricResourceHandlerBase<Tag, TagIdentifiers>
{
    protected override Task<ResourceResponse> Preview(ResourceRequest request, CancellationToken cancellationToken)
        => Task.FromResult(GetResponse(request));

    protected override Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            AdminModels.Tag result;

            if (request.Properties.Id is { } tagIdValue)
            {
                var tagId = ParseId(tagIdValue, "id");
                result = (await client.Admin.Tags.UpdateTagAsync(
                    tagId,
                    new AdminModels.UpdateTagRequest(request.Properties.DisplayName),
                    cancellationToken)).Value;
            }
            else
            {
                var create = new AdminModels.CreateTagsRequest([new AdminModels.CreateTagRequest(request.Properties.DisplayName)])
                {
                    Scope = ToSdkScope(request.Properties.Scope),
                };

                var response = (await client.Admin.Tags.BulkCreateTagsAsync(create, cancellationToken)).Value;
                result = response.Tags.Single();
            }

            return BuildResponse(request.Type, request.ApiVersion, ToProperties(result));
        });

    protected override Task<ResourceResponse> Get(ReferenceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            var tagId = RequireId(request.Identifiers.Id);

            await foreach (var tag in client.Admin.Tags.ListTagsAsync(cancellationToken: cancellationToken))
            {
                if (tag.Id == tagId)
                {
                    return BuildResponse(request.Type, request.ApiVersion, ToProperties(tag));
                }
            }

            throw new ResourceErrorException("NotFound", $"Tag '{tagId}' was not found.", "id");
        });

    protected override Task<ResourceResponse> Delete(ReferenceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            await client.Admin.Tags.DeleteTagAsync(RequireId(request.Identifiers.Id), cancellationToken);
            return GetResponse(request, properties: null);
        });

    protected override TagIdentifiers GetIdentifiers(Tag properties)
        => new() { Id = properties.Id };

    private static AdminModels.TagScope? ToSdkScope(TagScope? scope)
        => scope switch
        {
            null => null,
            { Type: TagScopeType.Tenant } => new AdminModels.TenantTagScope(),
            { Type: TagScopeType.Domain, DomainId: { } domainId } => new AdminModels.DomainTagScope(ParseId(domainId, "scope.domainId")),
            { Type: TagScopeType.Domain } => throw new ResourceErrorException("MissingProperty", "'scope.domainId' is required when 'scope.type' is 'Domain'.", "scope.domainId"),
            _ => throw new ResourceErrorException("InvalidProperty", $"Unsupported tag scope type '{scope.Type}'.", "scope.type"),
        };

    private static TagScope? ToModelScope(AdminModels.TagScope? scope)
        => scope switch
        {
            null => null,
            AdminModels.DomainTagScope domainScope => new TagScope { Type = TagScopeType.Domain, DomainId = domainScope.DomainId.ToString() },
            AdminModels.TenantTagScope => new TagScope { Type = TagScopeType.Tenant },
            _ => null,
        };

    private static Tag ToProperties(AdminModels.Tag tag)
        => new()
        {
            Id = tag.Id.ToString(),
            DisplayName = tag.DisplayName,
            Scope = ToModelScope(tag.Scope),
        };

    private static Tag ToProperties(AdminModels.AdminTagInfo tag)
        => new()
        {
            Id = tag.Id.ToString(),
            DisplayName = tag.DisplayName,
            Scope = ToModelScope(tag.Scope),
        };

    private static ResourceResponse BuildResponse(string type, string? apiVersion, Tag properties)
        => new()
        {
            Type = type,
            ApiVersion = apiVersion,
            Properties = properties,
            Identifiers = new TagIdentifiers { Id = properties.Id },
        };

    private static Guid RequireId(string? id)
        => id is null
            ? throw new ResourceErrorException("MissingIdentifier", "The Tag ID is required for this operation.")
            : ParseId(id, "id");

    private static Guid ParseId(string value, string target)
        => Guid.TryParse(value, out var result)
            ? result
            : throw new ResourceErrorException("InvalidIdentifier", $"'{value}' is not a valid GUID.", target);
}
