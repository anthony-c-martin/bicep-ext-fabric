using System.Text.Json;
using Bicep.Extension.Fabric.Handlers;
using Bicep.Local.Extension.Host.Handlers;

namespace Bicep.Extension.Fabric.Tests;

[TestClass]
public sealed class FabricRichItemHandlerTests
{
    [TestMethod]
    public async Task KQLDatabase_preview_preserves_shortcut_configuration()
    {
        var handler = new KQLDatabaseHandler();
        var workspaceId = Guid.NewGuid();
        var eventhouseId = Guid.NewGuid();

        var response = await HandlerHarness.PreviewAsync(handler, "KQLDatabase", new
        {
            workspaceId,
            displayName = "Telemetry",
            configuration = new
            {
                eventhouseId,
                databaseType = "Shortcut",
                sourceClusterUri = "https://cluster.kusto.windows.net",
                sourceDatabaseName = "Telemetry",
            },
        });

        var properties = response.ResourceProperties();
        var configuration = properties.GetProperty("configuration");
        Assert.AreEqual(eventhouseId, configuration.GetProperty("eventhouseId").GetGuid());
        Assert.AreEqual("shortcut", configuration.GetProperty("databaseType").GetString());
        Assert.AreEqual("Telemetry", configuration.GetProperty("sourceDatabaseName").GetString());
    }

    [TestMethod]
    public async Task WarehouseSnapshot_preview_preserves_configuration()
    {
        var handler = new WarehouseSnapshotHandler();
        var workspaceId = Guid.NewGuid();
        var parentWarehouseId = Guid.NewGuid();

        var response = await HandlerHarness.PreviewAsync(handler, "WarehouseSnapshot", new
        {
            workspaceId,
            displayName = "Nightly",
            configuration = new { parentWarehouseId, snapshotDateTime = "2026-01-01T00:00:00Z" },
        });

        var properties = response.ResourceProperties();
        var configuration = properties.GetProperty("configuration");
        Assert.AreEqual(parentWarehouseId, configuration.GetProperty("parentWarehouseId").GetGuid());
        Assert.AreEqual("2026-01-01T00:00:00Z", configuration.GetProperty("snapshotDateTime").GetString());
    }

    [TestMethod]
    public async Task DigitalTwinBuilderFlow_preview_preserves_item_reference()
    {
        var handler = new DigitalTwinBuilderFlowHandler();
        var workspaceId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        var response = await HandlerHarness.PreviewAsync(handler, "DigitalTwinBuilderFlow", new
        {
            workspaceId,
            displayName = "Flow",
            configuration = new
            {
                digitalTwinBuilderItemReference = new { workspaceId, itemId },
            },
        });

        var properties = response.ResourceProperties();
        var reference = properties.GetProperty("configuration").GetProperty("digitalTwinBuilderItemReference");
        Assert.AreEqual(itemId, reference.GetProperty("itemId").GetGuid());
    }

    [TestMethod]
    [DataRow("D")]
    [DataRow("B")]
    [DataRow("N")]
    public void DomainRoleAssignments_principal_keys_match_regardless_of_id_format(string idFormat)
    {
        // The handler diffs declared principals against the principals returned by the Fabric API.
        // Both keys must normalize their GUIDs, otherwise a principal written in a non-canonical
        // format is unassigned without being re-assigned.
        var principalId = Guid.NewGuid();

        var declaredKey = DomainRoleAssignmentsHandler.ToPrincipalKey(new DomainPrincipal
        {
            Id = principalId.ToString(idFormat).ToUpperInvariant(),
            Type = DomainPrincipalType.Group,
        });

        var currentKey = DomainRoleAssignmentsHandler.ToPrincipalKey(
            new Microsoft.Fabric.Api.Admin.Models.GroupPrincipal(principalId));

        Assert.AreEqual(currentKey, declaredKey, StringComparer.OrdinalIgnoreCase);
    }

    [TestMethod]
    public void DomainRoleAssignments_entire_tenant_principals_share_a_key()
    {
        var declaredKey = DomainRoleAssignmentsHandler.ToPrincipalKey(
            new DomainPrincipal { Type = DomainPrincipalType.EntireTenant });

        var currentKey = DomainRoleAssignmentsHandler.ToPrincipalKey(
            new Microsoft.Fabric.Api.Admin.Models.EntireTenantPrincipal(Guid.NewGuid()));

        Assert.AreEqual(currentKey, declaredKey);
    }
}
