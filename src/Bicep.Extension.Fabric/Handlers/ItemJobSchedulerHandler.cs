using System.Globalization;
using CoreModels = Microsoft.Fabric.Api.Core.Models;

namespace Bicep.Extension.Fabric.Handlers;

public sealed class ItemJobSchedulerHandler : FabricResourceHandlerBase<ItemJobScheduler, ItemJobSchedulerIdentifiers>
{
    private const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ssZ";

    protected override Task<ResourceResponse> Preview(ResourceRequest request, CancellationToken cancellationToken)
        => Task.FromResult(GetResponse(request));

    protected override Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            var workspaceId = ParseId(request.Properties.WorkspaceId, "workspaceId");
            var itemId = ParseId(request.Properties.ItemId, "itemId");
            var jobType = request.Properties.JobType;
            var configuration = ToSdkScheduleConfig(request.Properties.Configuration);
            CoreModels.ItemSchedule result;

            if (request.Properties.Id is { } scheduleIdValue)
            {
                var scheduleId = ParseId(scheduleIdValue, "id");
                var update = new CoreModels.UpdateScheduleRequest(request.Properties.Enabled, configuration);
                result = (await client.Core.JobScheduler.UpdateItemScheduleAsync(workspaceId, itemId, jobType, scheduleId, update, cancellationToken)).Value;
            }
            else
            {
                var create = new CoreModels.CreateScheduleRequest(request.Properties.Enabled, configuration);
                result = (await client.Core.JobScheduler.CreateItemScheduleAsync(workspaceId, itemId, jobType, create, cancellationToken)).Value;
            }

