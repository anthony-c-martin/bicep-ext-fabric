using Microsoft.Fabric.Api;
using CoreModels = Microsoft.Fabric.Api.Core.Models;

namespace Bicep.Extension.Fabric.Handlers;

public sealed class WorkspaceGitHandler : FabricResourceHandlerBase<WorkspaceGit, WorkspaceGitIdentifiers>
{
    protected override Task<ResourceResponse> Preview(ResourceRequest request, CancellationToken cancellationToken)
        => Task.FromResult(GetResponse(request));

    protected override Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            var workspaceId = ParseGuid(request.Properties.WorkspaceId, "workspaceId");
            var connection = (await client.Core.Git.GetConnectionAsync(workspaceId, cancellationToken)).Value;

            if (connection.GitConnectionState == CoreModels.GitConnectionState.NotConnected)
            {
                var connect = new CoreModels.GitConnectRequest(ToSdkProviderDetails(request.Properties.GitProviderDetails))
                {
                    MyGitCredentials = ToSdkCredentials(request.Properties.GitCredentials),
                };

                await client.Core.Git.ConnectAsync(workspaceId, connect, cancellationToken);
            }
            else
            {
                // Only the credentials can be changed in place. The Fabric API sets the provider
                // details on connect only, so a change there has to be surfaced rather than ignored.
                EnsureProviderDetailsUnchanged(request.Properties.GitProviderDetails, connection.GitProviderDetails);

                await client.Core.Git.UpdateMyGitCredentialsAsync(
                    workspaceId,
                    ToSdkUpdateCredentialsRequest(request.Properties.GitCredentials),
                    cancellationToken);
            }

            if (connection.GitConnectionState != CoreModels.GitConnectionState.ConnectedAndInitialized)
            {
                await InitializeAsync(client, workspaceId, request.Properties, cancellationToken);
            }

            var result = (await client.Core.Git.GetConnectionAsync(workspaceId, cancellationToken)).Value;

