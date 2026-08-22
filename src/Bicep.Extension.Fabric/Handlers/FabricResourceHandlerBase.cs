using System.Globalization;
using Azure;
using Bicep.Local.Extension.Host.Handlers;
using Microsoft.Fabric.Api;
using Microsoft.Fabric.Api.Core.Models;

namespace Bicep.Extension.Fabric.Handlers;

public abstract class FabricResourceHandlerBase<TProperties, TIdentifiers> : TypedResourceHandler<TProperties, TIdentifiers, Configuration>
    where TProperties : class
    where TIdentifiers : class
{
    protected async Task<ResourceResponse> HandleRequest(
        ResourceBase resource,
        Func<FabricClient, Task<ResourceResponse>> operation)
    {
        try
        {
            return await operation(new FabricClient(resource.Config.AccessToken));
        }
        catch (RequestFailedException exception)
        {
            throw new ResourceErrorException(
                exception.ErrorCode ?? "FabricApiError",
                exception.Message);
        }
        catch (ResourceErrorException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new ResourceErrorException(
                "UnhandledException",
                exception.Message,
                details:
                [
                    new ErrorDetail
                    {
                        Code = exception.GetType().Name,
                        Message = exception.ToString(),
                    }
                ]);
        }
    }

    // ResourceErrorException is nested inside the generic ResourceHandler base, so these helpers
    // cannot live in a standalone static class. They deliberately avoid the ParseId/RequireId/
    // ParseDouble names that several handlers still declare privately, to avoid hiding them.
    protected static Guid ParseGuid(string value, string target)
        => Guid.TryParse(value, out var result)
            ? result
            : throw new ResourceErrorException("InvalidIdentifier", $"'{value}' is not a valid GUID.", target);

    protected static Guid? ParseOptionalGuid(string? value, string target)
        => value is null ? null : ParseGuid(value, target);

    protected static Guid RequireGuid(string? id, string description)
        => id is null
            ? throw new ResourceErrorException("MissingIdentifier", $"The {description} is required for this operation.")
            : ParseGuid(id, "id");

    protected static double ParseInvariantDouble(string value, string target)
        => double.TryParse(value, CultureInfo.InvariantCulture, out var result)
            ? result
            : throw new ResourceErrorException("InvalidNumber", $"'{value}' is not a valid number.", target);

    protected static DateTimeOffset ParseTimestamp(string value, string target)
        => DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result)
            ? result
            : throw new ResourceErrorException("InvalidDateTime", $"'{value}' is not a valid ISO 8601 date and time.", target);

    protected static string[]? ToTagIds(Item item)
        => item.Tags is { Count: > 0 } tags
            ? tags.Select(tag => tag.Id.ToString()).ToArray()
            : null;

    /// <summary>
    /// Reconciles the tags applied to an item with the declared set, and returns the resulting set.
    /// </summary>
    protected static async Task<string[]> SyncTagsAsync(
        FabricClient client,
        Guid workspaceId,
        Guid itemId,
        string[] desiredTags,
        string[] currentTags,
        CancellationToken cancellationToken)
    {
        var desired = desiredTags.Select(tag => ParseGuid(tag, "tags")).Distinct().ToList();
        var current = currentTags.Select(tag => ParseGuid(tag, "tags")).Distinct().ToList();

        var removed = current.Except(desired).ToList();
        if (removed.Count > 0)
        {
            await client.Core.Tags.UnapplyTagsAsync(workspaceId, itemId, new UnapplyTagsRequest(removed), cancellationToken);
        }

        var added = desired.Except(current).ToList();
        if (added.Count > 0)
        {
            await client.Core.Tags.ApplyTagsAsync(workspaceId, itemId, new ApplyTagsRequest(added), cancellationToken);
        }

        return desired.Select(tag => tag.ToString()).ToArray();
    }
}
