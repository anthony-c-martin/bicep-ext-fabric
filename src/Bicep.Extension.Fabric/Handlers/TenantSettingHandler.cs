using AdminModels = Microsoft.Fabric.Api.Admin.Models;

namespace Bicep.Extension.Fabric.Handlers;

// There is no Fabric API to create/delete a tenant setting (they always exist tenant-wide); this
// resource always calls UpdateTenantSetting, and Get lists all settings and filters by name since
// there is no dedicated get-by-name operation.
public sealed class TenantSettingHandler : FabricResourceHandlerBase<TenantSetting, TenantSettingIdentifiers>
{
    protected override Task<ResourceResponse> Preview(ResourceRequest request, CancellationToken cancellationToken)
        => Task.FromResult(GetResponse(request));

    protected override Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            var settingName = request.Properties.SettingName;
            var update = ToSdkUpdateRequest(request.Properties);

            var response = (await client.Admin.Tenants.UpdateTenantSettingAsync(settingName, update, cancellationToken)).Value;
            var result = response.TenantSettings.FirstOrDefault(setting => setting.SettingName == settingName)
                ?? throw new ResourceErrorException("NotFound", $"Tenant setting '{settingName}' was not found in the update response.", "settingName");

            return BuildResponse(request.Type, request.ApiVersion, ToProperties(result, request.Properties.DeleteBehaviour));
        });

    protected override Task<ResourceResponse> Get(ReferenceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            var settingName = request.Identifiers.SettingName;
            var result = await FindSettingAsync(client, settingName, cancellationToken)
                ?? throw new ResourceErrorException("NotFound", $"Tenant setting '{settingName}' was not found.", "settingName");

            return BuildResponse(request.Type, request.ApiVersion, ToProperties(result, deleteBehaviour: null));
        });

    protected override Task<ResourceResponse> Delete(ReferenceRequest request, CancellationToken cancellationToken)
        // Delete only receives identifiers (not the full properties), so deleteBehaviour - captured
        // only on Properties - cannot be read here; this handler always leaves the tenant setting
        // unchanged on delete (matching Terraform's NoChange default). See deleteBehaviour's
        // description on the TenantSetting model for this known limitation.
        => Task.FromResult(GetResponse(request, properties: null));

    protected override TenantSettingIdentifiers GetIdentifiers(TenantSetting properties)
        => new() { SettingName = properties.SettingName };

    private static async Task<AdminModels.TenantSetting?> FindSettingAsync(Microsoft.Fabric.Api.FabricClient client, string settingName, CancellationToken cancellationToken)
    {
        await foreach (var setting in client.Admin.Tenants.ListTenantSettingsAsync(cancellationToken: cancellationToken))
        {
            if (setting.SettingName == settingName)
            {
                return setting;
            }
        }

        return null;
    }

    private static AdminModels.UpdateTenantSettingRequest ToSdkUpdateRequest(TenantSetting properties)
    {
        var request = new AdminModels.UpdateTenantSettingRequest(properties.Enabled)
        {
            DelegateToCapacity = properties.DelegateToCapacity,
            DelegateToDomain = properties.DelegateToDomain,
            DelegateToWorkspace = properties.DelegateToWorkspace,
        };

        foreach (var group in properties.EnabledSecurityGroups ?? [])
        {
            request.EnabledSecurityGroups.Add(new AdminModels.TenantSettingSecurityGroup(group.GraphId, group.Name ?? string.Empty));
        }

        foreach (var group in properties.ExcludedSecurityGroups ?? [])
        {
            request.ExcludedSecurityGroups.Add(new AdminModels.TenantSettingSecurityGroup(group.GraphId, group.Name ?? string.Empty));
        }

        foreach (var property in properties.Properties ?? [])
        {
            request.Properties.Add(new AdminModels.TenantSettingProperty
            {
                Name = property.Name,
                Value = property.Value,
                Type = property.Type is { } type ? ToSdkPropertyType(type) : null,
            });
        }

        return request;
    }

    private static AdminModels.TenantSettingPropertyType ToSdkPropertyType(TenantSettingPropertyType value)
        => value switch
        {
            TenantSettingPropertyType.Boolean => AdminModels.TenantSettingPropertyType.Boolean,
            TenantSettingPropertyType.FreeText => AdminModels.TenantSettingPropertyType.FreeText,
            TenantSettingPropertyType.Integer => AdminModels.TenantSettingPropertyType.Integer,
            TenantSettingPropertyType.MailEnabledSecurityGroup => AdminModels.TenantSettingPropertyType.MailEnabledSecurityGroup,
            TenantSettingPropertyType.Url => AdminModels.TenantSettingPropertyType.Url,
            _ => throw new ResourceErrorException("InvalidProperty", $"Unsupported tenant setting property type '{value}'.", "properties"),
        };

    private static TenantSettingPropertyType ToModelPropertyType(AdminModels.TenantSettingPropertyType value)
        => value.ToString() switch
        {
            nameof(TenantSettingPropertyType.Boolean) => TenantSettingPropertyType.Boolean,
            nameof(TenantSettingPropertyType.FreeText) => TenantSettingPropertyType.FreeText,
            nameof(TenantSettingPropertyType.Integer) => TenantSettingPropertyType.Integer,
            nameof(TenantSettingPropertyType.MailEnabledSecurityGroup) => TenantSettingPropertyType.MailEnabledSecurityGroup,
            nameof(TenantSettingPropertyType.Url) => TenantSettingPropertyType.Url,
            var other => throw new ResourceErrorException("InvalidResponse", $"Unsupported tenant setting property type '{other}' returned by the Fabric API."),
        };

    private static TenantSetting ToProperties(AdminModels.TenantSetting setting, TenantSettingDeleteBehaviour? deleteBehaviour)
        => new()
        {
            SettingName = setting.SettingName,
            Enabled = setting.Enabled,
            DelegateToCapacity = setting.DelegateToCapacity,
            DelegateToDomain = setting.DelegateToDomain,
            DelegateToWorkspace = setting.DelegateToWorkspace,
            DeleteBehaviour = deleteBehaviour,
            EnabledSecurityGroups = setting.EnabledSecurityGroups.Count > 0
                ? [.. setting.EnabledSecurityGroups.Select(ToModelSecurityGroup)]
                : null,
            ExcludedSecurityGroups = setting.ExcludedSecurityGroups.Count > 0
                ? [.. setting.ExcludedSecurityGroups.Select(ToModelSecurityGroup)]
                : null,
            Properties = setting.Properties.Count > 0
                ? [.. setting.Properties.Select(ToModelProperty)]
                : null,
            CanSpecifySecurityGroups = setting.CanSpecifySecurityGroups,
            TenantSettingGroup = setting.TenantSettingGroup,
            Title = setting.Title,
        };

    private static TenantSettingSecurityGroup ToModelSecurityGroup(AdminModels.TenantSettingSecurityGroup group)
        => new() { GraphId = group.GraphId, Name = group.Name };

    private static TenantSettingProperty ToModelProperty(AdminModels.TenantSettingProperty property)
        => new() { Name = property.Name, Value = property.Value, Type = property.Type is { } type ? ToModelPropertyType(type) : null };

    private static ResourceResponse BuildResponse(string type, string? apiVersion, TenantSetting properties)
        => new()
        {
            Type = type,
            ApiVersion = apiVersion,
            Properties = properties,
            Identifiers = new TenantSettingIdentifiers { SettingName = properties.SettingName },
        };
}
