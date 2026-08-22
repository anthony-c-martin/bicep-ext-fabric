using System.Text.Json;
using Bicep.Extension.Fabric.Handlers;
using Bicep.Local.Extension.Host.Handlers;

namespace Bicep.Extension.Fabric.Tests;

[TestClass]
public sealed class FabricSchedulingTenantGitHandlerTests
{
    [TestMethod]
    public async Task ItemJobScheduler_preview_preserves_weekly_configuration()
    {
        var handler = new ItemJobSchedulerHandler();
        var workspaceId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        var response = await HandlerHarness.PreviewAsync(handler, "ItemJobScheduler", new
        {
            workspaceId,
            itemId,
            jobType = "Execute",
            enabled = true,
            configuration = new
            {
                type = "Weekly",
                startDateTime = "2025-11-11T10:00:00Z",
                endDateTime = "2025-11-12T10:00:00Z",
                times = new[] { "10:00" },
                weekdays = new[] { "Monday" },
            },
        });

        var properties = response.ResourceProperties();
        Assert.AreEqual(workspaceId, properties.GetProperty("workspaceId").GetGuid());
        Assert.AreEqual("Execute", properties.GetProperty("jobType").GetString());
        Assert.IsTrue(properties.GetProperty("enabled").GetBoolean());
        Assert.AreEqual("weekly", properties.GetProperty("configuration").GetProperty("type").GetString());
        Assert.AreEqual("monday", properties.GetProperty("configuration").GetProperty("weekdays")[0].GetString());
    }

    [TestMethod]
    public async Task SparkCustomPool_preview_preserves_auto_scale_and_dynamic_executor_allocation()
    {
        var handler = new SparkCustomPoolHandler();
        var workspaceId = Guid.NewGuid();

        var response = await HandlerHarness.PreviewAsync(handler, "SparkCustomPool", new
        {
            workspaceId,
            name = "example",
            type = "Workspace",
            nodeFamily = "MemoryOptimized",
            nodeSize = "Small",
            autoScale = new { enabled = true, minNodeCount = 1, maxNodeCount = 3 },
            dynamicExecutorAllocation = new { enabled = true, minExecutors = 1, maxExecutors = 2 },
        });

        var properties = response.ResourceProperties();
        Assert.AreEqual(workspaceId, properties.GetProperty("workspaceId").GetGuid());
        Assert.AreEqual("memoryOptimized", properties.GetProperty("nodeFamily").GetString());
        Assert.AreEqual("small", properties.GetProperty("nodeSize").GetString());
        Assert.AreEqual(3, properties.GetProperty("autoScale").GetProperty("maxNodeCount").GetInt32());
        Assert.AreEqual(2, properties.GetProperty("dynamicExecutorAllocation").GetProperty("maxExecutors").GetInt32());
    }

    [TestMethod]
    public async Task SparkEnvironmentSettings_preview_preserves_pool_and_spark_properties()
    {
        var handler = new SparkEnvironmentSettingsHandler();
        var workspaceId = Guid.NewGuid();
        var environmentId = Guid.NewGuid();

        var response = await HandlerHarness.PreviewAsync(handler, "SparkEnvironmentSettings", new
        {
            workspaceId,
            environmentId,
            publicationStatus = "Staging",
            driverCores = 4,
            driverMemory = "28g",
            pool = new { name = "Starter Pool", type = "Workspace" },
            sparkProperties = new Dictionary<string, string> { ["spark.acls.enable"] = "true" },
        });

        var properties = response.ResourceProperties();
        Assert.AreEqual("staging", properties.GetProperty("publicationStatus").GetString());
        Assert.AreEqual("28g", properties.GetProperty("driverMemory").GetString());
        Assert.AreEqual("workspace", properties.GetProperty("pool").GetProperty("type").GetString());
        Assert.AreEqual("true", properties.GetProperty("sparkProperties").GetProperty("spark.acls.enable").GetString());
    }

    [TestMethod]
    public async Task ExternalDataShare_preview_preserves_paths_and_recipient()
    {
        var handler = new ExternalDataShareHandler();
        var workspaceId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        var response = await HandlerHarness.PreviewAsync(handler, "ExternalDataShare", new
        {
            workspaceId,
            itemId,
            paths = new[] { "Files/Sales/Contoso_Sales_2023" },
            recipient = new { userPrincipalName = "example@example.com" },
        });

        var properties = response.ResourceProperties();
        Assert.AreEqual(workspaceId, properties.GetProperty("workspaceId").GetGuid());
        Assert.AreEqual("Files/Sales/Contoso_Sales_2023", properties.GetProperty("paths")[0].GetString());
        Assert.AreEqual("example@example.com", properties.GetProperty("recipient").GetProperty("userPrincipalName").GetString());
    }

    [TestMethod]
    public async Task TenantSetting_preview_preserves_security_groups_and_properties()
    {
        var handler = new TenantSettingHandler();
        var graphId = Guid.NewGuid().ToString();

        var response = await HandlerHarness.PreviewAsync(handler, "TenantSetting", new
        {
            settingName = "ExampleSetting",
            enabled = true,
            deleteBehaviour = "Disable",
            enabledSecurityGroups = new[] { new { graphId } },
            properties = new[] { new { name = "MaxRows", type = "Integer", value = "100" } },
        });

        var properties = response.ResourceProperties();
        Assert.AreEqual("ExampleSetting", properties.GetProperty("settingName").GetString());
        Assert.IsTrue(properties.GetProperty("enabled").GetBoolean());
        Assert.AreEqual("disable", properties.GetProperty("deleteBehaviour").GetString());
        Assert.AreEqual(graphId, properties.GetProperty("enabledSecurityGroups")[0].GetProperty("graphId").GetString());
        Assert.AreEqual("integer", properties.GetProperty("properties")[0].GetProperty("type").GetString());
    }

    [TestMethod]
    public async Task WorkspaceGit_preview_preserves_azure_devops_provider_details()
    {
        var handler = new WorkspaceGitHandler();
        var workspaceId = Guid.NewGuid();
        var connectionId = Guid.NewGuid();

        var response = await HandlerHarness.PreviewAsync(handler, "WorkspaceGit", new
        {
            workspaceId,
            initializationStrategy = "PreferWorkspace",
            gitProviderDetails = new
            {
                gitProviderType = "AzureDevOps",
                organizationName = "contoso",
                projectName = "analytics",
                repositoryName = "fabric",
                branchName = "main",
                directoryName = "/workspace",
            },
            gitCredentials = new
            {
                source = "ConfiguredConnection",
                connectionId,
            },
            options = new { allowOverrideItems = true },
        });

        var properties = response.ResourceProperties();
        Assert.AreEqual(workspaceId, properties.GetProperty("workspaceId").GetGuid());
        Assert.AreEqual("preferWorkspace", properties.GetProperty("initializationStrategy").GetString());
        Assert.AreEqual("azureDevOps", properties.GetProperty("gitProviderDetails").GetProperty("gitProviderType").GetString());
        Assert.AreEqual("contoso", properties.GetProperty("gitProviderDetails").GetProperty("organizationName").GetString());
        Assert.AreEqual("/workspace", properties.GetProperty("gitProviderDetails").GetProperty("directoryName").GetString());
        Assert.AreEqual(connectionId, properties.GetProperty("gitCredentials").GetProperty("connectionId").GetGuid());
        Assert.IsTrue(properties.GetProperty("options").GetProperty("allowOverrideItems").GetBoolean());
    }
}
