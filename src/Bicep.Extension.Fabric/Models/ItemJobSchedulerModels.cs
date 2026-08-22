using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace Bicep.Extension.Fabric.Models;

public enum ScheduleType
{
    Cron,
    Daily,
    Weekly,
    Monthly,
}

public enum ScheduleDayOfWeek
{
    Monday,
    Tuesday,
    Wednesday,
    Thursday,
    Friday,
    Saturday,
    Sunday,
}

public enum ScheduleWeekIndex
{
    First,
    Second,
    Third,
    Fourth,
    Fifth,
}

public enum ScheduleOccurrenceType
{
    DayOfMonth,
    OrdinalWeekday,
}

public class ScheduleOccurrence
{
    [TypeProperty("The day for triggering jobs", ObjectTypePropertyFlags.Required)]
    public required ScheduleOccurrenceType OccurrenceType { get; set; }

    [TypeProperty("The date to trigger the job (1-31). Required when occurrenceType is DayOfMonth")]
    public int? DayOfMonth { get; set; }

    [TypeProperty("The week of the month. Required when occurrenceType is OrdinalWeekday")]
    public ScheduleWeekIndex? WeekIndex { get; set; }

    [TypeProperty("The weekday for triggering jobs. Required when occurrenceType is OrdinalWeekday")]
    public ScheduleDayOfWeek? Weekday { get; set; }
}

public class ScheduleConfiguration
{
    [TypeProperty("The type of the schedule plan", ObjectTypePropertyFlags.Required)]
    public required ScheduleType Type { get; set; }

    [TypeProperty("The start time for this schedule, in UTC using the YYYY-MM-DDTHH:mm:ssZ format", ObjectTypePropertyFlags.Required)]
    public required string StartDateTime { get; set; }

    [TypeProperty("The end time for this schedule, in UTC using the YYYY-MM-DDTHH:mm:ssZ format. Must be later than startDateTime", ObjectTypePropertyFlags.Required)]
    public required string EndDateTime { get; set; }

    [TypeProperty("The time interval in minutes (1-5270400). Required when type is Cron")]
    public int? Interval { get; set; }

    [TypeProperty("A list of time slots in hh:mm format, at most 100 elements. Required when type is Daily, Weekly, or Monthly")]
    public string[]? Times { get; set; }

    [TypeProperty("A list of weekdays, at most 7 elements. Required when type is Weekly")]
    public ScheduleDayOfWeek[]? Weekdays { get; set; }

    [TypeProperty("The monthly job repeat interval (1-12). Required when type is Monthly")]
    public int? Recurrence { get; set; }

    [TypeProperty("A date for triggering the job. Required when type is Monthly")]
    public ScheduleOccurrence? Occurrence { get; set; }
}

public class ScheduleOwner
{
    [TypeProperty("The principal's ID", ObjectTypePropertyFlags.ReadOnly)]
    public string? Id { get; set; }

    [TypeProperty("The type of the principal", ObjectTypePropertyFlags.ReadOnly)]
    public string? Type { get; set; }
}

public class ItemJobSchedulerIdentifiers
{
    [TypeProperty("The Workspace ID", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public string WorkspaceId { get; set; } = string.Empty;

    [TypeProperty("The item ID", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public string ItemId { get; set; } = string.Empty;

    [TypeProperty("The job type. Allowed job types per item type: dataflow: {ApplyChanges, Execute}; datapipeline: {Execute}; lakehouse: {RefreshMaterializedLakeViews}", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public string JobType { get; set; } = string.Empty;

    [TypeProperty("The Item Job Scheduler ID", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.ReadOnly)]
    public string? Id { get; set; }
}

[ResourceType("ItemJobScheduler")]
public class ItemJobScheduler : ItemJobSchedulerIdentifiers
{
    [TypeProperty("Whether this schedule is enabled", ObjectTypePropertyFlags.Required)]
    public required bool Enabled { get; set; }

    [TypeProperty("The schedule configuration", ObjectTypePropertyFlags.Required)]
    public required ScheduleConfiguration Configuration { get; set; }

    [TypeProperty("The created time stamp of this schedule in UTC, using the YYYY-MM-DDTHH:mm:ssZ format", ObjectTypePropertyFlags.ReadOnly)]
    public string? CreatedDateTime { get; set; }

    [TypeProperty("The user identity that created this schedule or last modified it", ObjectTypePropertyFlags.ReadOnly)]
    public ScheduleOwner? Owner { get; set; }
}
