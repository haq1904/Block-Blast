using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using CubePuzzle3DPlayerData.Contracts;

namespace CubePuzzle3DPlayerData.Domain;

public sealed class ProcessedGameReceipt
{
    [JsonPropertyName("gameSessionId")]
    public string GameSessionId { get; set; } = string.Empty;

    [JsonPropertyName("revision")]
    public long Revision { get; set; }

    [JsonPropertyName("acceptedAtUnixSeconds")]
    public long AcceptedAtUnixSeconds { get; set; }

    public ProcessedGameReceipt() { }

    public ProcessedGameReceipt(string gameSessionId, long revision, long acceptedAtUnixSeconds)
    {
        GameSessionId = gameSessionId;
        Revision = revision;
        AcceptedAtUnixSeconds = acceptedAtUnixSeconds;
    }
}

public sealed class StoredPlayerSnapshot
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

    [JsonPropertyName("processedGameReceipts")]
    public List<ProcessedGameReceipt> ProcessedGameReceipts { get; set; } = new();

    public bool HasReceipt(string gameSessionId)
    {
        if (string.IsNullOrEmpty(gameSessionId) || ProcessedGameReceipts == null)
        {
            return false;
        }

        for (int i = 0; i < ProcessedGameReceipts.Count; i++)
        {
            if (string.Equals(ProcessedGameReceipts[i].GameSessionId, gameSessionId, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    public GameCenterSnapshotContract ToContract()
    {
        return new GameCenterSnapshotContract
        {
            SchemaVersion = SchemaVersion,
            Revision = Revision,
            CreatedAtUnixSeconds = CreatedAtUnixSeconds,
            UpdatedAtUnixSeconds = UpdatedAtUnixSeconds,
            LifetimeStats = LifetimeStats ?? new(),
            CurrentWeek = CurrentWeek ?? new(),
            PreviousWeek = PreviousWeek ?? new(),
            BestWeekLinesCleared = BestWeekLinesCleared,
            RecentGames = RecentGames != null ? new List<RecentGameResultContract>(RecentGames) : new(),
            Achievements = Achievements != null ? new List<AchievementProgressContract>(Achievements) : new(),
            Insights = Insights != null ? new List<PlayerInsightContract>(Insights) : new()
        };
    }

    public static StoredPlayerSnapshot CreateDefault(long nowUnixSeconds)
    {
        var weekStart = UtcWeek.GetWeekStartUnixSeconds(nowUnixSeconds);
        var achievements = new List<AchievementProgressContract>(AchievementDefinitions.All.Count);
        foreach (var def in AchievementDefinitions.All)
        {
            achievements.Add(new AchievementProgressContract(def.Id, 0L, def.TargetValue, 0L));
        }

        return new StoredPlayerSnapshot
        {
            SchemaVersion = PlayerDataConstants.CurrentSchemaVersion,
            Revision = 0L,
            CreatedAtUnixSeconds = nowUnixSeconds,
            UpdatedAtUnixSeconds = nowUnixSeconds,
            LifetimeStats = new PlayerStatsContract(0L, 0L, 0, 0L, 0L, 0, 0L),
            CurrentWeek = new WeeklyPlayerStatsContract(weekStart, 0L, 0L, 0, 0L, 0L, 0L),
            PreviousWeek = new WeeklyPlayerStatsContract(weekStart - UtcWeek.SecondsPerWeek, 0L, 0L, 0, 0L, 0L, 0L),
            BestWeekLinesCleared = 0L,
            RecentGames = new List<RecentGameResultContract>(),
            Achievements = achievements,
            Insights = new List<PlayerInsightContract>(),
            ProcessedGameReceipts = new List<ProcessedGameReceipt>()
        };
    }
}
