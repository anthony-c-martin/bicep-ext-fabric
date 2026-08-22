using WarehouseModels = Microsoft.Fabric.Api.Warehouse.Models;

namespace Bicep.Extension.Fabric.Handlers;

public sealed class WarehouseSqlAuditSettingsHandler : FabricResourceHandlerBase<WarehouseSqlAuditSettings, WarehouseSqlAuditSettingsIdentifiers>
{
    protected override Task<ResourceResponse> Preview(ResourceRequest request, CancellationToken cancellationToken)
        => Task.FromResult(GetResponse(request));

    protected override Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            var workspaceId = ParseId(request.Properties.WorkspaceId, "workspaceId");
            var warehouseId = ParseId(request.Properties.WarehouseId, "warehouseId");

            var update = new WarehouseModels.SqlAuditSettingsUpdate
            {
                State = ToSdkState(request.Properties.State ?? AuditSettingsState.Disabled),
                RetentionDays = request.Properties.RetentionDays ?? 0,
            };

            await client.Warehouse.SQLAuditSettings.UpdateSQLAuditSettingsAsync(workspaceId, warehouseId, update, cancellationToken);

            if (request.Properties.AuditActionsAndGroups is { } auditActionsAndGroups)
            {
                await client.Warehouse.SQLAuditSettings.SetAuditActionsAndGroupsAsync(workspaceId, warehouseId, auditActionsAndGroups, cancellationToken);
            }

            var result = (await client.Warehouse.SQLAuditSettings.GetSQLAuditSettingsAsync(workspaceId, warehouseId, cancellationToken)).Value;

            return BuildResponse(request.Type, request.ApiVersion, ToProperties(result, workspaceId, warehouseId));
        });

    protected override Task<ResourceResponse> Get(ReferenceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            var workspaceId = ParseId(request.Identifiers.WorkspaceId, "workspaceId");
            var warehouseId = ParseId(request.Identifiers.WarehouseId, "warehouseId");

            var result = (await client.Warehouse.SQLAuditSettings.GetSQLAuditSettingsAsync(workspaceId, warehouseId, cancellationToken)).Value;

            return BuildResponse(request.Type, request.ApiVersion, ToProperties(result, workspaceId, warehouseId));
        });

    protected override Task<ResourceResponse> Delete(ReferenceRequest request, CancellationToken cancellationToken)
        // The Fabric API has no delete operation for warehouse SQL audit settings; they remain unchanged in Fabric
        // when this resource is removed from a Bicep deployment, matching the Terraform provider's behavior.
        => Task.FromResult(GetResponse(request, properties: null));

    protected override WarehouseSqlAuditSettingsIdentifiers GetIdentifiers(WarehouseSqlAuditSettings properties)
        => new() { WorkspaceId = properties.WorkspaceId, WarehouseId = properties.WarehouseId };

    private static WarehouseModels.AuditSettingsState ToSdkState(AuditSettingsState value)
        => value switch
        {
            AuditSettingsState.Enabled => WarehouseModels.AuditSettingsState.Enabled,
            AuditSettingsState.Disabled => WarehouseModels.AuditSettingsState.Disabled,
            _ => throw new ResourceErrorException("InvalidProperty", $"Unsupported audit settings state '{value}'.", "state"),
        };

    private static AuditSettingsState ToModelState(WarehouseModels.AuditSettingsState value)
        => value.ToString() switch
        {
            nameof(AuditSettingsState.Enabled) => AuditSettingsState.Enabled,
            nameof(AuditSettingsState.Disabled) => AuditSettingsState.Disabled,
            var other => throw new ResourceErrorException("InvalidResponse", $"Unsupported audit settings state '{other}' returned by the Fabric API."),
        };

    private static WarehouseSqlAuditSettings ToProperties(WarehouseModels.SqlAuditSettings settings, Guid workspaceId, Guid warehouseId)
        => new()
        {
            WorkspaceId = workspaceId.ToString(),
            WarehouseId = warehouseId.ToString(),
            State = ToModelState(settings.State),
            RetentionDays = settings.RetentionDays,
            AuditActionsAndGroups = [.. settings.AuditActionsAndGroups],
        };

    private static ResourceResponse BuildResponse(string type, string? apiVersion, WarehouseSqlAuditSettings properties)
        => new()
        {
            Type = type,
            ApiVersion = apiVersion,
            Properties = properties,
            Identifiers = new WarehouseSqlAuditSettingsIdentifiers { WorkspaceId = properties.WorkspaceId, WarehouseId = properties.WarehouseId },
        };

    private static Guid ParseId(string value, string target)
        => Guid.TryParse(value, out var result)
            ? result
            : throw new ResourceErrorException("InvalidIdentifier", $"'{value}' is not a valid GUID.", target);
}