            return BuildResponse(request.Type, request.ApiVersion, ToProperties(result, workspaceId, itemId, jobType));
        });

    protected override Task<ResourceResponse> Get(ReferenceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            var workspaceId = ParseId(request.Identifiers.WorkspaceId, "workspaceId");
            var itemId = ParseId(request.Identifiers.ItemId, "itemId");
            var jobType = request.Identifiers.JobType;

            var result = (await client.Core.JobScheduler.GetItemScheduleAsync(
                workspaceId,
                itemId,
                jobType,
                RequireId(request.Identifiers.Id),
                cancellationToken)).Value;

            return BuildResponse(request.Type, request.ApiVersion, ToProperties(result, workspaceId, itemId, jobType));
        });

    protected override Task<ResourceResponse> Delete(ReferenceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            await client.Core.JobScheduler.DeleteItemScheduleAsync(
                ParseId(request.Identifiers.WorkspaceId, "workspaceId"),
                ParseId(request.Identifiers.ItemId, "itemId"),
                request.Identifiers.JobType,
                RequireId(request.Identifiers.Id),
                cancellationToken);

            return GetResponse(request, properties: null);
        });

    protected override ItemJobSchedulerIdentifiers GetIdentifiers(ItemJobScheduler properties)
        => new() { WorkspaceId = properties.WorkspaceId, ItemId = properties.ItemId, JobType = properties.JobType, Id = properties.Id };

    private static CoreModels.ScheduleConfig ToSdkScheduleConfig(ScheduleConfiguration configuration)
    {
        var start = ParseDateTime(configuration.StartDateTime, "configuration.startDateTime");
        var end = ParseDateTime(configuration.EndDateTime, "configuration.endDateTime");

        return configuration.Type switch
        {
            ScheduleType.Cron => new CoreModels.CronScheduleConfig(
                start,
                end,
                "UTC",
                configuration.Interval ?? throw new ResourceErrorException("MissingProperty", "configuration.interval is required when configuration.type is Cron.", "configuration.interval")),
            ScheduleType.Daily => new CoreModels.DailyScheduleConfig(
                start,
                end,
                "UTC",
                configuration.Times ?? throw new ResourceErrorException("MissingProperty", "configuration.times is required when configuration.type is Daily.", "configuration.times")),
            ScheduleType.Weekly => new CoreModels.WeeklyScheduleConfig(
                start,
                end,
                "UTC",
                configuration.Times ?? throw new ResourceErrorException("MissingProperty", "configuration.times is required when configuration.type is Weekly.", "configuration.times"),
                (configuration.Weekdays ?? throw new ResourceErrorException("MissingProperty", "configuration.weekdays is required when configuration.type is Weekly.", "configuration.weekdays")).Select(ToSdkDayOfWeek)),
            ScheduleType.Monthly => new CoreModels.MonthlyScheduleConfig(
                start,
                end,
                "UTC",
                configuration.Recurrence ?? throw new ResourceErrorException("MissingProperty", "configuration.recurrence is required when configuration.type is Monthly.", "configuration.recurrence"),
                ToSdkOccurrence(configuration.Occurrence ?? throw new ResourceErrorException("MissingProperty", "configuration.occurrence is required when configuration.type is Monthly.", "configuration.occurrence")),
                configuration.Times ?? throw new ResourceErrorException("MissingProperty", "configuration.times is required when configuration.type is Monthly.", "configuration.times")),
            _ => throw new ResourceErrorException("InvalidProperty", $"Unsupported schedule type '{configuration.Type}'.", "configuration.type"),
        };
    }

    private static CoreModels.MonthlyOccurrence ToSdkOccurrence(ScheduleOccurrence occurrence)
        => occurrence.OccurrenceType switch
        {
            ScheduleOccurrenceType.DayOfMonth => new CoreModels.DayOfMonth(
                occurrence.DayOfMonth ?? throw new ResourceErrorException("MissingProperty", "configuration.occurrence.dayOfMonth is required when occurrenceType is DayOfMonth.", "configuration.occurrence.dayOfMonth")),
            ScheduleOccurrenceType.OrdinalWeekday => new CoreModels.OrdinalWeekday(
                ToSdkWeekIndex(occurrence.WeekIndex ?? throw new ResourceErrorException("MissingProperty", "configuration.occurrence.weekIndex is required when occurrenceType is OrdinalWeekday.", "configuration.occurrence.weekIndex")),
                ToSdkDayOfWeek(occurrence.Weekday ?? throw new ResourceErrorException("MissingProperty", "configuration.occurrence.weekday is required when occurrenceType is OrdinalWeekday.", "configuration.occurrence.weekday"))),
            _ => throw new ResourceErrorException("InvalidProperty", $"Unsupported occurrence type '{occurrence.OccurrenceType}'.", "configuration.occurrence.occurrenceType"),
        };

    private static ScheduleConfiguration ToModelConfiguration(CoreModels.ScheduleConfig config)
        => config switch
        {
            CoreModels.CronScheduleConfig cron => new ScheduleConfiguration
            {
                Type = ScheduleType.Cron,
                StartDateTime = FormatDateTime(cron.StartDateTime),
                EndDateTime = FormatDateTime(cron.EndDateTime),
                Interval = cron.Interval,
            },
            CoreModels.DailyScheduleConfig daily => new ScheduleConfiguration
            {
                Type = ScheduleType.Daily,
                StartDateTime = FormatDateTime(daily.StartDateTime),
                EndDateTime = FormatDateTime(daily.EndDateTime),
                Times = [.. daily.Times],
            },
            CoreModels.WeeklyScheduleConfig weekly => new ScheduleConfiguration
            {
                Type = ScheduleType.Weekly,
                StartDateTime = FormatDateTime(weekly.StartDateTime),
                EndDateTime = FormatDateTime(weekly.EndDateTime),
                Times = [.. weekly.Times],
                Weekdays = [.. weekly.Weekdays.Select(ToModelDayOfWeek)],
            },
            CoreModels.MonthlyScheduleConfig monthly => new ScheduleConfiguration
            {
                Type = ScheduleType.Monthly,
                StartDateTime = FormatDateTime(monthly.StartDateTime),
                EndDateTime = FormatDateTime(monthly.EndDateTime),
                Times = [.. monthly.Times],
                Recurrence = monthly.Recurrence,
                Occurrence = ToModelOccurrence(monthly.Occurrence),
            },
            _ => throw new ResourceErrorException("InvalidResponse", $"Unsupported schedule configuration type '{config.GetType().Name}' returned by the Fabric API."),
        };

    private static ScheduleOccurrence ToModelOccurrence(CoreModels.MonthlyOccurrence occurrence)
        => occurrence switch
        {
            CoreModels.DayOfMonth dayOfMonth => new ScheduleOccurrence { OccurrenceType = ScheduleOccurrenceType.DayOfMonth, DayOfMonth = dayOfMonth.DayOfMonthProperty },
            CoreModels.OrdinalWeekday ordinalWeekday => new ScheduleOccurrence
            {
                OccurrenceType = ScheduleOccurrenceType.OrdinalWeekday,
                WeekIndex = ToModelWeekIndex(ordinalWeekday.WeekIndex),
                Weekday = ToModelDayOfWeek(ordinalWeekday.Weekday),
            },
            _ => throw new ResourceErrorException("InvalidResponse", $"Unsupported occurrence type '{occurrence.GetType().Name}' returned by the Fabric API."),
        };

    private static CoreModels.DayOfWeek ToSdkDayOfWeek(ScheduleDayOfWeek value)
        => value switch
        {
            ScheduleDayOfWeek.Monday => CoreModels.DayOfWeek.Monday,
            ScheduleDayOfWeek.Tuesday => CoreModels.DayOfWeek.Tuesday,
            ScheduleDayOfWeek.Wednesday => CoreModels.DayOfWeek.Wednesday,
            ScheduleDayOfWeek.Thursday => CoreModels.DayOfWeek.Thursday,
            ScheduleDayOfWeek.Friday => CoreModels.DayOfWeek.Friday,
            ScheduleDayOfWeek.Saturday => CoreModels.DayOfWeek.Saturday,
            ScheduleDayOfWeek.Sunday => CoreModels.DayOfWeek.Sunday,
            _ => throw new ResourceErrorException("InvalidProperty", $"Unsupported weekday '{value}'.", "configuration.weekdays"),
        };

    private static ScheduleDayOfWeek ToModelDayOfWeek(CoreModels.DayOfWeek value)
        => value.ToString() switch
        {
            nameof(ScheduleDayOfWeek.Monday) => ScheduleDayOfWeek.Monday,
            nameof(ScheduleDayOfWeek.Tuesday) => ScheduleDayOfWeek.Tuesday,
            nameof(ScheduleDayOfWeek.Wednesday) => ScheduleDayOfWeek.Wednesday,
            nameof(ScheduleDayOfWeek.Thursday) => ScheduleDayOfWeek.Thursday,
            nameof(ScheduleDayOfWeek.Friday) => ScheduleDayOfWeek.Friday,
            nameof(ScheduleDayOfWeek.Saturday) => ScheduleDayOfWeek.Saturday,
            nameof(ScheduleDayOfWeek.Sunday) => ScheduleDayOfWeek.Sunday,
            var other => throw new ResourceErrorException("InvalidResponse", $"Unsupported weekday '{other}' returned by the Fabric API."),
        };

    private static CoreModels.WeekIndex ToSdkWeekIndex(ScheduleWeekIndex value)
        => value switch
        {
            ScheduleWeekIndex.First => CoreModels.WeekIndex.First,
            ScheduleWeekIndex.Second => CoreModels.WeekIndex.Second,
            ScheduleWeekIndex.Third => CoreModels.WeekIndex.Third,
            ScheduleWeekIndex.Fourth => CoreModels.WeekIndex.Fourth,
            ScheduleWeekIndex.Fifth => CoreModels.WeekIndex.Fifth,
            _ => throw new ResourceErrorException("InvalidProperty", $"Unsupported week index '{value}'.", "configuration.occurrence.weekIndex"),
        };

    private static ScheduleWeekIndex ToModelWeekIndex(CoreModels.WeekIndex value)
        => value.ToString() switch
        {
            nameof(ScheduleWeekIndex.First) => ScheduleWeekIndex.First,
            nameof(ScheduleWeekIndex.Second) => ScheduleWeekIndex.Second,
            nameof(ScheduleWeekIndex.Third) => ScheduleWeekIndex.Third,
            nameof(ScheduleWeekIndex.Fourth) => ScheduleWeekIndex.Fourth,
            nameof(ScheduleWeekIndex.Fifth) => ScheduleWeekIndex.Fifth,
            var other => throw new ResourceErrorException("InvalidResponse", $"Unsupported week index '{other}' returned by the Fabric API."),
        };

    private static ScheduleOwner? ToModelOwner(CoreModels.Principal? owner)
        => owner is null
            ? null
            : new ScheduleOwner
            {
                Id = owner.Id.ToString(),
                Type = owner switch
                {
                    CoreModels.UserPrincipal => nameof(PrincipalType.User),
                    CoreModels.GroupPrincipal => nameof(PrincipalType.Group),
                    CoreModels.ServicePrincipal => nameof(PrincipalType.ServicePrincipal),
                    CoreModels.ServicePrincipalProfilePrincipal => nameof(PrincipalType.ServicePrincipalProfile),
                    CoreModels.EntireTenantPrincipal => nameof(ConnectionPrincipalType.EntireTenant),
                    _ => owner.GetType().Name,
                },
            };

    private static DateTimeOffset ParseDateTime(string value, string target)
        => DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var result)
            ? result
            : throw new ResourceErrorException("InvalidProperty", $"'{value}' is not a valid UTC date/time in the YYYY-MM-DDTHH:mm:ssZ format.", target);

    private static string FormatDateTime(DateTimeOffset value)
        => value.UtcDateTime.ToString(DateTimeFormat, CultureInfo.InvariantCulture);

    private static ItemJobScheduler ToProperties(CoreModels.ItemSchedule schedule, Guid workspaceId, Guid itemId, string jobType)
        => new()
        {
            WorkspaceId = workspaceId.ToString(),
            ItemId = itemId.ToString(),
            JobType = jobType,
            Id = schedule.Id.ToString(),
            Enabled = schedule.Enabled,
            Configuration = ToModelConfiguration(schedule.Configuration),
            CreatedDateTime = schedule.CreatedDateTime is { } createdDateTime ? FormatDateTime(createdDateTime) : null,
            Owner = ToModelOwner(schedule.Owner),
        };

    private static ResourceResponse BuildResponse(string type, string? apiVersion, ItemJobScheduler properties)
        => new()
        {
            Type = type,
            ApiVersion = apiVersion,
            Properties = properties,
            Identifiers = new ItemJobSchedulerIdentifiers { WorkspaceId = properties.WorkspaceId, ItemId = properties.ItemId, JobType = properties.JobType, Id = properties.Id },
        };

    private static Guid RequireId(string? id)
        => id is null
            ? throw new ResourceErrorException("MissingIdentifier", "The Item Job Scheduler ID is required for this operation.")
            : ParseId(id, "id");

    private static Guid ParseId(string value, string target)
        => Guid.TryParse(value, out var result)
            ? result
            : throw new ResourceErrorException("InvalidIdentifier", $"'{value}' is not a valid GUID.", target);
}
