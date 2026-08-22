using System.Text.Json;
using Bicep.Extension.Fabric.Handlers;
using Bicep.Local.Extension.Host.Handlers;

namespace Bicep.Extension.Fabric.Tests;

[TestClass]
public sealed class FabricWorkspaceConnectionGatewayHandlerTests
{
    [TestMethod]
    public async Task Environment_preview_uses_camel_case_properties()
    {
        var handler = new EnvironmentHandler();
        var workspaceId = Guid.NewGuid();

        var response = await HandlerHarness.PreviewAsync(handler, "Environment", new
        {
            workspaceId,
            displayName = "Shared environment",
        });

        var properties = response.ResourceProperties();
        Assert.AreEqual(workspaceId, properties.GetProperty("workspaceId").GetGuid());
        Assert.AreEqual("Shared environment", properties.GetProperty("displayName").GetString());
        Assert.IsFalse(properties.TryGetProperty("publishDetails", out var publishDetails) && publishDetails.ValueKind != JsonValueKind.Null);
    }

    [TestMethod]
    public async Task WorkspaceGitOutboundPolicy_preview_uses_camel_case_properties()
    {
        var handler = new WorkspaceGitOutboundPolicyHandler();
        var workspaceId = Guid.NewGuid();

        var response = await HandlerHarness.PreviewAsync(handler, "WorkspaceGitOutboundPolicy", new
        {
            workspaceId,
            defaultAction = "Deny",
        });

        var properties = response.ResourceProperties();
        Assert.AreEqual(workspaceId, properties.GetProperty("workspaceId").GetGuid());
        Assert.AreEqual("deny", properties.GetProperty("defaultAction").GetString());
    }

    [TestMethod]
    public async Task WorkspaceOutboundGatewayRules_preview_preserves_allowed_gateways()
    {
        var handler = new WorkspaceOutboundGatewayRulesHandler();
        var workspaceId = Guid.NewGuid();
        var gatewayId = Guid.NewGuid();

        var response = await HandlerHarness.PreviewAsync(handler, "WorkspaceOutboundGatewayRules", new
        {
            workspaceId,
            defaultAction = "Deny",
            allowedGateways = new[] { new { id = gatewayId } },
        });

        var properties = response.ResourceProperties();
        Assert.AreEqual("deny", properties.GetProperty("defaultAction").GetString());
        Assert.AreEqual(gatewayId, properties.GetProperty("allowedGateways")[0].GetProperty("id").GetGuid());
    }

    [TestMethod]
    public async Task WorkspaceOutboundCloudConnectionRules_preview_preserves_rules()
    {
        var handler = new WorkspaceOutboundCloudConnectionRulesHandler();
        var workspaceId = Guid.NewGuid();
        var allowedWorkspaceId = Guid.NewGuid();

        var response = await HandlerHarness.PreviewAsync(handler, "WorkspaceOutboundCloudConnectionRules", new
        {
            workspaceId,
            defaultAction = "Deny",
            rules = new[]
            {
                new
                {
                    connectionType = "Lakehouse",
                    defaultAction = "Allow",
                    allowedWorkspaces = new[] { new { workspaceId = allowedWorkspaceId } },
                },
            },
        });

        var properties = response.ResourceProperties();
        Assert.AreEqual("deny", properties.GetProperty("defaultAction").GetString());
        var rule = properties.GetProperty("rules")[0];
        Assert.AreEqual("Lakehouse", rule.GetProperty("connectionType").GetString());
        Assert.AreEqual("allow", rule.GetProperty("defaultAction").GetString());
        Assert.AreEqual(allowedWorkspaceId, rule.GetProperty("allowedWorkspaces")[0].GetProperty("workspaceId").GetGuid());
    }

    [TestMethod]
    public async Task WorkspaceNetworkCommunicationPolicy_preview_preserves_inbound_and_outbound_rules()
    {
        var handler = new WorkspaceNetworkCommunicationPolicyHandler();
        var workspaceId = Guid.NewGuid();

        var response = await HandlerHarness.PreviewAsync(handler, "WorkspaceNetworkCommunicationPolicy", new
        {
            workspaceId,
            inbound = new { publicAccessRules = new { defaultAction = "Deny" } },
            outbound = new { publicAccessRules = new { defaultAction = "Allow" } },
        });

        var properties = response.ResourceProperties();
        Assert.AreEqual(workspaceId, properties.GetProperty("workspaceId").GetGuid());
        Assert.AreEqual("deny", properties.GetProperty("inbound").GetProperty("publicAccessRules").GetProperty("defaultAction").GetString());
        Assert.AreEqual("allow", properties.GetProperty("outbound").GetProperty("publicAccessRules").GetProperty("defaultAction").GetString());
    }

