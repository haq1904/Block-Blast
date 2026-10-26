using System;
using System.Collections.Generic;
using System.Linq;
using CubePuzzle3DPlayerData.Contracts;

namespace CubePuzzle3DPlayerData.Domain;

public sealed class ReducerResult
{
    public StoredPlayerSnapshot Snapshot { get; }
    public List<string> NewlyUnlockedAchievementIds { get; }
    public bool IsNewPersonalBest { get; }

    public ReducerResult(
        StoredPlayerSnapshot snapshot,
        List<string> newlyUnlockedAchievementIds,
        bool isNewPersonalBest)
    {
        Snapshot = snapshot;
        NewlyUnlockedAchievementIds = newlyUnlockedAchievementIds;
        IsNewPersonalBest = isNewPersonalBest;
    }
}

public static class PlayerSnapshotReducer
{
    public static ReducerResult Reduce(
        StoredPlayerSnapshot current,
        GameResultRequest request,
        long serverNowUnixSeconds)
    {
        checked
        {
            long newRevision = current.Revision + 1L;
            long createdAt = current.CreatedAtUnixSeconds > 0L ? current.CreatedAtUnixSeconds : serverNowUnixSeconds;
            long updatedAt = serverNowUnixSeconds;

            // 1. Lifetime stats
            long newGamesPlayed = current.LifetimeStats.GamesPlayed + 1L;
            long newTotalScore = current.LifetimeStats.TotalScore + request.Score;
            long newTotalLines = current.LifetimeStats.TotalLinesCleared + request.LinesCleared;
            long newTotalBlocks = current.LifetimeStats.TotalBlocksPlaced + request.BlocksPlaced;
            int newBestCombo = Math.Max(current.LifetimeStats.BestCombo, request.BestCombo);

            bool isFirstGame = current.LifetimeStats.GamesPlayed == 0L;
            bool isNewPersonalBest = false;
            int newBestScore = current.LifetimeStats.BestScore;
            long newPbBreakCount = current.LifetimeStats.PersonalBestBreakCount;

            if (isFirstGame)
            {
                newBestScore = request.Score;
                // First game establishes baseline; does not count as a personal best break.
                isNewPersonalBest = false;
            }
            else if (request.Score > current.LifetimeStats.BestScore)
            {
                newBestScore = request.Score;
                newPbBreakCount += 1L;
                isNewPersonalBest = true;
            }

            var newLifetimeStats = new PlayerStatsContract(
                newGamesPlayed,
                newTotalScore,
                newBestScore,
                newTotalLines,
                newTotalBlocks,
                newBestCombo,
                newPbBreakCount);

            // 2. Recent games (oldest -> newest, max 7)
            var newRecentGames = new List<RecentGameResultContract>(current.RecentGames ?? Enumerable.Empty<RecentGameResultContract>());
            newRecentGames.Add(new RecentGameResultContract(request.GameSessionId, request.Score, serverNowUnixSeconds));
            if (newRecentGames.Count > PlayerDataConstants.MaxRecentGames)
            {
                newRecentGames = newRecentGames.GetRange(
                    newRecentGames.Count - PlayerDataConstants.MaxRecentGames,
                    PlayerDataConstants.MaxRecentGames);
            }

            // 3. Weekly stats
            long currentWeekStart = UtcWeek.GetWeekStartUnixSeconds(serverNowUnixSeconds);
            long prevStoredWeekStart = current.CurrentWeek?.WeekStartUnixSeconds ?? 0L;

            WeeklyPlayerStatsContract newCurrentWeek;
            WeeklyPlayerStatsContract newPreviousWeek;

            if (prevStoredWeekStart == 0L || prevStoredWeekStart == currentWeekStart)
            {
                // Same week (or uninitialized)
                newPreviousWeek = current.PreviousWeek ??
                    new WeeklyPlayerStatsContract(currentWeekStart - UtcWeek.SecondsPerWeek, 0L, 0L, 0, 0L, 0L, 0L);

                long currGames = (current.CurrentWeek?.GamesPlayed ?? 0L) + 1L;
                long currScore = (current.CurrentWeek?.TotalScore ?? 0L) + request.Score;
                int currBestScore = Math.Max(current.CurrentWeek?.BestScore ?? 0, request.Score);
                long currLines = (current.CurrentWeek?.TotalLinesCleared ?? 0L) + request.LinesCleared;
                long currBlocks = (current.CurrentWeek?.TotalBlocksPlaced ?? 0L) + request.BlocksPlaced;
                long currPbBreaks = (current.CurrentWeek?.PersonalBestBreakCount ?? 0L) + (isNewPersonalBest ? 1L : 0L);

                newCurrentWeek = new WeeklyPlayerStatsContract(
                    currentWeekStart,
                    currGames,
                    currScore,
                    currBestScore,
                    currLines,
                    currBlocks,
                    currPbBreaks);
            }
            else if (currentWeekStart == prevStoredWeekStart + UtcWeek.SecondsPerWeek)
            {
                // Immediate next week: rotate current -> previous
                newPreviousWeek = current.CurrentWeek!;
                newCurrentWeek = new WeeklyPlayerStatsContract(
                    currentWeekStart,
                    1L,
                    request.Score,
                    request.Score,
                    request.LinesCleared,
                    request.BlocksPlaced,
                    isNewPersonalBest ? 1L : 0L);
            }
            else if (currentWeekStart > prevStoredWeekStart + UtcWeek.SecondsPerWeek)
            {
                // Gap of more than one week: previous week was empty
                newPreviousWeek = new WeeklyPlayerStatsContract(
                    currentWeekStart - UtcWeek.SecondsPerWeek,
                    0L, 0L, 0, 0L, 0L, 0L);
                newCurrentWeek = new WeeklyPlayerStatsContract(
                    currentWeekStart,
                    1L,
                    request.Score,
                    request.Score,
                    request.LinesCleared,
                    request.BlocksPlaced,
                    isNewPersonalBest ? 1L : 0L);
            }
            else
            {
                // Edge case: server time moved slightly backwards
                newPreviousWeek = current.PreviousWeek ??
                    new WeeklyPlayerStatsContract(prevStoredWeekStart - UtcWeek.SecondsPerWeek, 0L, 0L, 0, 0L, 0L, 0L);

                long currGames = (current.CurrentWeek?.GamesPlayed ?? 0L) + 1L;
                long currScore = (current.CurrentWeek?.TotalScore ?? 0L) + request.Score;
                int currBestScore = Math.Max(current.CurrentWeek?.BestScore ?? 0, request.Score);
                long currLines = (current.CurrentWeek?.TotalLinesCleared ?? 0L) + request.LinesCleared;
                long currBlocks = (current.CurrentWeek?.TotalBlocksPlaced ?? 0L) + request.BlocksPlaced;
                long currPbBreaks = (current.CurrentWeek?.PersonalBestBreakCount ?? 0L) + (isNewPersonalBest ? 1L : 0L);

                newCurrentWeek = new WeeklyPlayerStatsContract(
                    prevStoredWeekStart,
                    currGames,
                    currScore,
                    currBestScore,
                    currLines,
                    currBlocks,
                    currPbBreaks);
            }

            long newBestWeekLines = Math.Max(current.BestWeekLinesCleared, newCurrentWeek.TotalLinesCleared);

            // 4. Achievements (18 catalog definitions in deterministic order)
            var existingMap = new Dictionary<string, AchievementProgressContract>(StringComparer.Ordinal);
            if (current.Achievements != null)
            {
                foreach (var ach in current.Achievements)
                {
                    if (ach != null && !string.IsNullOrEmpty(ach.AchievementId))
                    {
                        existingMap[ach.AchievementId] = ach;
                    }
                }
            }

            var newAchievements = new List<AchievementProgressContract>(AchievementDefinitions.All.Count);
            var newlyUnlockedAchievementIds = new List<string>();

            foreach (var def in AchievementDefinitions.All)
            {
                long currentValue = def.Metric switch
                {
                    AchievementMetric.GamesPlayed => newGamesPlayed,
                    AchievementMetric.BestScore => newBestScore,
                    AchievementMetric.TotalLinesCleared => newTotalLines,
                    AchievementMetric.BestCombo => newBestCombo,
                    AchievementMetric.TotalBlocksPlaced => newTotalBlocks,
                    AchievementMetric.PersonalBestBreakCount => newPbBreakCount,
                    _ => 0L
                };

                long targetValue = def.TargetValue;
                long unlockedAt = 0L;

                if (existingMap.TryGetValue(def.Id, out var existing) && existing.UnlockedAtUnixSeconds > 0L)
                {
                    // Already unlocked; preserve original unlock timestamp
                    unlockedAt = existing.UnlockedAtUnixSeconds;
                }
                else if (currentValue >= targetValue)
                {
                    // Newly unlocked in this game result
                    unlockedAt = serverNowUnixSeconds;
                    newlyUnlockedAchievementIds.Add(def.Id);
                }

                newAchievements.Add(new AchievementProgressContract(def.Id, currentValue, targetValue, unlockedAt));
            }

            // 5. Insights (only if GamesPlayed >= 5, max 2)
            var newInsights = ComputeInsights(
                newGamesPlayed,
                newTotalScore,
                newLifetimeStats,
                newCurrentWeek,
                newPreviousWeek,
                current.BestWeekLinesCleared,
                newRecentGames,
                newAchievements);

            // 6. Receipts ring buffer (max 256)
            var newReceipts = new List<ProcessedGameReceipt>(current.ProcessedGameReceipts ?? Enumerable.Empty<ProcessedGameReceipt>());
            newReceipts.Add(new ProcessedGameReceipt(request.GameSessionId, newRevision, serverNowUnixSeconds));
            if (newReceipts.Count > PlayerDataConstants.MaxReceipts)
            {
                newReceipts = newReceipts.GetRange(
                    newReceipts.Count - PlayerDataConstants.MaxReceipts,
                    PlayerDataConstants.MaxReceipts);
            }

            var updatedSnapshot = new StoredPlayerSnapshot
            {
                SchemaVersion = PlayerDataConstants.CurrentSchemaVersion,
                Revision = newRevision,
                CreatedAtUnixSeconds = createdAt,
                UpdatedAtUnixSeconds = updatedAt,
                LifetimeStats = newLifetimeStats,
                CurrentWeek = newCurrentWeek,
                PreviousWeek = newPreviousWeek,
                BestWeekLinesCleared = newBestWeekLines,
                RecentGames = newRecentGames,
                Achievements = newAchievements,
                Insights = newInsights,
                ProcessedGameReceipts = newReceipts
            };

            return new ReducerResult(updatedSnapshot, newlyUnlockedAchievementIds, isNewPersonalBest);
        }
    }

