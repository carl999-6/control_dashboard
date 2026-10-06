using System.Globalization;
using Dashboard.Api.Domain;

namespace Dashboard.Api.Services;

public static class AutomationScheduleCalculator
{
    public static DateTimeOffset? CalculateNext(AutomationSchedule schedule, string timeZoneId, DateTimeOffset fromUtc)
    {
        if (!schedule.IsEnabled || schedule.Frequency == "manual") return null;
        if (schedule.Frequency == "interval") return fromUtc.AddMinutes(schedule.IntervalMinutes ?? 60);

        var zone = FindZone(timeZoneId);
        var localNow = TimeZoneInfo.ConvertTime(fromUtc, zone);
        if (!TimeOnly.TryParseExact(schedule.LocalTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
        {
            time = new TimeOnly(8, 0);
        }

        var candidateDate = DateOnly.FromDateTime(localNow.DateTime);
        if (schedule.Frequency == "weekly")
        {
            var targetDay = schedule.DayOfWeek ?? 1;
            var currentDay = (int)candidateDate.DayOfWeek;
            var days = (targetDay - currentDay + 7) % 7;
            candidateDate = candidateDate.AddDays(days);
        }

        var localCandidate = candidateDate.ToDateTime(time, DateTimeKind.Unspecified);
        if (localCandidate <= localNow.DateTime)
        {
            localCandidate = localCandidate.AddDays(schedule.Frequency == "weekly" ? 7 : 1);
        }

        if (zone.IsInvalidTime(localCandidate)) localCandidate = localCandidate.AddHours(1);
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localCandidate, zone), TimeSpan.Zero);
    }

    public static DateTimeOffset StartOfLocalDayUtc(string timeZoneId, DateTimeOffset nowUtc)
    {
        var zone = FindZone(timeZoneId);
        var local = TimeZoneInfo.ConvertTime(nowUtc, zone);
        var midnight = DateTime.SpecifyKind(local.Date, DateTimeKind.Unspecified);
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(midnight, zone), TimeSpan.Zero);
    }

    private static TimeZoneInfo FindZone(string id)
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.Utc; }
        catch (InvalidTimeZoneException) { return TimeZoneInfo.Utc; }
    }
}