    [TestMethod]
    public async Task WorkspaceManagedPrivateEndpoint_preview_uses_camel_case_properties()
    {
        var handler = new WorkspaceManagedPrivateEndpointHandler();
        var workspaceId = Guid.NewGuid();

        var response = await HandlerHarness.PreviewAsync(handler, "WorkspaceManagedPrivateEndpoint", new
        {
            workspaceId,
            name = "sql-endpoint",
            targetPrivateLinkResourceId = "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/rg/providers/Microsoft.Sql/servers/sql-server",
            requestMessage = "Please approve",
        });

        var properties = response.ResourceProperties();
        Assert.AreEqual(workspaceId, properties.GetProperty("workspaceId").GetGuid());
        Assert.AreEqual("sql-endpoint", properties.GetProperty("name").GetString());
        Assert.AreEqual("Please approve", properties.GetProperty("requestMessage").GetString());
        Assert.IsFalse(properties.TryGetProperty("WorkspaceId", out _));
    }

    [TestMethod]
    public async Task OneLakeDataAccessSecurity_preview_preserves_decision_rules_and_members()
    {
        var handler = new OneLakeDataAccessSecurityHandler();
        var workspaceId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var objectId = Guid.NewGuid();

        var response = await HandlerHarness.PreviewAsync(handler, "OneLakeDataAccessSecurity", new
        {
            workspaceId,
            itemId,
            roleName = "AnalystsReadOnly",
            decisionRules = new[]
            {
                new
                {
                    effect = "Permit",
                    permission = new[]
                    {
                        new { attributeName = "Path", attributeValueIncludedIn = new[] { "*" } },
                    },
                    constraints = new
                    {
                        rows = new[] { new { tablePath = "/Tables/Sales", value = "Region = 'US'" } },
                    },
                },
            },
            members = new
            {
                microsoftEntraMembers = new[]
                {
                    new { objectId, objectType = "Group", tenantId },
                },
            },
        });

        var properties = response.ResourceProperties();
        Assert.AreEqual(workspaceId, properties.GetProperty("workspaceId").GetGuid());
        Assert.AreEqual(itemId, properties.GetProperty("itemId").GetGuid());
        Assert.AreEqual("AnalystsReadOnly", properties.GetProperty("roleName").GetString());
        var decisionRule = properties.GetProperty("decisionRules")[0];
        Assert.AreEqual("permit", decisionRule.GetProperty("effect").GetString());
        Assert.AreEqual("path", decisionRule.GetProperty("permission")[0].GetProperty("attributeName").GetString());
        Assert.AreEqual("/Tables/Sales", decisionRule.GetProperty("constraints").GetProperty("rows")[0].GetProperty("tablePath").GetString());
        var entraMember = properties.GetProperty("members").GetProperty("microsoftEntraMembers")[0];
        Assert.AreEqual(objectId, entraMember.GetProperty("objectId").GetGuid());
        Assert.AreEqual(tenantId, entraMember.GetProperty("tenantId").GetGuid());
    }

    [TestMethod]
    public async Task SparkWorkspaceSettings_preview_preserves_nested_properties()
    {
        var handler = new SparkWorkspaceSettingsHandler();
        var workspaceId = Guid.NewGuid();

        var response = await HandlerHarness.PreviewAsync(handler, "SparkWorkspaceSettings", new
        {
            workspaceId,
            automaticLog = new { enabled = true },
            pool = new
            {
                customizeComputeEnabled = true,
                starterPool = new { maxNodeCount = 4, maxExecutors = 3 },
            },
        });

        var properties = response.ResourceProperties();
        Assert.AreEqual(workspaceId, properties.GetProperty("workspaceId").GetGuid());
        Assert.IsTrue(properties.GetProperty("automaticLog").GetProperty("enabled").GetBoolean());
        Assert.AreEqual(4, properties.GetProperty("pool").GetProperty("starterPool").GetProperty("maxNodeCount").GetInt32());
    }

    [TestMethod]
    public async Task WarehouseSqlAuditSettings_preview_uses_camel_case_properties()
    {
        var handler = new WarehouseSqlAuditSettingsHandler();
        var workspaceId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();

        var response = await HandlerHarness.PreviewAsync(handler, "WarehouseSqlAuditSettings", new
        {
            workspaceId,
            warehouseId,
            state = "Enabled",
            retentionDays = 30,
            auditActionsAndGroups = new[] { "BATCH_COMPLETED_GROUP" },
        });

        var properties = response.ResourceProperties();
        Assert.AreEqual(workspaceId, properties.GetProperty("workspaceId").GetGuid());
        Assert.AreEqual(warehouseId, properties.GetProperty("warehouseId").GetGuid());
        Assert.AreEqual("enabled", properties.GetProperty("state").GetString());
        Assert.AreEqual(30, properties.GetProperty("retentionDays").GetInt32());
        Assert.AreEqual("BATCH_COMPLETED_GROUP", properties.GetProperty("auditActionsAndGroups")[0].GetString());
    }

