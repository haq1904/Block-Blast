using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CubePuzzle3DPlayerData.Contracts;

public sealed class GameCenterSnapshotContract
{
    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; set; } = PlayerDataConstants.CurrentSchemaVersion;

    [JsonPropertyName("revision")]
    public long Revision { get; set; }

    [JsonPropertyName("createdAtUnixSeconds")]
    public long CreatedAtUnixSeconds { get; set; }

    [JsonPropertyName("updatedAtUnixSeconds")]
    public long UpdatedAtUnixSeconds { get; set; }

    [JsonPropertyName("lifetimeStats")]
    public PlayerStatsContract LifetimeStats { get; set; } = new();

    [JsonPropertyName("currentWeek")]
    public WeeklyPlayerStatsContract CurrentWeek { get; set; } = new();

    [JsonPropertyName("previousWeek")]
    public WeeklyPlayerStatsContract PreviousWeek { get; set; } = new();

    [JsonPropertyName("bestWeekLinesCleared")]
    public long BestWeekLinesCleared { get; set; }

    [JsonPropertyName("recentGames")]
    public List<RecentGameResultContract> RecentGames { get; set; } = new();

    [JsonPropertyName("achievements")]
    public List<AchievementProgressContract> Achievements { get; set; } = new();

    [JsonPropertyName("insights")]
    public List<PlayerInsightContract> Insights { get; set; } = new();
}

public sealed class PlayerStatsContract
{
    [JsonPropertyName("gamesPlayed")]
    public long GamesPlayed { get; set; }

    [JsonPropertyName("totalScore")]
    public long TotalScore { get; set; }

    [JsonPropertyName("bestScore")]
    public int BestScore { get; set; }

    [JsonPropertyName("totalLinesCleared")]
    public long TotalLinesCleared { get; set; }

    [JsonPropertyName("totalBlocksPlaced")]
    public long TotalBlocksPlaced { get; set; }

    [JsonPropertyName("bestCombo")]
    public int BestCombo { get; set; }

    [JsonPropertyName("personalBestBreakCount")]
    public long PersonalBestBreakCount { get; set; }

    public PlayerStatsContract() { }

    public PlayerStatsContract(
        long gamesPlayed,
        long totalScore,
        int bestScore,
        long totalLinesCleared,
        long totalBlocksPlaced,
        int bestCombo,
        long personalBestBreakCount)
    {
        GamesPlayed = gamesPlayed;
        TotalScore = totalScore;
        BestScore = bestScore;
        TotalLinesCleared = totalLinesCleared;
        TotalBlocksPlaced = totalBlocksPlaced;
        BestCombo = bestCombo;
        PersonalBestBreakCount = personalBestBreakCount;
    }
}

public sealed class WeeklyPlayerStatsContract
{
    [JsonPropertyName("weekStartUnixSeconds")]
    public long WeekStartUnixSeconds { get; set; }

    [JsonPropertyName("gamesPlayed")]
    public long GamesPlayed { get; set; }

    [JsonPropertyName("totalScore")]
    public long TotalScore { get; set; }

    [JsonPropertyName("bestScore")]
    public int BestScore { get; set; }

    [JsonPropertyName("totalLinesCleared")]
    public long TotalLinesCleared { get; set; }

    [JsonPropertyName("totalBlocksPlaced")]
    public long TotalBlocksPlaced { get; set; }

    [JsonPropertyName("personalBestBreakCount")]
    public long PersonalBestBreakCount { get; set; }

    public WeeklyPlayerStatsContract() { }

    public WeeklyPlayerStatsContract(
        long weekStartUnixSeconds,
        long gamesPlayed,
        long totalScore,
        int bestScore,
        long totalLinesCleared,
        long totalBlocksPlaced,
        long personalBestBreakCount)
    {
        WeekStartUnixSeconds = weekStartUnixSeconds;
        GamesPlayed = gamesPlayed;
        TotalScore = totalScore;
        BestScore = bestScore;
        TotalLinesCleared = totalLinesCleared;
        TotalBlocksPlaced = totalBlocksPlaced;
        PersonalBestBreakCount = personalBestBreakCount;
    }
}

public sealed class RecentGameResultContract
{
    [JsonPropertyName("gameSessionId")]
    public string GameSessionId { get; set; } = string.Empty;

    [JsonPropertyName("score")]
    public int Score { get; set; }

    [JsonPropertyName("completedAtUnixSeconds")]
    public long CompletedAtUnixSeconds { get; set; }

    public RecentGameResultContract() { }

    public RecentGameResultContract(string gameSessionId, int score, long completedAtUnixSeconds)
    {
        GameSessionId = gameSessionId;
        Score = score;
        CompletedAtUnixSeconds = completedAtUnixSeconds;
    }
}

public sealed class AchievementProgressContract
{
    [JsonPropertyName("achievementId")]
    public string AchievementId { get; set; } = string.Empty;

    [JsonPropertyName("currentValue")]
    public long CurrentValue { get; set; }

    [JsonPropertyName("targetValue")]
    public long TargetValue { get; set; }

    [JsonPropertyName("unlockedAtUnixSeconds")]
    public long UnlockedAtUnixSeconds { get; set; }

    public AchievementProgressContract() { }

    public AchievementProgressContract(
        string achievementId,
        long currentValue,
        long targetValue,
        long unlockedAtUnixSeconds = 0)
    {
        AchievementId = achievementId;
        CurrentValue = currentValue;
        TargetValue = targetValue;
        UnlockedAtUnixSeconds = unlockedAtUnixSeconds;
    }
}

public sealed class PlayerInsightContract
{
    [JsonPropertyName("insightId")]
    public string InsightId { get; set; } = string.Empty;

    [JsonPropertyName("currentValue")]
    public double CurrentValue { get; set; }

    [JsonPropertyName("previousValue")]
    public double PreviousValue { get; set; }

    public PlayerInsightContract() { }

    public PlayerInsightContract(string insightId, double currentValue, double previousValue)
    {
        InsightId = insightId;
        CurrentValue = currentValue;
        PreviousValue = previousValue;
    }
}
