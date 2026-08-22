using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace Bicep.Extension.Fabric.Models;

public enum TenantSettingPropertyType
{
    Boolean,
    FreeText,
    Integer,
    MailEnabledSecurityGroup,
    Url,
}

// There is no Fabric API to delete/reset a tenant setting; DeleteBehaviour controls what this
// handler does locally when the resource is removed from a Bicep deployment (mirroring the
// Terraform provider's `delete_behaviour` field, which is also not sent to the API).
public enum TenantSettingDeleteBehaviour
{
    NoChange,
    Disable,
}

public class TenantSettingSecurityGroup
{
    [TypeProperty("The graph ID of the security group", ObjectTypePropertyFlags.Required)]
    public required string GraphId { get; set; }

    [TypeProperty("The name of the security group", ObjectTypePropertyFlags.ReadOnly)]
    public string? Name { get; set; }
}

public class TenantSettingProperty
{
    [TypeProperty("The name of the property")]
    public string? Name { get; set; }

    [TypeProperty("The type of the property")]
    public TenantSettingPropertyType? Type { get; set; }

    [TypeProperty("The value of the property")]
    public string? Value { get; set; }
}

public class TenantSettingIdentifiers
{
    [TypeProperty("The name of the tenant setting", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public string SettingName { get; set; } = string.Empty;
}

[ResourceType("TenantSetting")]
public class TenantSetting : TenantSettingIdentifiers
{
    [TypeProperty("The status of the tenant setting", ObjectTypePropertyFlags.Required)]
    public required bool Enabled { get; set; }

    [TypeProperty("Whether the tenant setting can be delegated to a capacity admin")]
    public bool? DelegateToCapacity { get; set; }

    [TypeProperty("Whether the tenant setting can be delegated to a domain admin")]
    public bool? DelegateToDomain { get; set; }

    [TypeProperty("Whether the tenant setting can be delegated to a workspace admin")]
    public bool? DelegateToWorkspace { get; set; }

    [TypeProperty("Whether the tenant setting is disabled when this resource is deleted. Defaults to NoChange. NOTE: not currently enforced - the resource handler framework does not pass properties to Delete, so this value cannot be read at delete time; the setting is always left unchanged")]
    public TenantSettingDeleteBehaviour? DeleteBehaviour { get; set; }

    [TypeProperty("A list of enabled security groups")]
    public TenantSettingSecurityGroup[]? EnabledSecurityGroups { get; set; }

    [TypeProperty("A list of excluded security groups")]
    public TenantSettingSecurityGroup[]? ExcludedSecurityGroups { get; set; }

    [TypeProperty("Tenant setting properties")]
    public TenantSettingProperty[]? Properties { get; set; }

    [TypeProperty("Whether the tenant setting is enabled for a security group rather than the entire organization", ObjectTypePropertyFlags.ReadOnly)]
    public bool? CanSpecifySecurityGroups { get; set; }

    [TypeProperty("The tenant setting group name", ObjectTypePropertyFlags.ReadOnly)]
    public string? TenantSettingGroup { get; set; }

    [TypeProperty("The title of the tenant setting", ObjectTypePropertyFlags.ReadOnly)]
    public string? Title { get; set; }
}
