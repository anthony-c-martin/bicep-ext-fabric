using System.Text.Json;
using Bicep.Extension.Fabric.Handlers;
using Bicep.Local.Extension.Host.Handlers;

namespace Bicep.Extension.Fabric.Tests;

[TestClass]
public sealed class FabricCoreResourceHandlerTests
{
    [TestMethod]
    public async Task Folder_preview_uses_camel_case_properties()
    {
        var handler = new FolderHandler();
        var workspaceId = Guid.NewGuid();
        var parentFolderId = Guid.NewGuid();

        var response = await HandlerHarness.PreviewAsync(handler, "Folder", new
        {
            workspaceId,
            displayName = "Reports",
            parentFolderId,
        });

        var properties = response.ResourceProperties();
        Assert.AreEqual(workspaceId, properties.GetProperty("workspaceId").GetGuid());
        Assert.AreEqual("Reports", properties.GetProperty("displayName").GetString());
        Assert.AreEqual(parentFolderId, properties.GetProperty("parentFolderId").GetGuid());
        Assert.IsFalse(properties.TryGetProperty("WorkspaceId", out _));
    }

    [TestMethod]
    public async Task Domain_preview_uses_camel_case_properties()
    {
        var handler = new DomainHandler();
        var domainId = Guid.NewGuid();

        var response = await HandlerHarness.PreviewAsync(handler, "Domain", new
        {
            id = domainId,
            displayName = "Sales",
            description = "Sales domain",
        });

        var properties = response.ResourceProperties();
        Assert.AreEqual(domainId, properties.GetProperty("id").GetGuid());
        Assert.AreEqual("Sales", properties.GetProperty("displayName").GetString());
    }

    [TestMethod]
    public async Task Tag_preview_uses_camel_case_properties()
    {
        var handler = new TagHandler();
        var domainId = Guid.NewGuid();

        var response = await HandlerHarness.PreviewAsync(handler, "Tag", new
        {
            displayName = "PII",
            scope = new { type = "Domain", domainId },
        });

        var properties = response.ResourceProperties();
        Assert.AreEqual("PII", properties.GetProperty("displayName").GetString());
        Assert.AreEqual("domain", properties.GetProperty("scope").GetProperty("type").GetString());
        Assert.AreEqual(domainId, properties.GetProperty("scope").GetProperty("domainId").GetGuid());
    }

    [TestMethod]
    public async Task DeploymentPipeline_preview_preserves_stages()
    {
        var handler = new DeploymentPipelineHandler();

        var response = await HandlerHarness.PreviewAsync(handler, "DeploymentPipeline", new
        {
            displayName = "Release pipeline",
            stages = new[]
            {
                new { displayName = "Dev", description = "Development", isPublic = true },
                new { displayName = "Prod", description = "Production", isPublic = false },
            },
        });

        var properties = response.ResourceProperties();
        Assert.AreEqual("Release pipeline", properties.GetProperty("displayName").GetString());
        var stages = properties.GetProperty("stages");
        Assert.AreEqual(2, stages.GetArrayLength());
        Assert.AreEqual("Dev", stages[0].GetProperty("displayName").GetString());
        Assert.IsTrue(stages[0].GetProperty("isPublic").GetBoolean());
    }

    [TestMethod]
    public async Task WorkspaceRoleAssignment_preview_uses_camel_case_properties()
    {
        var handler = new WorkspaceRoleAssignmentHandler();
        var workspaceId = Guid.NewGuid();
        var principalId = Guid.NewGuid();

        var response = await HandlerHarness.PreviewAsync(handler, "WorkspaceRoleAssignment", new
        {
            workspaceId,
            principal = new { id = principalId, type = "User" },
            role = "Member",
        });

        var properties = response.ResourceProperties();
        Assert.AreEqual(workspaceId, properties.GetProperty("workspaceId").GetGuid());
        Assert.AreEqual(principalId, properties.GetProperty("principal").GetProperty("id").GetGuid());
        Assert.AreEqual("member", properties.GetProperty("role").GetString());
    }

    [TestMethod]
    public async Task DeploymentPipelineRoleAssignment_preview_uses_camel_case_properties()
    {
        var handler = new DeploymentPipelineRoleAssignmentHandler();
        var deploymentPipelineId = Guid.NewGuid();
        var principalId = Guid.NewGuid();

        var response = await HandlerHarness.PreviewAsync(handler, "DeploymentPipelineRoleAssignment", new
        {
            deploymentPipelineId,
            principal = new { id = principalId, type = "ServicePrincipal" },
            role = "Admin",
        });

        var properties = response.ResourceProperties();
        Assert.AreEqual(deploymentPipelineId, properties.GetProperty("deploymentPipelineId").GetGuid());
        Assert.AreEqual(principalId, properties.GetProperty("principal").GetProperty("id").GetGuid());
        Assert.AreEqual("admin", properties.GetProperty("role").GetString());
    }

    [TestMethod]
    public async Task DomainWorkspaceAssignment_preview_uses_camel_case_properties()
    {
        var handler = new DomainWorkspaceAssignmentHandler();
        var domainId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();

        var response = await HandlerHarness.PreviewAsync(handler, "DomainWorkspaceAssignment", new
        {
            domainId,
            workspaceIds = new[] { workspaceId },
        });

        var properties = response.ResourceProperties();
        Assert.AreEqual(domainId, properties.GetProperty("domainId").GetGuid());
        Assert.AreEqual(workspaceId, properties.GetProperty("workspaceIds")[0].GetGuid());
    }
}