    private static List<PlayerInsightContract> ComputeInsights(
        long gamesPlayed,
        long totalScore,
        PlayerStatsContract lifetimeStats,
        WeeklyPlayerStatsContract currentWeek,
        WeeklyPlayerStatsContract previousWeek,
        long previousHistoricalBestLines,
        List<RecentGameResultContract> recentGames,
        List<AchievementProgressContract> achievements)
    {
        var candidates = new List<PlayerInsightContract>();
        if (gamesPlayed < 5L)
        {
            return candidates;
        }

        // 1. new_personal_best_this_week
        if (currentWeek.PersonalBestBreakCount > 0L)
        {
            candidates.Add(new PlayerInsightContract(
                AchievementDefinitions.Insights.NewPersonalBestThisWeek,
                currentWeek.PersonalBestBreakCount,
                previousWeek.PersonalBestBreakCount));
        }

        // 2. weekly_average_change
        if (currentWeek.GamesPlayed > 0L && previousWeek.GamesPlayed > 0L && previousWeek.TotalScore > 0L)
        {
            double currAvg = (double)currentWeek.TotalScore / currentWeek.GamesPlayed;
            double prevAvg = (double)previousWeek.TotalScore / previousWeek.GamesPlayed;
            if (prevAvg > 0.0)
            {
                candidates.Add(new PlayerInsightContract(
                    AchievementDefinitions.Insights.WeeklyAverageChange,
                    currAvg,
                    prevAvg));
            }
        }

        // 3. recent_vs_lifetime_average
        if (recentGames.Count >= 3 && gamesPlayed > recentGames.Count)
        {
            double recentAvg = recentGames.Average(r => (double)r.Score);
            double lifetimeAvg = (double)totalScore / gamesPlayed;
            candidates.Add(new PlayerInsightContract(
                AchievementDefinitions.Insights.RecentVsLifetimeAverage,
                recentAvg,
                lifetimeAvg));
        }

        // 4. best_combo_next_goal
        long nextComboTarget = FindNextComboTarget(lifetimeStats.BestCombo, achievements);
        if (nextComboTarget > 0L)
        {
            candidates.Add(new PlayerInsightContract(
                AchievementDefinitions.Insights.BestComboNextGoal,
                lifetimeStats.BestCombo,
                nextComboTarget));
        }

        // 5. most_active_week
        if (currentWeek.TotalLinesCleared > previousHistoricalBestLines)
        {
            candidates.Add(new PlayerInsightContract(
                AchievementDefinitions.Insights.MostActiveWeek,
                currentWeek.TotalLinesCleared,
                previousHistoricalBestLines));
        }

        // 6. personal_best_break_count
        if (lifetimeStats.PersonalBestBreakCount > 0L)
        {
            candidates.Add(new PlayerInsightContract(
                AchievementDefinitions.Insights.PersonalBestBreakCount,
                lifetimeStats.PersonalBestBreakCount,
                0.0));
        }

        // 7. recent_average fallback
        if (recentGames.Count > 0)
        {
            candidates.Add(new PlayerInsightContract(
                AchievementDefinitions.Insights.RecentAverage,
                recentGames.Average(r => (double)r.Score),
                0.0));
        }

        return candidates.Take(PlayerDataConstants.MaxInsights).ToList();
    }

    private static long FindNextComboTarget(int currentBestCombo, List<AchievementProgressContract> achievements)
    {
        long[] comboTargets = [3L, 10L, 15L, 17L];
        for (int i = 0; i < comboTargets.Length; i++)
        {
            if (comboTargets[i] > currentBestCombo)
            {
                return comboTargets[i];
            }
        }
        return 0L;
    }
}
