using System;
using System.Collections.Generic;
using BlockBlast.Features.PlayerData;
using UnityEngine;

namespace BlockBlast.Features.Lobby.GameCenter
{
    public static class GameCenterViewDataMapper
    {
        public const int RecentGamesCount = 7;

        public static readonly (string Id, string Title, long Target)[] HighlightedAchievements =
        {
            (AchievementCatalog.ScoreMaster, "Score Master", 500000L),
            (AchievementCatalog.ComboCollector, "Combo Collector", 15L),
            (AchievementCatalog.LineCleaner3, "Line Cleaner III", 1000L)
        };

        public static GameCenterViewData CreateEmptyViewData(
            GameCenterPresentationState state = GameCenterPresentationState.Empty,
            string statusText = "")
        {
            var bars = new RecentGameBarViewData[RecentGamesCount];
            for (int i = 0; i < RecentGamesCount; i++)
            {
                bars[i] = new RecentGameBarViewData(0, 0f, "-");
            }

            var achMetas = GameCenterPresentationCatalog.Achievements;
            var achievements = new AchievementRowViewData[achMetas.Count];
            for (int i = 0; i < achMetas.Count; i++)
            {
                var meta = achMetas[i];
                achievements[i] = new AchievementRowViewData(meta.Id, meta.Title, $"0/{meta.DefaultTarget:N0}", 0f, false);
            }

            var highlights = new List<InsightCardViewData>
            {
                new InsightCardViewData("style_discovery", "Play Style", "Play 5 more games to discover your play style.")
            };

            return new GameCenterViewData(
                state: state,
                bestScoreText: "0",
                averageScoreText: "0",
                gamesPlayedText: "0",
                performanceTrendText: string.Empty,
                totalLinesText: "0",
                totalBlocksText: "0",
                bestComboText: "0",
                linesPerGameText: "0.0",
                linesPerBlockText: "0.0",
                recentInsightText: "No recent games",
                highlights: highlights,
                recentGames: bars,
                achievements: achievements,
                syncStatusText: statusText ?? string.Empty,
                hasData: false,
                currentWeekScoreText: "0",
                previousWeekScoreText: "0");
        }

        public static GameCenterViewData Map(
            GameCenterSnapshot snapshot,
            string statusOverride = null,
            int pendingCount = 0)
        {
            var state = snapshot != null && (snapshot.Revision > 0 || (snapshot.LifetimeStats != null && snapshot.LifetimeStats.GamesPlayed > 0))
                ? GameCenterPresentationState.Ready
                : GameCenterPresentationState.Empty;

            return Map(snapshot, state, statusOverride, pendingCount);
        }

