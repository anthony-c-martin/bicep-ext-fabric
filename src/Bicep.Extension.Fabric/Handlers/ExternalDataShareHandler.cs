using CoreModels = Microsoft.Fabric.Api.Core.Models;

namespace Bicep.Extension.Fabric.Handlers;

// External data shares have no update operation (all fields are immutable at creation); CreateOrUpdate
// only ever creates, matching the Terraform provider's ForceNew-only schema for this resource.
public sealed class ExternalDataShareHandler : FabricResourceHandlerBase<ExternalDataShare, ExternalDataShareIdentifiers>
{
    protected override Task<ResourceResponse> Preview(ResourceRequest request, CancellationToken cancellationToken)
        => Task.FromResult(GetResponse(request));

    protected override Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            var workspaceId = ParseId(request.Properties.WorkspaceId, "workspaceId");
            var itemId = ParseId(request.Properties.ItemId, "itemId");

            if (request.Properties.Id is not null)
            {
                var result = (await client.Core.ExternalDataSharesProvider.GetExternalDataShareAsync(
                    workspaceId,
                    itemId,
                    ParseId(request.Properties.Id, "id"),
                    cancellationToken)).Value;

                return BuildResponse(request.Type, request.ApiVersion, ToProperties(result, workspaceId, itemId));
            }

            var create = new CoreModels.CreateExternalDataShareRequest(request.Properties.Paths, ToSdkRecipient(request.Properties.Recipient));
            var created = (await client.Core.ExternalDataSharesProvider.CreateExternalDataShareAsync(workspaceId, itemId, create, cancellationToken)).Value;

            return BuildResponse(request.Type, request.ApiVersion, ToProperties(created, workspaceId, itemId));
        });

    protected override Task<ResourceResponse> Get(ReferenceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            var workspaceId = ParseId(request.Identifiers.WorkspaceId, "workspaceId");
            var itemId = ParseId(request.Identifiers.ItemId, "itemId");

            var result = (await client.Core.ExternalDataSharesProvider.GetExternalDataShareAsync(
                workspaceId,
                itemId,
                RequireId(request.Identifiers.Id),
                cancellationToken)).Value;

            return BuildResponse(request.Type, request.ApiVersion, ToProperties(result, workspaceId, itemId));
        });

    protected override Task<ResourceResponse> Delete(ReferenceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            await client.Core.ExternalDataSharesProvider.DeleteExternalDataShareAsync(
                ParseId(request.Identifiers.WorkspaceId, "workspaceId"),
                ParseId(request.Identifiers.ItemId, "itemId"),
                RequireId(request.Identifiers.Id),
                cancellationToken);

            return GetResponse(request, properties: null);
        });

    protected override ExternalDataShareIdentifiers GetIdentifiers(ExternalDataShare properties)
        => new() { WorkspaceId = properties.WorkspaceId, ItemId = properties.ItemId, Id = properties.Id };

    private static CoreModels.BaseExternalDataShareRecipient ToSdkRecipient(ExternalDataShareRecipient recipient)
        => (recipient.Type ?? ExternalDataShareRecipientType.User) switch
        {
            ExternalDataShareRecipientType.User => new CoreModels.ExternalDataShareUserRecipient(
                recipient.UserPrincipalName ?? throw new ResourceErrorException("MissingProperty", "recipient.userPrincipalName is required when recipient.type is User.", "recipient.userPrincipalName"))
            {
                TenantId = ParseOptionalId(recipient.TenantId, "recipient.tenantId"),
            },
            // The Fabric SDK's ServicePrincipal recipient requires a service principal object ID
            // (principalId), but the Terraform provider's schema for this resource only exposes
            // tenant_id/user_principal_name - it has no field for the SP's object ID. This is a gap
            // versus Terraform: ServicePrincipal recipients cannot be created through this resource
            // until the Fabric API/SDK exposes (or Terraform documents) how the principal ID is derived.
            ExternalDataShareRecipientType.ServicePrincipal => throw new ResourceErrorException(
                "NotSupported",
                "recipient.type 'ServicePrincipal' is not currently supported: the Fabric SDK requires a service principal object ID that this resource's schema (matching the Terraform provider) does not expose.",
                "recipient.type"),
            _ => throw new ResourceErrorException("InvalidProperty", $"Unsupported recipient type '{recipient.Type}'.", "recipient.type"),
        };

    private static ExternalDataShare ToProperties(CoreModels.ExternalDataShare share, Guid workspaceId, Guid itemId)
        => new()
        {
            WorkspaceId = workspaceId.ToString(),
            ItemId = itemId.ToString(),
            Id = share.Id.ToString(),
            Paths = [.. share.Paths],
            Recipient = ToModelRecipient(share.Recipient),
            AcceptedByTenantId = share.AcceptedByTenantId?.ToString(),
            ExpirationTime = share.ExpirationTimeUtc?.ToString("o"),
            InvitationUrl = share.InvitationUrl?.ToString(),
            PrincipalModel = new ExternalDataSharePrincipal { Id = share.CreatorPrincipal.Id.ToString(), Type = share.CreatorPrincipal.GetType().Name },
            Status = ToModelStatus(share.Status),
        };

    private static ExternalDataShareRecipient ToModelRecipient(CoreModels.BaseExternalDataShareRecipient recipient)
        => recipient switch
        {
            CoreModels.ExternalDataShareSPRecipient sp => new ExternalDataShareRecipient { Type = ExternalDataShareRecipientType.ServicePrincipal, TenantId = sp.TenantId.ToString() },
            CoreModels.ExternalDataShareUserRecipient user => new ExternalDataShareRecipient { Type = ExternalDataShareRecipientType.User, UserPrincipalName = user.UserPrincipalName, TenantId = user.TenantId?.ToString() },
            _ => throw new ResourceErrorException("InvalidResponse", $"Unsupported recipient type '{recipient.GetType().Name}' returned by the Fabric API."),
        };

    private static ExternalDataShareStatus ToModelStatus(CoreModels.ExternalDataShareStatus status)
        => status.ToString() switch
        {
            nameof(ExternalDataShareStatus.Active) => ExternalDataShareStatus.Active,
            nameof(ExternalDataShareStatus.InvitationExpired) => ExternalDataShareStatus.InvitationExpired,
            nameof(ExternalDataShareStatus.Pending) => ExternalDataShareStatus.Pending,
            nameof(ExternalDataShareStatus.Revoked) => ExternalDataShareStatus.Revoked,
            var other => throw new ResourceErrorException("InvalidResponse", $"Unsupported external data share status '{other}' returned by the Fabric API."),
        };

    private static ResourceResponse BuildResponse(string type, string? apiVersion, ExternalDataShare properties)
        => new()
        {
            Type = type,
            ApiVersion = apiVersion,
            Properties = properties,
            Identifiers = new ExternalDataShareIdentifiers { WorkspaceId = properties.WorkspaceId, ItemId = properties.ItemId, Id = properties.Id },
        };

    private static Guid RequireId(string? id)
        => id is null
            ? throw new ResourceErrorException("MissingIdentifier", "The External Data Share ID is required for this operation.")
            : ParseId(id, "id");

    private static Guid ParseId(string value, string target)
        => Guid.TryParse(value, out var result)
            ? result
            : throw new ResourceErrorException("InvalidIdentifier", $"'{value}' is not a valid GUID.", target);

    private static Guid? ParseOptionalId(string? value, string target)
        => value is null ? null : ParseId(value, target);
}
