
namespace Bicep.Extension.Fabric.Handlers;

// Microsoft.Fabric.Api 2.20.0 exposes Domain Get/List (Core) and Delete (Admin), but no Create/Update.
// This handler therefore only supports referencing an existing Domain by `id`; it cannot provision new domains.
public sealed class DomainHandler : FabricResourceHandlerBase<FabricDomain, DomainIdentifiers>
{
    protected override Task<ResourceResponse> Preview(ResourceRequest request, CancellationToken cancellationToken)
        => Task.FromResult(GetResponse(request));

    protected override Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            var result = (await client.Core.Domains.GetDomainAsync(ParseId(request.Properties.Id, "id"), cancellationToken)).Value;

            return BuildResponse(request.Type, request.ApiVersion, ToProperties(result));
        });

    protected override Task<ResourceResponse> Get(ReferenceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            var result = (await client.Core.Domains.GetDomainAsync(ParseId(request.Identifiers.Id, "id"), cancellationToken)).Value;
            return BuildResponse(request.Type, request.ApiVersion, ToProperties(result));
        });

    protected override Task<ResourceResponse> Delete(ReferenceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            await client.Admin.Domains.DeleteDomainAsync(ParseId(request.Identifiers.Id, "id"), cancellationToken);
            return GetResponse(request, properties: null);
        });

    protected override DomainIdentifiers GetIdentifiers(FabricDomain properties)
        => new() { Id = properties.Id };

    private static FabricDomain ToProperties(Microsoft.Fabric.Api.Core.Models.Domain domain)
        => new()
        {
            Id = domain.Id.ToString(),
            DisplayName = domain.DisplayName,
            Description = domain.Description,
            ParentDomainId = domain.ParentDomainId?.ToString(),
        };

    private static ResourceResponse BuildResponse(string type, string? apiVersion, FabricDomain properties)
        => new()
        {
            Type = type,
            ApiVersion = apiVersion,
            Properties = properties,
            Identifiers = new DomainIdentifiers { Id = properties.Id },
        };

    private static Guid ParseId(string value, string target)
        => Guid.TryParse(value, out var result)
            ? result
            : throw new ResourceErrorException("InvalidIdentifier", $"'{value}' is not a valid GUID.", target);
}
