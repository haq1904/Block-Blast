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

        public static GameCenterViewData CreateEmptyViewData(string statusText = "")
        {
            var bars = new RecentGameBarViewData[RecentGamesCount];
            for (int i = 0; i < RecentGamesCount; i++)
            {
                bars[i] = new RecentGameBarViewData(0, 0f, "-");
            }

            var achievements = new AchievementRowViewData[HighlightedAchievements.Length];
            for (int i = 0; i < HighlightedAchievements.Length; i++)
            {
                var (id, title, target) = HighlightedAchievements[i];
                achievements[i] = new AchievementRowViewData(id, title, $"0/{target:N0}", 0f, false);
            }

            return new GameCenterViewData(
                bestScoreText: "0",
                totalGamesText: "0",
                totalLinesText: "0",
                bestComboText: "0",
                averageScoreText: "0",
                currentWeekScoreText: "0",
                previousWeekScoreText: "0",
                recentGames: bars,
                achievements: achievements,
                syncStatusText: statusText ?? string.Empty,
                hasData: false);
        }

        public static GameCenterViewData Map(
            GameCenterSnapshot snapshot,
            string statusOverride = null,
            int pendingCount = 0)
        {
            if (snapshot == null)
            {
                return CreateEmptyViewData(statusOverride);
            }

            var stats = snapshot.LifetimeStats;
            long gamesPlayed = stats != null ? stats.GamesPlayed : 0L;
            long totalScore = stats != null ? stats.TotalScore : 0L;
            int bestScore = stats != null ? stats.BestScore : 0;
            long totalLines = stats != null ? stats.TotalLinesCleared : 0L;
            int bestCombo = stats != null ? stats.BestCombo : 0;

            long avgScore = gamesPlayed > 0 ? (totalScore / gamesPlayed) : 0L;

            string bestScoreText = bestScore.ToString("N0");
            string totalGamesText = gamesPlayed.ToString("N0");
            string totalLinesText = totalLines.ToString("N0");
            string bestComboText = bestCombo.ToString("N0");
            string avgScoreText = avgScore.ToString("N0");

            long currentWeekScore = snapshot.CurrentWeek != null ? snapshot.CurrentWeek.TotalScore : 0L;
            long prevWeekScore = snapshot.PreviousWeek != null ? snapshot.PreviousWeek.TotalScore : 0L;

            string currentWeekText = currentWeekScore.ToString("N0");
            string prevWeekText = prevWeekScore.ToString("N0");

            var recentResults = snapshot.RecentGames;
            int count = recentResults != null ? recentResults.Count : 0;

            int maxRecentScore = 1;
            if (bestScore > maxRecentScore)
            {
                maxRecentScore = bestScore;
            }

            if (recentResults != null)
            {
                for (int i = 0; i < count; i++)
                {
                    if (recentResults[i].Score > maxRecentScore)
                    {
                        maxRecentScore = recentResults[i].Score;
                    }
                }
            }

            var bars = new RecentGameBarViewData[RecentGamesCount];
            int take = Math.Min(count, RecentGamesCount);
            int startIndex = count - take;

            for (int slot = 0; slot < RecentGamesCount; slot++)
            {
                if (slot < take)
                {
                    var game = recentResults[startIndex + slot];
                    int score = game.Score;
                    float fill = maxRecentScore > 0 ? Mathf.Clamp01((float)score / maxRecentScore) : 0f;
                    string label = score > 0 ? score.ToString("N0") : "0";
                    bars[slot] = new RecentGameBarViewData(score, fill, label);
                }
                else
                {
                    bars[slot] = new RecentGameBarViewData(0, 0f, "-");
                }
            }

            var achievements = new AchievementRowViewData[HighlightedAchievements.Length];
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

            for (int i = 0; i < HighlightedAchievements.Length; i++)
            {
                var (id, defaultTitle, defaultTarget) = HighlightedAchievements[i];
                if (lookup.TryGetValue(id, out var progress))
                {
                    long target = progress.TargetValue > 0 ? progress.TargetValue : defaultTarget;
                    long current = progress.CurrentValue;
                    bool isUnlocked = progress.IsUnlocked || (target > 0 && current >= target);
                    float fill = target > 0 ? Mathf.Clamp01((float)current / target) : (isUnlocked ? 1f : 0f);
                    string progText = isUnlocked ? "COMPLETED" : $"{current:N0}/{target:N0}";
                    achievements[i] = new AchievementRowViewData(id, defaultTitle, progText, fill, isUnlocked);
                }
                else
                {
                    achievements[i] = new AchievementRowViewData(id, defaultTitle, $"0/{defaultTarget:N0}", 0f, false);
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

            return new GameCenterViewData(
                bestScoreText,
                totalGamesText,
                totalLinesText,
                bestComboText,
                avgScoreText,
                currentWeekText,
                prevWeekText,
                bars,
                achievements,
                syncStatus,
                hasData);
        }
    }
}
