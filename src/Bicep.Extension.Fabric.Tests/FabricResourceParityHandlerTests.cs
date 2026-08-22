using System.Text.Json;
using Bicep.Extension.Fabric.Handlers;
using Bicep.Local.Extension.Host.Handlers;

namespace Bicep.Extension.Fabric.Tests;

[TestClass]
public sealed class FabricResourceParityHandlerTests
{
    [TestMethod]
    public async Task DomainRoleAssignments_preview_preserves_principals()
    {
        var handler = new DomainRoleAssignmentsHandler();
        var domainId = Guid.NewGuid();
        var principalId = Guid.NewGuid();

        var response = await HandlerHarness.PreviewAsync(handler, "DomainRoleAssignments", new
        {
            domainId,
            role = "Contributor",
            principals = new object[]
            {
                new { id = principalId, type = "Group" },
                new { type = "EntireTenant" },
            },
        });

        var properties = response.ResourceProperties();
        Assert.AreEqual(domainId, properties.GetProperty("domainId").GetGuid());
        Assert.AreEqual("contributor", properties.GetProperty("role").GetString());
        Assert.AreEqual(principalId, properties.GetProperty("principals")[0].GetProperty("id").GetGuid());
        Assert.AreEqual("entireTenant", properties.GetProperty("principals")[1].GetProperty("type").GetString());
    }

    [TestMethod]
    public async Task Workspace_preview_preserves_system_assigned_identity()
    {
        var handler = new WorkspaceHandler();

        var response = await HandlerHarness.PreviewAsync(handler, "Workspace", new
        {
            displayName = "Analytics",
            identity = new { type = "SystemAssigned" },
        });

        var properties = response.ResourceProperties();
        Assert.AreEqual("systemAssigned", properties.GetProperty("identity").GetProperty("type").GetString());
    }

    [TestMethod]
    public async Task Gateway_preview_supports_referencing_on_premises_gateways()
    {
        var handler = new GatewayHandler();
        var id = Guid.NewGuid();

        var response = await HandlerHarness.PreviewAsync(handler, "Gateway", new { id, type = "OnPremises" });

        var properties = response.ResourceProperties();
        Assert.AreEqual("onPremises", properties.GetProperty("type").GetString());
        Assert.AreEqual(id, properties.GetProperty("id").GetGuid());
    }

    [TestMethod]
    public async Task Gateway_create_rejects_on_premises_types()
    {
        var handler = new GatewayHandler();

        var response = await HandlerHarness.CreateOrUpdateAsync(handler, "Gateway", new { type = "OnPremises" });

        Assert.IsNotNull(response.ErrorData);
        StringAssert.Contains(response.ErrorData.Error.Message, "does not support creating");
    }

    [TestMethod]
    public async Task Connection_preview_preserves_parameter_data_types()
    {
        var handler = new ConnectionHandler();

        var response = await HandlerHarness.PreviewAsync(handler, "Connection", new
        {
            displayName = "Sql",
            connectivityType = "ShareableCloud",
            connectionDetails = new
            {
                type = "SQL",
                creationMethod = "SQL",
                parameters = new object[]
                {
                    new { name = "server", value = "contoso.database.windows.net" },
                    new { name = "timeout", value = "30", dataType = "Number" },
                },
            },
            credentialDetails = new
            {
                credentialType = "Basic",
                basic = new { username = "admin", password = "secret" },
            },
        });

        var properties = response.ResourceProperties();
        var parameters = properties.GetProperty("connectionDetails").GetProperty("parameters");
        Assert.AreEqual("number", parameters[1].GetProperty("dataType").GetString());
    }
}