        public static GameCenterViewData Map(
            GameCenterSnapshot snapshot,
            GameCenterPresentationState state,
            string statusOverride = null,
            int pendingCount = 0)
        {
            if (snapshot == null)
            {
                var emptyState = state == GameCenterPresentationState.Ready ? GameCenterPresentationState.Empty : state;
                return CreateEmptyViewData(emptyState, statusOverride);
            }

            var stats = snapshot.LifetimeStats;
            long gamesPlayed = stats != null ? stats.GamesPlayed : 0L;
            long totalScore = stats != null ? stats.TotalScore : 0L;
            int bestScore = stats != null ? stats.BestScore : 0;
            long totalLines = stats != null ? stats.TotalLinesCleared : 0L;
            long totalBlocks = stats != null ? stats.TotalBlocksPlaced : 0L;
            int bestCombo = stats != null ? stats.BestCombo : 0;

            long avgScore = gamesPlayed > 0 ? (totalScore / gamesPlayed) : 0L;
            double linesPerGame = gamesPlayed > 0 ? ((double)totalLines / gamesPlayed) : 0.0;
            double linesPerBlock = totalBlocks > 0 ? ((double)totalLines / totalBlocks) : 0.0;

            string bestScoreText = bestScore.ToString("N0");
            string gamesPlayedText = gamesPlayed.ToString("N0");
            string totalLinesText = totalLines.ToString("N0");
            string totalBlocksText = totalBlocks.ToString("N0");
            string bestComboText = bestCombo.ToString("N0");
            string avgScoreText = avgScore.ToString("N0");
            string linesPerGameText = linesPerGame.ToString("F1");
            string linesPerBlockText = linesPerBlock.ToString("F1");

            long currentWeekScore = snapshot.CurrentWeek != null ? snapshot.CurrentWeek.TotalScore : 0L;
            long prevWeekScore = snapshot.PreviousWeek != null ? snapshot.PreviousWeek.TotalScore : 0L;
            long currentWeekBest = snapshot.CurrentWeek != null ? snapshot.CurrentWeek.BestScore : 0L;
            long currentWeekGames = snapshot.CurrentWeek != null ? snapshot.CurrentWeek.GamesPlayed : 0L;
            long prevWeekGames = snapshot.PreviousWeek != null ? snapshot.PreviousWeek.GamesPlayed : 0L;

            string currentWeekText = currentWeekScore.ToString("N0");
            string prevWeekText = prevWeekScore.ToString("N0");

            // Performance trend calculation
            string performanceTrendText = string.Empty;
            if (currentWeekBest > 0 && currentWeekBest >= bestScore && gamesPlayed > 0)
            {
                performanceTrendText = "New personal best this week";
            }
            else if (prevWeekGames > 0 && currentWeekGames > 0)
            {
                long currAvg = currentWeekScore / currentWeekGames;
                long prevAvg = prevWeekScore / prevWeekGames;
                if (prevAvg > 0)
                {
                    double pct = ((double)(currAvg - prevAvg) / prevAvg) * 100.0;
                    performanceTrendText = $"{(pct >= 0 ? "+" : "")}{pct:0}% vs last week";
                }
            }

            // Recent games chart normalization (normalized to max of recent window)
            var recentResults = snapshot.RecentGames;
            int count = recentResults != null ? recentResults.Count : 0;
            int take = Math.Min(count, RecentGamesCount);
            int startIndex = count - take;

            int maxRecentScore = 1;
            for (int i = 0; i < take; i++)
            {
                int s = recentResults[startIndex + i].Score;
                if (s > maxRecentScore)
                {
                    maxRecentScore = s;
                }
            }

            var bars = new RecentGameBarViewData[RecentGamesCount];
            long recentSum = 0;
            for (int slot = 0; slot < RecentGamesCount; slot++)
            {
                if (slot < take)
                {
                    var game = recentResults[startIndex + slot];
                    int score = game.Score;
                    recentSum += score;
                    float fill = maxRecentScore > 0 ? Mathf.Clamp01((float)score / maxRecentScore) : 0f;
                    string label = score > 0 ? score.ToString("N0") : "0";
                    bars[slot] = new RecentGameBarViewData(score, fill, label);
                }
                else
                {
                    bars[slot] = new RecentGameBarViewData(0, 0f, "-");
                }
            }

            // Recent insight text
            string recentInsightText = "No recent games";
            if (take > 0)
            {
                long recentAvg = recentSum / take;
                if (avgScore > 0)
                {
                    double diff = ((double)(recentAvg - avgScore) / avgScore) * 100.0;
                    recentInsightText = $"Recent avg: {recentAvg:N0} ({(diff >= 0 ? "+" : "")}{diff:0}% vs lifetime)";
                }
                else
                {
                    recentInsightText = $"Recent avg: {recentAvg:N0}";
                }
            }

            // Highlights mapping (gated to >= 5 games)
            var highlights = new List<InsightCardViewData>();
            if (gamesPlayed < 5)
            {
                long remaining = Math.Max(1L, 5L - gamesPlayed);
                highlights.Add(new InsightCardViewData("style_discovery", "Play Style", $"Play {remaining} more games to discover your play style."));
            }
            else
            {
                if (snapshot.Insights != null && snapshot.Insights.Count > 0)
                {
                    int maxInsights = Math.Min(snapshot.Insights.Count, GameCenterSnapshot.MaxInsights);
                    for (int i = 0; i < maxInsights; i++)
                    {
                        var ins = snapshot.Insights[i];
                        highlights.Add(MapInsightCard(ins));
                    }
                }
                else
                {
                    highlights.Add(new InsightCardViewData("veteran", "Steady Progress", "Keep playing to unlock advanced play style insights."));
                }
            }

            // 18 Achievements mapped in deterministic catalog order
            var achMetas = GameCenterPresentationCatalog.Achievements;
            var achievements = new AchievementRowViewData[achMetas.Count];
            var lookup = new Dictionary<string, AchievementProgress>(StringComparer.OrdinalIgnoreCase);
            if (snapshot.Achievements != null)
            {
                for (int i = 0; i < snapshot.Achievements.Count; i++)
                {
                    var a = snapshot.Achievements[i];
                    if (a != null && !string.IsNullOrEmpty(a.AchievementId))
                    {
                        lookup[a.AchievementId] = a;
                    }
                }
            }

            for (int i = 0; i < achMetas.Count; i++)
            {
                var meta = achMetas[i];
                if (lookup.TryGetValue(meta.Id, out var progress))
                {
                    long target = progress.TargetValue > 0 ? progress.TargetValue : meta.DefaultTarget;
                    long current = progress.CurrentValue;
                    bool isUnlocked = progress.IsUnlocked;
                    float fill = target > 0 ? Mathf.Clamp01((float)current / target) : (isUnlocked ? 1f : 0f);
                    string progText = isUnlocked ? "COMPLETED" : $"{current:N0}/{target:N0}";
                    achievements[i] = new AchievementRowViewData(meta.Id, meta.Title, progText, fill, isUnlocked);
                }
                else
                {
                    achievements[i] = new AchievementRowViewData(meta.Id, meta.Title, $"0/{meta.DefaultTarget:N0}", 0f, false);
                }
            }

            string syncStatus = statusOverride;
            if (string.IsNullOrEmpty(syncStatus))
            {
                if (pendingCount > 0)
                {
                    syncStatus = $"Offline ({pendingCount} pending)";
                }
                else
                {
                    syncStatus = "Synced";
                }
            }

            bool hasData = snapshot.Revision > 0 || gamesPlayed > 0 || bestScore > 0;
            var finalState = state;
            if (finalState == GameCenterPresentationState.Ready && !hasData)
            {
                finalState = GameCenterPresentationState.Empty;
            }

            return new GameCenterViewData(
                state: finalState,
                bestScoreText: bestScoreText,
                averageScoreText: avgScoreText,
                gamesPlayedText: gamesPlayedText,
                performanceTrendText: performanceTrendText,
                totalLinesText: totalLinesText,
                totalBlocksText: totalBlocksText,
                bestComboText: bestComboText,
                linesPerGameText: linesPerGameText,
                linesPerBlockText: linesPerBlockText,
                recentInsightText: recentInsightText,
                highlights: highlights,
                recentGames: bars,
                achievements: achievements,
                syncStatusText: syncStatus,
                hasData: hasData,
                currentWeekScoreText: currentWeekText,
                previousWeekScoreText: prevWeekText);
        }

