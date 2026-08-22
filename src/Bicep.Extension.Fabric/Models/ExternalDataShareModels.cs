using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace Bicep.Extension.Fabric.Models;

public enum ExternalDataShareRecipientType
{
    User,
    ServicePrincipal,
}

public enum ExternalDataShareStatus
{
    Active,
    InvitationExpired,
    Pending,
    Revoked,
}

public class ExternalDataShareRecipient
{
    [TypeProperty("The type of the recipient. Defaults to User")]
    public ExternalDataShareRecipientType? Type { get; set; }

    [TypeProperty("The user principal name of the recipient. Required when type is User")]
    public string? UserPrincipalName { get; set; }

    [TypeProperty("The tenant ID of the recipient. Required when type is ServicePrincipal, optional when type is User")]
    public string? TenantId { get; set; }
}

public class ExternalDataSharePrincipal
{
    [TypeProperty("The ID of the creator principal", ObjectTypePropertyFlags.ReadOnly)]
    public string? Id { get; set; }

    [TypeProperty("The type of the creator principal", ObjectTypePropertyFlags.ReadOnly)]
    public string? Type { get; set; }
}

public class ExternalDataShareIdentifiers
{
    [TypeProperty("The Workspace ID", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public string WorkspaceId { get; set; } = string.Empty;

    [TypeProperty("The item ID", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public string ItemId { get; set; } = string.Empty;

    [TypeProperty("The External Data Share ID", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.ReadOnly)]
    public string? Id { get; set; }
}

[ResourceType("ExternalDataShare")]
public class ExternalDataShare : ExternalDataShareIdentifiers
{
    [TypeProperty("The paths to share. Each path must start with 'Files/' or 'Tables/' followed by a subpath - the root folder itself cannot be shared", ObjectTypePropertyFlags.Required)]
    public required string[] Paths { get; set; }

    [TypeProperty("The recipient of the external data share", ObjectTypePropertyFlags.Required)]
    public required ExternalDataShareRecipient Recipient { get; set; }

    [TypeProperty("The tenant ID that accepted the external data share", ObjectTypePropertyFlags.ReadOnly)]
    public string? AcceptedByTenantId { get; set; }

    [TypeProperty("The expiration time of the external data share in UTC", ObjectTypePropertyFlags.ReadOnly)]
    public string? ExpirationTime { get; set; }

    [TypeProperty("The invitation URL for the external data share", ObjectTypePropertyFlags.ReadOnly)]
    public string? InvitationUrl { get; set; }

    [TypeProperty("The creator principal of the external data share", ObjectTypePropertyFlags.ReadOnly)]
    public ExternalDataSharePrincipal? PrincipalModel { get; set; }

    [TypeProperty("The status of the external data share", ObjectTypePropertyFlags.ReadOnly)]
    public ExternalDataShareStatus? Status { get; set; }
}