            return BuildResponse(request.Type, request.ApiVersion, ToProperties(result, request.Properties));
        });

    protected override Task<ResourceResponse> Get(ReferenceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            var workspaceId = ParseGuid(request.Identifiers.WorkspaceId, "workspaceId");
            var result = (await client.Core.Git.GetConnectionAsync(workspaceId, cancellationToken)).Value;

            if (result.GitConnectionState == CoreModels.GitConnectionState.NotConnected)
            {
                throw new ResourceErrorException(
                    "GitConnectionNotFound",
                    $"Workspace '{workspaceId}' is not connected to a Git repository.");
            }

            var properties = ToProperties(result, declared: null);
            properties.WorkspaceId = request.Identifiers.WorkspaceId;

            return BuildResponse(request.Type, request.ApiVersion, properties);
        });

    protected override Task<ResourceResponse> Delete(ReferenceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            await client.Core.Git.DisconnectAsync(ParseGuid(request.Identifiers.WorkspaceId, "workspaceId"), cancellationToken);
            return GetResponse(request, properties: null);
        });

    protected override WorkspaceGitIdentifiers GetIdentifiers(WorkspaceGit properties)
        => new() { WorkspaceId = properties.WorkspaceId };

    /// <summary>
    /// Initializes the connection and performs whichever sync the Fabric API reports as required, so
    /// that the workspace and the repository are in sync once the deployment completes.
    /// </summary>
    private static async Task InitializeAsync(FabricClient client, Guid workspaceId, WorkspaceGit properties, CancellationToken cancellationToken)
    {
        var initialize = new CoreModels.InitializeGitConnectionRequest
        {
            InitializationStrategy = ToSdkInitializationStrategy(properties.InitializationStrategy),
        };

        var result = (await client.Core.Git.InitializeConnectionAsync(workspaceId, initialize, cancellationToken)).Value;

        if (result.RequiredAction == CoreModels.RequiredAction.UpdateFromGit && result.RemoteCommitHash is { } remoteCommitHash)
        {
            await client.Core.Git.UpdateFromGitAsync(
                workspaceId,
                new CoreModels.UpdateFromGitRequest(remoteCommitHash)
                {
                    WorkspaceHead = result.WorkspaceHead,
                    Options = new CoreModels.UpdateOptions { AllowOverrideItems = properties.Options?.AllowOverrideItems },
                },
                cancellationToken);
        }
        else if (result.RequiredAction == CoreModels.RequiredAction.CommitToGit)
        {
            await client.Core.Git.CommitToGitAsync(
                workspaceId,
                new CoreModels.CommitToGitRequest(CoreModels.CommitMode.All) { WorkspaceHead = result.WorkspaceHead },
                cancellationToken);
        }
    }

    private static CoreModels.InitializationStrategy ToSdkInitializationStrategy(WorkspaceGitInitializationStrategy strategy)
        => strategy switch
        {
            WorkspaceGitInitializationStrategy.None => CoreModels.InitializationStrategy.None,
            WorkspaceGitInitializationStrategy.PreferRemote => CoreModels.InitializationStrategy.PreferRemote,
            WorkspaceGitInitializationStrategy.PreferWorkspace => CoreModels.InitializationStrategy.PreferWorkspace,
            _ => throw new ResourceErrorException("InvalidProperty", $"Unsupported initialization strategy '{strategy}'.", "initializationStrategy"),
        };

    /// <summary>
    /// The Fabric API only accepts Git provider details when the workspace is first connected, so a
    /// change to them cannot be applied in place. Failing loudly avoids silently reporting success
    /// while the workspace stays connected to the previous repository.
    /// </summary>
    private static void EnsureProviderDetailsUnchanged(WorkspaceGitProviderDetails declared, CoreModels.GitProviderDetails? current)
    {
        if (current is null)
        {
            return;
        }

        var changed = new List<string>();

        void Compare(string property, string? declaredValue, string? currentValue)
        {
            if (declaredValue is not null && !string.Equals(declaredValue, currentValue, StringComparison.OrdinalIgnoreCase))
            {
                changed.Add($"{property} ('{currentValue}' -> '{declaredValue}')");
            }
        }

        var currentProviderType = current is CoreModels.GitHubDetails
            ? WorkspaceGitProviderType.GitHub
            : WorkspaceGitProviderType.AzureDevOps;

        Compare("gitProviderType", declared.GitProviderType.ToString(), currentProviderType.ToString());
        Compare("repositoryName", declared.RepositoryName, current.RepositoryName);
        Compare("branchName", declared.BranchName, current.BranchName);
        Compare("directoryName", declared.DirectoryName, current.DirectoryName);
        Compare("organizationName", declared.OrganizationName, (current as CoreModels.AzureDevOpsDetails)?.OrganizationName);
        Compare("projectName", declared.ProjectName, (current as CoreModels.AzureDevOpsDetails)?.ProjectName);
        Compare("ownerName", declared.OwnerName, (current as CoreModels.GitHubDetails)?.OwnerName);

        if (changed.Count > 0)
        {
            throw new ResourceErrorException(
                "GitProviderDetailsImmutable",
                $"The Fabric API does not support changing the Git provider details of a connected workspace ({string.Join(", ", changed)}). Remove the WorkspaceGit resource to disconnect the workspace, then redeploy it with the new details.",
                "gitProviderDetails");
        }
    }

    private static CoreModels.GitProviderDetails ToSdkProviderDetails(WorkspaceGitProviderDetails details)
        => details.GitProviderType switch
        {
            WorkspaceGitProviderType.AzureDevOps => new CoreModels.AzureDevOpsDetails(
                details.RepositoryName,
                details.BranchName,
                details.DirectoryName,
                details.OrganizationName ?? throw MissingProviderProperty("organizationName", "AzureDevOps"),
                details.ProjectName ?? throw MissingProviderProperty("projectName", "AzureDevOps")),
            WorkspaceGitProviderType.GitHub => new CoreModels.GitHubDetails(
                details.RepositoryName,
                details.BranchName,
                details.DirectoryName,
                details.OwnerName ?? throw MissingProviderProperty("ownerName", "GitHub")),
            _ => throw new ResourceErrorException("InvalidProperty", $"Unsupported Git provider type '{details.GitProviderType}'.", "gitProviderDetails.gitProviderType"),
        };

    private static ResourceErrorException MissingProviderProperty(string property, string providerType)
        => new(
            "MissingProperty",
            $"'gitProviderDetails.{property}' is required when 'gitProviderType' is '{providerType}'.",
            $"gitProviderDetails.{property}");

    private static Guid RequireConnectionId(WorkspaceGitCredentials credentials)
        => ParseGuid(
            credentials.ConnectionId ?? throw new ResourceErrorException(
                "MissingProperty",
                "'gitCredentials.connectionId' is required when 'source' is 'ConfiguredConnection'.",
                "gitCredentials.connectionId"),
            "gitCredentials.connectionId");

    private static CoreModels.GitCredentials ToSdkCredentials(WorkspaceGitCredentials credentials)
        => credentials.Source switch
        {
            WorkspaceGitCredentialsSource.Automatic => new CoreModels.AutomaticGitCredentials(),
            WorkspaceGitCredentialsSource.ConfiguredConnection => new CoreModels.ConfiguredConnectionGitCredentials(RequireConnectionId(credentials)),
            _ => throw new ResourceErrorException("InvalidProperty", $"Unsupported Git credentials source '{credentials.Source}'.", "gitCredentials.source"),
        };

    private static CoreModels.UpdateGitCredentialsRequest ToSdkUpdateCredentialsRequest(WorkspaceGitCredentials credentials)
        => credentials.Source switch
        {
            WorkspaceGitCredentialsSource.Automatic => new CoreModels.UpdateGitCredentialsToAutomaticRequest(),
            WorkspaceGitCredentialsSource.ConfiguredConnection => new CoreModels.UpdateGitCredentialsToConfiguredConnectionRequest(RequireConnectionId(credentials)),
            _ => throw new ResourceErrorException("InvalidProperty", $"Unsupported Git credentials source '{credentials.Source}'.", "gitCredentials.source"),
        };

    /// <summary>
    /// The Fabric API never returns the configured credentials or the initialization strategy, so
    /// those are echoed from the declared properties when they are available.
    /// </summary>
    private static WorkspaceGit ToProperties(CoreModels.GitConnection connection, WorkspaceGit? declared)
    {
        var details = connection.GitProviderDetails;

        return new WorkspaceGit
        {
            WorkspaceId = declared?.WorkspaceId ?? string.Empty,
            GitProviderDetails = new WorkspaceGitProviderDetails
            {
                GitProviderType = details is CoreModels.GitHubDetails
                    ? WorkspaceGitProviderType.GitHub
                    : WorkspaceGitProviderType.AzureDevOps,
                RepositoryName = details?.RepositoryName ?? string.Empty,
                BranchName = details?.BranchName ?? string.Empty,
                DirectoryName = details?.DirectoryName ?? string.Empty,
                OrganizationName = (details as CoreModels.AzureDevOpsDetails)?.OrganizationName,
                ProjectName = (details as CoreModels.AzureDevOpsDetails)?.ProjectName,
                OwnerName = (details as CoreModels.GitHubDetails)?.OwnerName,
            },
            GitCredentials = declared?.GitCredentials
                ?? new WorkspaceGitCredentials { Source = WorkspaceGitCredentialsSource.ConfiguredConnection },
            InitializationStrategy = declared?.InitializationStrategy ?? WorkspaceGitInitializationStrategy.None,
            Options = declared?.Options,
            GitConnectionState = connection.GitConnectionState is { } state && Enum.TryParse<WorkspaceGitConnectionState>(state.ToString(), out var parsedState)
                ? parsedState
                : null,
            GitSyncDetails = connection.GitSyncDetails is { } sync
                ? new WorkspaceGitSyncDetails
                {
                    Head = sync.Head,
                    LastSyncTime = sync.LastSyncTime.ToString("o"),
                }
                : null,
        };
    }

    private ResourceResponse BuildResponse(string type, string? apiVersion, WorkspaceGit properties)
        => new()
        {
            Type = type,
            ApiVersion = apiVersion,
            Properties = properties,
            Identifiers = GetIdentifiers(properties),
        };
}
