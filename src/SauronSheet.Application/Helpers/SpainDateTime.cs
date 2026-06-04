namespace SauronSheet.Application.Helpers;

using System;

/// <summary>
/// Date/time helper for Spain-local timezone normalization.
/// PostgreSQL timestamptz stores datetimes in UTC. Since SauronSheet operates
/// exclusively in Spain (UTC+1 / UTC+2), we normalize UTC dates back to
/// Spain local time when grouping by month or comparing date boundaries.
/// </summary>
public static class SpainDateTime
{
    private static readonly TimeZoneInfo SpainZone = ResolveSpainTimeZone();

    private static TimeZoneInfo ResolveSpainTimeZone()
    {
        var id = OperatingSystem.IsWindows() ? "Romance Standard Time" : "Europe/Madrid";
        return TimeZoneInfo.FindSystemTimeZoneById(id);
    }

    /// <summary>
    /// Converts a DateTime to Spain local time, regardless of its Kind.
    /// Handles both CET (UTC+1) and CEST (UTC+2) automatically.
    /// </summary>
    public static DateTime ToSpainLocal(this DateTime dateTime)
    {
        return dateTime.Kind switch
        {
            DateTimeKind.Utc => TimeZoneInfo.ConvertTimeFromUtc(dateTime, SpainZone),
            DateTimeKind.Local => dateTime,
            DateTimeKind.Unspecified => dateTime, // Assume already local for date-only values
            _ => dateTime
        };
    }

    /// <summary>
    /// Returns the month number (1-12) in Spain local time.
    /// Fixes the PostgreSQL timestamptz → UTC → Spain month boundary issue.
    /// </summary>
    public static int GetSpainMonth(this DateTime dateTime)
    {
        return dateTime.ToSpainLocal().Month;
    }
}
