using System;
using CubePuzzle3DPlayerData.Domain;
using NUnit.Framework;

namespace CubePuzzle3DPlayerData.Tests;

[TestFixture]
public class UtcWeekTest
{
    [Test]
    public void GetWeekStartUnixSeconds_MondayExact_ReturnsSameTimestamp()
    {
        // 2026-10-26 00:00:00 UTC (Monday)
        var monday = new DateTimeOffset(2026, 10, 26, 0, 0, 0, TimeSpan.Zero);
        long mondaySec = monday.ToUnixTimeSeconds();

        long result = UtcWeek.GetWeekStartUnixSeconds(mondaySec);
        Assert.That(result, Is.EqualTo(mondaySec));
    }

    [Test]
    public void GetWeekStartUnixSeconds_Midweek_ReturnsMondayMidnight()
    {
        // 2026-10-28 15:30:45 UTC (Wednesday)
        var wednesday = new DateTimeOffset(2026, 10, 28, 15, 30, 45, TimeSpan.Zero);
        var monday = new DateTimeOffset(2026, 10, 26, 0, 0, 0, TimeSpan.Zero);

        long result = UtcWeek.GetWeekStartUnixSeconds(wednesday.ToUnixTimeSeconds());
        Assert.That(result, Is.EqualTo(monday.ToUnixTimeSeconds()));
    }

    [Test]
    public void GetWeekStartUnixSeconds_SundayEndOfWeek_ReturnsPrecedingMondayMidnight()
    {
        // 2026-11-01 23:59:59 UTC (Sunday)
        var sunday = new DateTimeOffset(2026, 11, 1, 23, 59, 59, TimeSpan.Zero);
        var monday = new DateTimeOffset(2026, 10, 26, 0, 0, 0, TimeSpan.Zero);

        long result = UtcWeek.GetWeekStartUnixSeconds(sunday.ToUnixTimeSeconds());
        Assert.That(result, Is.EqualTo(monday.ToUnixTimeSeconds()));
    }

    [Test]
    public void GetWeekStartUnixSeconds_NextMonday_RolloversToNewWeek()
    {
        // 2026-11-02 00:00:00 UTC (Next Monday)
        var nextMonday = new DateTimeOffset(2026, 11, 2, 0, 0, 0, TimeSpan.Zero);

        long result = UtcWeek.GetWeekStartUnixSeconds(nextMonday.ToUnixTimeSeconds());
        Assert.That(result, Is.EqualTo(nextMonday.ToUnixTimeSeconds()));
    }

    [TestCase(0L)]
    [TestCase(-100L)]
    public void GetWeekStartUnixSeconds_ZeroOrNegative_ReturnsZero(long input)
    {
        Assert.That(UtcWeek.GetWeekStartUnixSeconds(input), Is.EqualTo(0L));
    }
}