        private static InsightCardViewData MapInsightCard(PlayerInsight insight)
        {
            if (insight == null)
            {
                return new InsightCardViewData(string.Empty, "Insight", string.Empty);
            }

            switch (insight.InsightId)
            {
                case AchievementCatalog.Insights.NewPersonalBestThisWeek:
                    return new InsightCardViewData(insight.InsightId, "New Record", "New personal best score achieved this week!");
                case AchievementCatalog.Insights.WeeklyAverageChange:
                    return new InsightCardViewData(insight.InsightId, "Weekly Momentum", $"{(insight.CurrentValue >= 0 ? "+" : "")}{insight.CurrentValue:0}% average score vs last week");
                case AchievementCatalog.Insights.RecentAverage:
                    return new InsightCardViewData(insight.InsightId, "Recent Average", $"Averaging {insight.CurrentValue:N0} in recent matches");
                case AchievementCatalog.Insights.RecentVsLifetimeAverage:
                    return new InsightCardViewData(insight.InsightId, "Form Comparison", $"{(insight.CurrentValue >= 0 ? "+" : "")}{insight.CurrentValue:0}% vs lifetime average");
                case AchievementCatalog.Insights.BestComboNextGoal:
                    return new InsightCardViewData(insight.InsightId, "Combo Goal", $"Aiming for {insight.CurrentValue:0}x combo");
                case AchievementCatalog.Insights.MostActiveWeek:
                    return new InsightCardViewData(insight.InsightId, "Peak Week", $"{insight.CurrentValue:N0} games played in your most active week");
                case AchievementCatalog.Insights.PersonalBestBreakCount:
                    return new InsightCardViewData(insight.InsightId, "Record Breaker", $"Broken personal record {insight.CurrentValue:N0} times");
                default:
                    return new InsightCardViewData(insight.InsightId, "Player Insight", $"{insight.CurrentValue:N0}");
            }
        }
    }
}
