using System;

namespace CubePuzzle3DPlayerData.Domain;

public static class UtcWeek
{
    public const long SecondsPerDay = 86400L;
    public const long SecondsPerWeek = 7L * SecondsPerDay;

    public static long GetWeekStartUnixSeconds(long unixSeconds)
    {
        if (unixSeconds <= 0) return 0L;
        var dto = DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
        var dayOfWeek = dto.DayOfWeek;
        int daysSinceMonday = ((int)dayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        var monday = new DateTimeOffset(dto.Year, dto.Month, dto.Day, 0, 0, 0, TimeSpan.Zero)
            .AddDays(-daysSinceMonday);
        return monday.ToUnixTimeSeconds();
    }
}
