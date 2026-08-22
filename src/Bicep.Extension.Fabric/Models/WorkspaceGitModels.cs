using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace Bicep.Extension.Fabric.Models;

public enum WorkspaceGitProviderType
{
    AzureDevOps,
    GitHub,
}

public enum WorkspaceGitCredentialsSource
{
    Automatic,
    ConfiguredConnection,
}

public enum WorkspaceGitInitializationStrategy
{
    None,
    PreferRemote,
    PreferWorkspace,
}

public enum WorkspaceGitConnectionState
{
    NotConnected,
    Connected,
    ConnectedAndInitialized,
}

public class WorkspaceGitProviderDetails
{
    [TypeProperty("The Git provider type. Changing this forces the connection to be recreated.", ObjectTypePropertyFlags.Required)]
    public required WorkspaceGitProviderType GitProviderType { get; set; }

    [TypeProperty("The repository name", ObjectTypePropertyFlags.Required)]
    public required string RepositoryName { get; set; }

    [TypeProperty("The branch name", ObjectTypePropertyFlags.Required)]
    public required string BranchName { get; set; }

    [TypeProperty("The directory name. Must start with a forward slash.", ObjectTypePropertyFlags.Required)]
    public required string DirectoryName { get; set; }

    [TypeProperty("The Azure DevOps organization name. Required when 'gitProviderType' is 'AzureDevOps'.")]
    public string? OrganizationName { get; set; }

    [TypeProperty("The Azure DevOps project name. Required when 'gitProviderType' is 'AzureDevOps'.")]
    public string? ProjectName { get; set; }

    [TypeProperty("The GitHub owner name. Required when 'gitProviderType' is 'GitHub'.")]
    public string? OwnerName { get; set; }
}

public class WorkspaceGitCredentials
{
    [TypeProperty("The Git credentials source. 'Automatic' is only supported for Azure DevOps.", ObjectTypePropertyFlags.Required)]
    public required WorkspaceGitCredentialsSource Source { get; set; }

    [TypeProperty("The connection used to authenticate to the Git provider. Required when 'source' is 'ConfiguredConnection'.")]
    public string? ConnectionId { get; set; }
}

public class WorkspaceGitOptions
{
    [TypeProperty("Whether incoming items may override existing workspace items during the initial sync from Git")]
    public bool? AllowOverrideItems { get; set; }
}

public class WorkspaceGitSyncDetails
{
    [TypeProperty("The commit hash of the last sync", ObjectTypePropertyFlags.ReadOnly)]
    public string? Head { get; set; }

    [TypeProperty("The date and time of the last sync", ObjectTypePropertyFlags.ReadOnly)]
    public string? LastSyncTime { get; set; }
}

public class WorkspaceGitIdentifiers
{
    [TypeProperty("The Fabric workspace ID", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public string WorkspaceId { get; set; } = string.Empty;
}

// Connects a workspace to a Git repository. Deleting the resource disconnects the workspace; the
// contents of the repository are left untouched.
[ResourceType("WorkspaceGit")]
public class WorkspaceGit : WorkspaceGitIdentifiers
{
    [TypeProperty("The Git provider details. Changing these forces the connection to be recreated.", ObjectTypePropertyFlags.Required)]
    public required WorkspaceGitProviderDetails GitProviderDetails { get; set; }

    [TypeProperty("The Git credentials details", ObjectTypePropertyFlags.Required)]
    public required WorkspaceGitCredentials GitCredentials { get; set; }

    [TypeProperty("The strategy used to reconcile the workspace and the repository when the connection is initialized", ObjectTypePropertyFlags.Required)]
    public required WorkspaceGitInitializationStrategy InitializationStrategy { get; set; }

    [TypeProperty("Options applied to the initial sync from Git")]
    public WorkspaceGitOptions? Options { get; set; }

    [TypeProperty("The Git connection state", ObjectTypePropertyFlags.ReadOnly)]
    public WorkspaceGitConnectionState? GitConnectionState { get; set; }

    [TypeProperty("Details of the last sync between the workspace and the repository", ObjectTypePropertyFlags.ReadOnly)]
    public WorkspaceGitSyncDetails? GitSyncDetails { get; set; }
}
