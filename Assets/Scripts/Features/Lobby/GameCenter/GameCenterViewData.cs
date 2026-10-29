using System;
using System.Collections.Generic;

namespace BlockBlast.Features.Lobby.GameCenter
{
    public enum GameCenterPresentationState
    {
        Loading,
        Ready,
        Empty,
        OfflineCached,
        Error
    }

    [Serializable]
    public readonly struct InsightCardViewData
    {
        public string InsightId { get; }
        public string Title { get; }
        public string Description { get; }

        public InsightCardViewData(string insightId, string title, string description)
        {
            InsightId = insightId ?? string.Empty;
            Title = title ?? string.Empty;
            Description = description ?? string.Empty;
        }
    }

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
        public GameCenterPresentationState State { get; }
        public string BestScoreText { get; }
        public string AverageScoreText { get; }
        public string GamesPlayedText { get; }
        public string TotalGamesText => GamesPlayedText;
        public string PerformanceTrendText { get; }
        public string TotalLinesText { get; }
        public string TotalBlocksText { get; }
        public string BestComboText { get; }
        public string LinesPerGameText { get; }
        public string LinesPerBlockText { get; }
        public string RecentInsightText { get; }
        public IReadOnlyList<InsightCardViewData> Highlights { get; }
        public IReadOnlyList<RecentGameBarViewData> RecentGames { get; }
        public IReadOnlyList<AchievementRowViewData> Achievements { get; }
        public string SyncStatusText { get; }
        public bool HasData { get; }
        public string CurrentWeekScoreText { get; }
        public string PreviousWeekScoreText { get; }

        public GameCenterViewData(
            GameCenterPresentationState state,
            string bestScoreText,
            string averageScoreText,
            string gamesPlayedText,
            string performanceTrendText,
            string totalLinesText,
            string totalBlocksText,
            string bestComboText,
            string linesPerGameText,
            string linesPerBlockText,
            string recentInsightText,
            IReadOnlyList<InsightCardViewData> highlights,
            IReadOnlyList<RecentGameBarViewData> recentGames,
            IReadOnlyList<AchievementRowViewData> achievements,
            string syncStatusText,
            bool hasData,
            string currentWeekScoreText = "0",
            string previousWeekScoreText = "0")
        {
            State = state;
            BestScoreText = bestScoreText ?? "0";
            AverageScoreText = averageScoreText ?? "0";
            GamesPlayedText = gamesPlayedText ?? "0";
            PerformanceTrendText = performanceTrendText ?? string.Empty;
            TotalLinesText = totalLinesText ?? "0";
            TotalBlocksText = totalBlocksText ?? "0";
            BestComboText = bestComboText ?? "0";
            LinesPerGameText = linesPerGameText ?? "0.0";
            LinesPerBlockText = linesPerBlockText ?? "0.0";
            RecentInsightText = recentInsightText ?? string.Empty;
            Highlights = highlights ?? Array.Empty<InsightCardViewData>();
            RecentGames = recentGames ?? Array.Empty<RecentGameBarViewData>();
            Achievements = achievements ?? Array.Empty<AchievementRowViewData>();
            SyncStatusText = syncStatusText ?? string.Empty;
            HasData = hasData;
            CurrentWeekScoreText = currentWeekScoreText ?? "0";
            PreviousWeekScoreText = previousWeekScoreText ?? "0";
        }

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
            : this(
                hasData ? GameCenterPresentationState.Ready : GameCenterPresentationState.Empty,
                bestScoreText,
                averageScoreText,
                totalGamesText,
                string.Empty,
                totalLinesText,
                "0",
                bestComboText,
                "0.0",
                "0.0",
                string.Empty,
                Array.Empty<InsightCardViewData>(),
                recentGames,
                achievements,
                syncStatusText,
                hasData,
                currentWeekScoreText,
                previousWeekScoreText)
        {
        }
    }
}