    [TestMethod]
    public async Task Connection_preview_preserves_connection_and_credential_details()
    {
        var handler = new ConnectionHandler();

        var response = await HandlerHarness.PreviewAsync(handler, "Connection", new
        {
            displayName = "example",
            connectivityType = "ShareableCloud",
            privacyLevel = "Organizational",
            allowUsageInUserControlledCode = true,
            connectionDetails = new
            {
                type = "FTP",
                creationMethod = "FTP.Contents",
                parameters = new[] { new { name = "server", value = "ftp.example.com" } },
            },
            credentialDetails = new
            {
                credentialType = "Basic",
                connectionEncryption = "NotEncrypted",
                basic = new { username = "user", password = "secret" },
            },
        });

        var properties = response.ResourceProperties();
        Assert.AreEqual("shareableCloud", properties.GetProperty("connectivityType").GetString());
        Assert.AreEqual("organizational", properties.GetProperty("privacyLevel").GetString());
        Assert.AreEqual("FTP", properties.GetProperty("connectionDetails").GetProperty("type").GetString());
        Assert.AreEqual("server", properties.GetProperty("connectionDetails").GetProperty("parameters")[0].GetProperty("name").GetString());
        Assert.AreEqual("basic", properties.GetProperty("credentialDetails").GetProperty("credentialType").GetString());
        Assert.AreEqual("user", properties.GetProperty("credentialDetails").GetProperty("basic").GetProperty("username").GetString());
    }

    [TestMethod]
    public async Task ConnectionRoleAssignment_preview_supports_entire_tenant_principal()
    {
        var handler = new ConnectionRoleAssignmentHandler();
        var connectionId = Guid.NewGuid();
        var principalId = Guid.NewGuid();

        var response = await HandlerHarness.PreviewAsync(handler, "ConnectionRoleAssignment", new
        {
            connectionId,
            principal = new { id = principalId, type = "EntireTenant" },
            role = "UserWithReshare",
        });

        var properties = response.ResourceProperties();
        Assert.AreEqual(connectionId, properties.GetProperty("connectionId").GetGuid());
        Assert.AreEqual(principalId, properties.GetProperty("principal").GetProperty("id").GetGuid());
        Assert.AreEqual("entireTenant", properties.GetProperty("principal").GetProperty("type").GetString());
        Assert.AreEqual("userWithReshare", properties.GetProperty("role").GetString());
    }

    [TestMethod]
    public async Task Gateway_preview_preserves_virtual_network_azure_resource()
    {
        var handler = new GatewayHandler();
        var capacityId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();

        var response = await HandlerHarness.PreviewAsync(handler, "Gateway", new
        {
            type = "VirtualNetwork",
            displayName = "example",
            capacityId,
            inactivityMinutesBeforeSleep = 30,
            numberOfMemberGateways = 1,
            virtualNetworkAzureResource = new
            {
                subscriptionId,
                resourceGroupName = "example-rg",
                virtualNetworkName = "example-vnet",
                subnetName = "example-subnet",
            },
        });

        var properties = response.ResourceProperties();
        Assert.AreEqual("virtualNetwork", properties.GetProperty("type").GetString());
        Assert.AreEqual(capacityId, properties.GetProperty("capacityId").GetGuid());
        Assert.AreEqual(30, properties.GetProperty("inactivityMinutesBeforeSleep").GetInt32());
        Assert.AreEqual("example-vnet", properties.GetProperty("virtualNetworkAzureResource").GetProperty("virtualNetworkName").GetString());
    }

    [TestMethod]
    public async Task GatewayRoleAssignment_preview_uses_camel_case_properties()
    {
        var handler = new GatewayRoleAssignmentHandler();
        var gatewayId = Guid.NewGuid();
        var principalId = Guid.NewGuid();

        var response = await HandlerHarness.PreviewAsync(handler, "GatewayRoleAssignment", new
        {
            gatewayId,
            principal = new { id = principalId, type = "ServicePrincipal" },
            role = "ConnectionCreatorWithResharing",
        });

        var properties = response.ResourceProperties();
        Assert.AreEqual(gatewayId, properties.GetProperty("gatewayId").GetGuid());
        Assert.AreEqual("servicePrincipal", properties.GetProperty("principal").GetProperty("type").GetString());
        Assert.AreEqual("connectionCreatorWithResharing", properties.GetProperty("role").GetString());
    }

    [TestMethod]
    public async Task Shortcut_preview_preserves_onelake_target()
    {
        var handler = new ShortcutHandler();
        var workspaceId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var targetWorkspaceId = Guid.NewGuid();
        var targetItemId = Guid.NewGuid();

        var response = await HandlerHarness.PreviewAsync(handler, "Shortcut", new
        {
            workspaceId,
            itemId,
            path = "Tables",
            name = "MyShortcut",
            shortcutConflictPolicy = "CreateOrOverwrite",
            target = new
            {
                oneLake = new { workspaceId = targetWorkspaceId, itemId = targetItemId, path = "Tables/myFolder" },
            },
        });

        var properties = response.ResourceProperties();
        Assert.AreEqual(workspaceId, properties.GetProperty("workspaceId").GetGuid());
        Assert.AreEqual("MyShortcut", properties.GetProperty("name").GetString());
        Assert.AreEqual("createOrOverwrite", properties.GetProperty("shortcutConflictPolicy").GetString());
        Assert.AreEqual(targetItemId, properties.GetProperty("target").GetProperty("oneLake").GetProperty("itemId").GetGuid());
    }
}
