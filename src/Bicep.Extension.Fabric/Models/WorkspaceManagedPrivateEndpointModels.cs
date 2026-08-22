using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace Bicep.Extension.Fabric.Models;

public enum PrivateEndpointProvisioningState
{
    Provisioning,
    Succeeded,
    Updating,
    Deleting,
    Failed,
}

public enum ManagedPrivateEndpointConnectionStatus
{
    Pending,
    Approved,
    Rejected,
    Disconnected,
}

public class PrivateEndpointConnectionState
{
    [TypeProperty("The status of the private endpoint connection", ObjectTypePropertyFlags.ReadOnly)]
    public ManagedPrivateEndpointConnectionStatus? Status { get; set; }

    [TypeProperty("The description provided when approving or rejecting the connection", ObjectTypePropertyFlags.ReadOnly)]
    public string? Description { get; set; }

    [TypeProperty("Any actions required to establish the connection", ObjectTypePropertyFlags.ReadOnly)]
    public string? ActionsRequired { get; set; }
}

public class WorkspaceManagedPrivateEndpointIdentifiers
{
    [TypeProperty("The containing Fabric workspace ID", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public string WorkspaceId { get; set; } = string.Empty;

    [TypeProperty("The Managed Private Endpoint ID", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.ReadOnly)]
    public string? Id { get; set; }
}

// The Fabric API does not support updating a Managed Private Endpoint; any property change requires deleting and recreating it.
[ResourceType("WorkspaceManagedPrivateEndpoint")]
public class WorkspaceManagedPrivateEndpoint : WorkspaceManagedPrivateEndpointIdentifiers
{
    [TypeProperty("The Managed Private Endpoint name", ObjectTypePropertyFlags.Required)]
    public required string Name { get; set; }

    [TypeProperty("The Azure resource ID of the data source for which the private endpoint is created", ObjectTypePropertyFlags.Required)]
    public required string TargetPrivateLinkResourceId { get; set; }

    [TypeProperty("The private-link sub-resource to target. Leave unset when targeting a Private Link Service")]
    public string? TargetSubresourceType { get; set; }

    // Not returned by the Fabric API on read, so it is only honored at creation time.
    [TypeProperty("The request message to send with the private endpoint connection request", ObjectTypePropertyFlags.Required)]
    public string? RequestMessage { get; set; }

    [TypeProperty("The provisioning state of the endpoint", ObjectTypePropertyFlags.ReadOnly)]
    public PrivateEndpointProvisioningState? ProvisioningState { get; set; }

    [TypeProperty("The endpoint connection state of the provisioned endpoint", ObjectTypePropertyFlags.ReadOnly)]
    public PrivateEndpointConnectionState? ConnectionState { get; set; }
}
