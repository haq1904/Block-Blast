using System;
using System.Text;
using System.Text.Json;
using CubePuzzle3DPlayerData.Contracts;
using CubePuzzle3DPlayerData.Domain;
using NUnit.Framework;

namespace CubePuzzle3DPlayerData.Tests;

[TestFixture]
public class SnapshotCompatibilityAndSizeTest
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    [Test]
    public void Serialization_CamelCaseNaming_MatchesClientExpectedFields()
    {
        var snapshot = StoredPlayerSnapshot.CreateDefault(1761465600L);
        snapshot.LifetimeStats.TotalScore = 12345;
        snapshot.CurrentWeek.TotalLinesCleared = 88;

        string json = JsonSerializer.Serialize(snapshot, s_jsonOptions);

        Assert.That(json.Contains("\"schemaVersion\":"), Is.True);
        Assert.That(json.Contains("\"revision\":"), Is.True);
        Assert.That(json.Contains("\"createdAtUnixSeconds\":"), Is.True);
        Assert.That(json.Contains("\"updatedAtUnixSeconds\":"), Is.True);
        Assert.That(json.Contains("\"lifetimeStats\":"), Is.True);
        Assert.That(json.Contains("\"gamesPlayed\":"), Is.True);
        Assert.That(json.Contains("\"totalScore\":12345"), Is.True);
        Assert.That(json.Contains("\"currentWeek\":"), Is.True);
        Assert.That(json.Contains("\"previousWeek\":"), Is.True);
        Assert.That(json.Contains("\"bestWeekLinesCleared\":"), Is.True);
        Assert.That(json.Contains("\"recentGames\":"), Is.True);
        Assert.That(json.Contains("\"achievements\":"), Is.True);
        Assert.That(json.Contains("\"insights\":"), Is.True);
        Assert.That(json.Contains("\"processedGameReceipts\":"), Is.True);
    }

    [Test]
    public void Serialization_WorstCaseBoundedSnapshot_MeasuresSizeAndPassesBudget()
    {
        long now = 1761465600L;
        var snapshot = StoredPlayerSnapshot.CreateDefault(now);
        snapshot.Revision = 999999L;
        snapshot.LifetimeStats = new PlayerStatsContract(5000L, 50_000_000L, 750000, 15000L, 35000L, 18, 25L);
        snapshot.CurrentWeek = new WeeklyPlayerStatsContract(now, 100L, 1_000_000L, 750000, 500L, 1200L, 3L);
        snapshot.PreviousWeek = new WeeklyPlayerStatsContract(now - UtcWeek.SecondsPerWeek, 120L, 1_200_000L, 700000, 600L, 1500L, 2L);
        snapshot.BestWeekLinesCleared = 600L;

        // 7 recent games with 64-char max session IDs
        for (int i = 0; i < 7; i++)
        {
            snapshot.RecentGames.Add(new RecentGameResultContract(
                new string('s', 64),
                700000,
                now - (i * 60)));
        }

        // 18 achievements fully populated
        snapshot.Achievements.Clear();
        foreach (var def in AchievementDefinitions.All)
        {
            snapshot.Achievements.Add(new AchievementProgressContract(
                def.Id,
                def.TargetValue + 100,
                def.TargetValue,
                now - 1000));
        }

        // 2 insights
        snapshot.Insights.Add(new PlayerInsightContract("new_personal_best_this_week", 3.0, 2.0));
        snapshot.Insights.Add(new PlayerInsightContract("weekly_average_change", 12500.5, 11000.2));

        // 256 receipts with 64-char max session IDs
        for (int i = 0; i < 256; i++)
        {
            snapshot.ProcessedGameReceipts.Add(new ProcessedGameReceipt(
                $"receipt_{i:000}_{new string('x', 50)}", // ~64 chars
                i + 1,
                now - (i * 30)));
        }

        string json = JsonSerializer.Serialize(snapshot, s_jsonOptions);
        byte[] utf8Bytes = Encoding.UTF8.GetBytes(json);
        int byteCount = utf8Bytes.Length;

        Console.WriteLine($"[Worst-Case Snapshot Size] UTF-8 Byte Count: {byteCount} bytes ({byteCount / 1024.0:F2} KiB)");

        // Budget check from Section 10 & 11:
        // Target <= 64 KiB (65,536 bytes)
        // Hard fail > 128 KiB (131,072 bytes)
        Assert.That(byteCount, Is.LessThanOrEqualTo(64 * 1024), $"Worst-case snapshot exceeds 64 KiB target: {byteCount} bytes");
        Assert.That(byteCount, Is.LessThanOrEqualTo(128 * 1024), $"Worst-case snapshot exceeds 128 KiB hard limit: {byteCount} bytes");
    }
}
