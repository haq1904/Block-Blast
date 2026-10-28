using System;
using System.Collections.Generic;

namespace BlockBlast.Features.Lobby.GameCenter
{
    [Serializable]
    public readonly struct RecentGameBarViewData
    {
        public int Score { get; }
        public float NormalizedFill { get; }
        public string Label { get; }

        public RecentGameBarViewData(int score, float normalizedFill, string label)
        {
            Score = score;
            NormalizedFill = normalizedFill;
            Label = label ?? string.Empty;
        }
    }

    [Serializable]
    public readonly struct AchievementRowViewData
    {
        public string Id { get; }
        public string Title { get; }
        public string ProgressText { get; }
        public float NormalizedFill { get; }
        public bool IsUnlocked { get; }

        public AchievementRowViewData(string id, string title, string progressText, float normalizedFill, bool isUnlocked)
        {
            Id = id ?? string.Empty;
            Title = title ?? string.Empty;
            ProgressText = progressText ?? string.Empty;
            NormalizedFill = normalizedFill;
            IsUnlocked = isUnlocked;
        }
    }

    public sealed class GameCenterViewData
    {
        public string BestScoreText { get; }
        public string TotalGamesText { get; }
        public string TotalLinesText { get; }
        public string BestComboText { get; }
        public string AverageScoreText { get; }
        public string CurrentWeekScoreText { get; }
        public string PreviousWeekScoreText { get; }
        public IReadOnlyList<RecentGameBarViewData> RecentGames { get; }
        public IReadOnlyList<AchievementRowViewData> Achievements { get; }
        public string SyncStatusText { get; }
        public bool HasData { get; }

        public GameCenterViewData(
            string bestScoreText,
            string totalGamesText,
            string totalLinesText,
            string bestComboText,
            string averageScoreText,
            string currentWeekScoreText,
            string previousWeekScoreText,
            IReadOnlyList<RecentGameBarViewData> recentGames,
            IReadOnlyList<AchievementRowViewData> achievements,
            string syncStatusText,
            bool hasData)
        {
            BestScoreText = bestScoreText ?? "0";
            TotalGamesText = totalGamesText ?? "0";
            TotalLinesText = totalLinesText ?? "0";
            BestComboText = bestComboText ?? "0";
            AverageScoreText = averageScoreText ?? "0";
            CurrentWeekScoreText = currentWeekScoreText ?? "0";
            PreviousWeekScoreText = previousWeekScoreText ?? "0";
            RecentGames = recentGames ?? Array.Empty<RecentGameBarViewData>();
            Achievements = achievements ?? Array.Empty<AchievementRowViewData>();
            SyncStatusText = syncStatusText ?? string.Empty;
            HasData = hasData;
        }
    }
}
