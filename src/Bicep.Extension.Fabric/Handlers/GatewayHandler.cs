using CoreModels = Microsoft.Fabric.Api.Core.Models;

namespace Bicep.Extension.Fabric.Handlers;

public sealed class GatewayHandler : FabricResourceHandlerBase<Gateway, GatewayIdentifiers>
{
    protected override Task<ResourceResponse> Preview(ResourceRequest request, CancellationToken cancellationToken)
        => Task.FromResult(GetResponse(request));

    protected override Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
    {
        // Validated before the Fabric client is created, because this depends only on the input.
        if (request.Properties.Type is not GatewayType.VirtualNetwork)
        {
            throw new ResourceErrorException(
                "GatewayTypeNotProvisionable",
                $"The Fabric API does not support creating '{request.Properties.Type}' gateways, because they require an on-premises data gateway installation. Reference an existing gateway with an 'existing' resource instead.",
                "type");
        }

        return HandleRequest(request, async client =>
        {
            CoreModels.Gateway result;

            var displayName = Require(request.Properties.DisplayName, "displayName");
            var capacityId = ParseGuid(Require(request.Properties.CapacityId, "capacityId"), "capacityId");
            var inactivityMinutesBeforeSleep = request.Properties.InactivityMinutesBeforeSleep
                ?? throw new ResourceErrorException("MissingProperty", "'inactivityMinutesBeforeSleep' is required for a VirtualNetwork gateway.", "inactivityMinutesBeforeSleep");

            if (request.Properties.Id is { } idValue)
            {
                var gatewayId = ParseGuid(idValue, "id");
                var update = new CoreModels.UpdateVirtualNetworkGatewayRequest
                {
                    DisplayName = displayName,
                    CapacityId = capacityId,
                    InactivityMinutesBeforeSleep = inactivityMinutesBeforeSleep,
                    NumberOfMemberGateways = request.Properties.NumberOfMemberGateways,
                    MinMemberGatewayCount = request.Properties.MinMemberGatewayCount,
                    MaxMemberGatewayCount = request.Properties.MaxMemberGatewayCount,
                };

                result = (await client.Core.Gateways.UpdateGatewayAsync(gatewayId, update, cancellationToken)).Value;
            }
            else
            {
                var virtualNetworkAzureResource = request.Properties.VirtualNetworkAzureResource
                    ?? throw new ResourceErrorException("MissingProperty", "'virtualNetworkAzureResource' is required for a VirtualNetwork gateway.", "virtualNetworkAzureResource");

                var create = new CoreModels.CreateVirtualNetworkGatewayRequest(
                    displayName,
                    capacityId,
                    ToSdkVirtualNetworkAzureResource(virtualNetworkAzureResource),
                    inactivityMinutesBeforeSleep)
                {
                    NumberOfMemberGateways = request.Properties.NumberOfMemberGateways,
                    MinMemberGatewayCount = request.Properties.MinMemberGatewayCount,
                    MaxMemberGatewayCount = request.Properties.MaxMemberGatewayCount,
                };

                result = (await client.Core.Gateways.CreateGatewayAsync(create, cancellationToken)).Value;
            }

            return BuildResponse(request.Type, request.ApiVersion, ToProperties(result));
        });
    }

    protected override Task<ResourceResponse> Get(ReferenceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            var result = (await client.Core.Gateways.GetGatewayAsync(RequireGuid(request.Identifiers.Id, "Gateway ID"), cancellationToken)).Value;
            return BuildResponse(request.Type, request.ApiVersion, ToProperties(result));
        });

    protected override Task<ResourceResponse> Delete(ReferenceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            await client.Core.Gateways.DeleteGatewayAsync(RequireGuid(request.Identifiers.Id, "Gateway ID"), cancellationToken);
            return GetResponse(request, properties: null);
        });

    protected override GatewayIdentifiers GetIdentifiers(Gateway properties)
        => new() { Id = properties.Id };

    private static string Require(string? value, string target)
        => value ?? throw new ResourceErrorException("MissingProperty", $"'{target}' is required for a VirtualNetwork gateway.", target);

    private static CoreModels.VirtualNetworkAzureResource ToSdkVirtualNetworkAzureResource(GatewayVirtualNetworkAzureResource resource)
        => new(ParseGuid(resource.SubscriptionId, "virtualNetworkAzureResource.subscriptionId"), resource.ResourceGroupName, resource.VirtualNetworkName, resource.SubnetName);

    private static GatewayVirtualNetworkAzureResource ToModelVirtualNetworkAzureResource(CoreModels.VirtualNetworkAzureResource resource)
        => new()
        {
            SubscriptionId = resource.SubscriptionId.ToString(),
            ResourceGroupName = resource.ResourceGroupName,
            VirtualNetworkName = resource.VirtualNetworkName,
            SubnetName = resource.SubnetName,
        };

    private static GatewayPublicKey ToModelPublicKey(CoreModels.PublicKey publicKey)
        => new()
        {
            Exponent = publicKey.Exponent,
            Modulus = publicKey.Modulus,
        };

    private static Gateway ToProperties(CoreModels.Gateway gateway)
        => gateway switch
        {
            CoreModels.VirtualNetworkGateway virtualNetworkGateway => new Gateway
            {
                Id = virtualNetworkGateway.Id.ToString(),
                Type = GatewayType.VirtualNetwork,
                DisplayName = virtualNetworkGateway.DisplayName,
                CapacityId = virtualNetworkGateway.CapacityId?.ToString(),
                VirtualNetworkAzureResource = ToModelVirtualNetworkAzureResource(virtualNetworkGateway.VirtualNetworkAzureResource),
                InactivityMinutesBeforeSleep = virtualNetworkGateway.InactivityMinutesBeforeSleep,
                NumberOfMemberGateways = virtualNetworkGateway.NumberOfMemberGateways,
                MinMemberGatewayCount = virtualNetworkGateway.MinMemberGatewayCount,
                MaxMemberGatewayCount = virtualNetworkGateway.MaxMemberGatewayCount,
            },
            CoreModels.OnPremisesGateway onPremisesGateway => new Gateway
            {
                Id = onPremisesGateway.Id.ToString(),
                Type = GatewayType.OnPremises,
                DisplayName = onPremisesGateway.DisplayName,
                NumberOfMemberGateways = onPremisesGateway.NumberOfMemberGateways,
                AllowCloudConnectionRefresh = onPremisesGateway.AllowCloudConnectionRefresh,
                AllowCustomConnectors = onPremisesGateway.AllowCustomConnectors,
                LoadBalancingSetting = Enum.TryParse<GatewayLoadBalancingSetting>(onPremisesGateway.LoadBalancingSetting.ToString(), out var loadBalancingSetting)
                    ? loadBalancingSetting
                    : null,
                PublicKey = ToModelPublicKey(onPremisesGateway.PublicKey),
                Version = onPremisesGateway.Version,
            },
            CoreModels.OnPremisesGatewayPersonal personalGateway => new Gateway
            {
                Id = personalGateway.Id.ToString(),
                Type = GatewayType.OnPremisesPersonal,
                PublicKey = ToModelPublicKey(personalGateway.PublicKey),
                Version = personalGateway.Version,
            },
            _ => throw new ResourceErrorException("InvalidResponse", $"Unsupported gateway type '{gateway.GetType().Name}' returned by the Fabric API."),
        };

    private static ResourceResponse BuildResponse(string type, string? apiVersion, Gateway properties)
        => new()
        {
            Type = type,
            ApiVersion = apiVersion,
            Properties = properties,
            Identifiers = new GatewayIdentifiers { Id = properties.Id },
        };
}
