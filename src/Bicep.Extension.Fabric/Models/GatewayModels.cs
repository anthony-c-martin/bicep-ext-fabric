using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace Bicep.Extension.Fabric.Models;

// Only VirtualNetwork gateways can be created through the Fabric API; the on-premises types require a
// gateway installation, so they can be referenced and read but not provisioned. This matches the
// Terraform provider, whose gateway resource likewise only accepts VirtualNetwork.
public enum GatewayType
{
    VirtualNetwork,
    OnPremises,
    OnPremisesPersonal,
}

public enum GatewayLoadBalancingSetting
{
    Failover,
    DistributeEvenly,
}

public class GatewayVirtualNetworkAzureResource
{
    [TypeProperty("The subscription ID", ObjectTypePropertyFlags.Required)]
    public required string SubscriptionId { get; set; }

    [TypeProperty("The resource group name", ObjectTypePropertyFlags.Required)]
    public required string ResourceGroupName { get; set; }

    [TypeProperty("The virtual network name", ObjectTypePropertyFlags.Required)]
    public required string VirtualNetworkName { get; set; }

    [TypeProperty("The subnet name", ObjectTypePropertyFlags.Required)]
    public required string SubnetName { get; set; }
}

public class GatewayPublicKey
{
    [TypeProperty("The public key exponent", ObjectTypePropertyFlags.ReadOnly)]
    public string? Exponent { get; set; }

    [TypeProperty("The public key modulus", ObjectTypePropertyFlags.ReadOnly)]
    public string? Modulus { get; set; }
}

public class GatewayIdentifiers
{
    [TypeProperty("The Gateway ID", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.ReadOnly)]
    public string? Id { get; set; }
}

[ResourceType("Gateway")]
public class Gateway : GatewayIdentifiers
{
    [TypeProperty("The Gateway type. Only 'VirtualNetwork' gateways can be created; the on-premises types can only be referenced.", ObjectTypePropertyFlags.Required)]
    public required GatewayType Type { get; set; }

    [TypeProperty("The Gateway display name. Required when 'type' is 'VirtualNetwork' or 'OnPremises'.")]
    public string? DisplayName { get; set; }

    [TypeProperty("The capacity ID. Required when 'type' is 'VirtualNetwork'.")]
    public string? CapacityId { get; set; }

    [TypeProperty("The Azure virtual network resource. Required when 'type' is 'VirtualNetwork'.")]
    public GatewayVirtualNetworkAzureResource? VirtualNetworkAzureResource { get; set; }

    [TypeProperty("The inactivity minutes before sleep. Must be one of: 30, 60, 90, 120, 150, 240, 360, 480, 720, 1440. Required when 'type' is 'VirtualNetwork'.")]
    public int? InactivityMinutesBeforeSleep { get; set; }

    [TypeProperty("The number of member gateways. Mutually exclusive with minMemberGatewayCount/maxMemberGatewayCount")]
    public int? NumberOfMemberGateways { get; set; }

    [TypeProperty("The minimum number of member gateways to scale down to. Must be set together with maxMemberGatewayCount")]
    public int? MinMemberGatewayCount { get; set; }

    [TypeProperty("The maximum number of member gateways to scale up to. Must be set together with minMemberGatewayCount")]
    public int? MaxMemberGatewayCount { get; set; }

    [TypeProperty("Whether the on-premises Gateway allows cloud connections to refresh through it", ObjectTypePropertyFlags.ReadOnly)]
    public bool? AllowCloudConnectionRefresh { get; set; }

    [TypeProperty("Whether the on-premises Gateway allows custom connectors", ObjectTypePropertyFlags.ReadOnly)]
    public bool? AllowCustomConnectors { get; set; }

    [TypeProperty("The load balancing setting of the on-premises Gateway", ObjectTypePropertyFlags.ReadOnly)]
    public GatewayLoadBalancingSetting? LoadBalancingSetting { get; set; }

    [TypeProperty("The public key of the primary gateway member, used to encrypt connection credentials", ObjectTypePropertyFlags.ReadOnly)]
    public GatewayPublicKey? PublicKey { get; set; }

    [TypeProperty("The Gateway version", ObjectTypePropertyFlags.ReadOnly)]
    public string? Version { get; set; }
}
