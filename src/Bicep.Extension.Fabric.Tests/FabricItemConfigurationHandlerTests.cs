using System.Text.Json;
using Bicep.Extension.Fabric.Handlers;
using Bicep.Local.Extension.Host.Handlers;

namespace Bicep.Extension.Fabric.Tests;

[TestClass]
public sealed class FabricItemConfigurationHandlerTests
{
    [TestMethod]
    public async Task Warehouse_preview_preserves_configuration()
    {
        var handler = new WarehouseHandler();
        var workspaceId = Guid.NewGuid();

        var response = await HandlerHarness.PreviewAsync(handler, "Warehouse", new
        {
            workspaceId,
            displayName = "Sales warehouse",
            configuration = new { collationType = "Latin1_General_100_BIN2_UTF8" },
        });

        var properties = response.ResourceProperties();
        Assert.AreEqual(workspaceId, properties.GetProperty("workspaceId").GetGuid());
        Assert.AreEqual(
            "latin1_General_100_BIN2_UTF8",
            properties.GetProperty("configuration").GetProperty("collationType").GetString());
        Assert.IsFalse(properties.TryGetProperty("definition", out var definition) && definition.ValueKind != JsonValueKind.Null);
    }

    [TestMethod]
    public async Task SQLDatabase_preview_preserves_new_configuration()
    {
        var handler = new SQLDatabaseHandler();
        var workspaceId = Guid.NewGuid();

        var response = await HandlerHarness.PreviewAsync(handler, "SQLDatabase", new
        {
            workspaceId,
            displayName = "Orders",
            configuration = new
            {
                creationMode = "New",
                collation = "SQL_Latin1_General_CP1_CI_AS",
                backupRetentionDays = 10,
            },
        });

        var properties = response.ResourceProperties();
        var configuration = properties.GetProperty("configuration");
        Assert.AreEqual("new", configuration.GetProperty("creationMode").GetString());
        Assert.AreEqual("SQL_Latin1_General_CP1_CI_AS", configuration.GetProperty("collation").GetString());
        Assert.AreEqual(10, configuration.GetProperty("backupRetentionDays").GetInt32());
    }

    [TestMethod]
    public async Task SQLDatabase_preview_preserves_restore_configuration_with_source_reference()
    {
        var handler = new SQLDatabaseHandler();
        var workspaceId = Guid.NewGuid();
        var sourceItemId = Guid.NewGuid();
        var sourceWorkspaceId = Guid.NewGuid();

        var response = await HandlerHarness.PreviewAsync(handler, "SQLDatabase", new
        {
            workspaceId,
            displayName = "Orders restored",
            configuration = new
            {
                creationMode = "Restore",
                restorePointInTime = "2026-01-01T00:00:00Z",
                sourceDatabaseReference = new { itemId = sourceItemId, workspaceId = sourceWorkspaceId },
            },
        });

        var properties = response.ResourceProperties();
        var configuration = properties.GetProperty("configuration");
        Assert.AreEqual("restore", configuration.GetProperty("creationMode").GetString());
        var sourceReference = configuration.GetProperty("sourceDatabaseReference");
        Assert.AreEqual(sourceItemId, sourceReference.GetProperty("itemId").GetGuid());
        Assert.AreEqual(sourceWorkspaceId, sourceReference.GetProperty("workspaceId").GetGuid());
    }

    [TestMethod]
    public async Task Eventhouse_preview_preserves_configuration()
    {
        var handler = new EventhouseHandler();
        var workspaceId = Guid.NewGuid();

        var response = await HandlerHarness.PreviewAsync(handler, "Eventhouse", new
        {
            workspaceId,
            displayName = "Telemetry",
            configuration = new { minimumConsumptionUnits = "2.25" },
        });

        var properties = response.ResourceProperties();
        Assert.AreEqual(
            "2.25",
            properties.GetProperty("configuration").GetProperty("minimumConsumptionUnits").GetString());
    }

    [TestMethod]
    public async Task SparkJobDefinition_preview_preserves_definition_parts()
    {
        var handler = new SparkJobDefinitionHandler();
        var workspaceId = Guid.NewGuid();

        var response = await HandlerHarness.PreviewAsync(handler, "SparkJobDefinition", new
        {
            workspaceId,
            displayName = "Ingest job",
            definition = new
            {
                format = "SparkJobDefinitionV1",
                parts = new[]
                {
                    new
                    {
                        path = "SparkJobDefinitionV1.json",
                        payload = Convert.ToBase64String("{}"u8.ToArray()),
                    },
                },
            },
        });

        var properties = response.ResourceProperties();
        Assert.AreEqual("SparkJobDefinitionV1", properties.GetProperty("definition").GetProperty("format").GetString());
        Assert.AreEqual(
            "SparkJobDefinitionV1.json",
            properties.GetProperty("definition").GetProperty("parts")[0].GetProperty("path").GetString());
    }
}
