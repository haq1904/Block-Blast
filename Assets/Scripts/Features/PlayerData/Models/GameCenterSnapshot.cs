using System;
using System.Collections.Generic;

namespace BlockBlast.Features.PlayerData
{
    public sealed class GameCenterSnapshot
    {
        public const int MaxRecentGames = 7;
        public const int MaxInsights = 2;

        public int SchemaVersion { get; }
        public long Revision { get; }
        public long CreatedAtUnixSeconds { get; }
        public long UpdatedAtUnixSeconds { get; }
        public PlayerStats LifetimeStats { get; }
        public WeeklyPlayerStats CurrentWeek { get; }
        public WeeklyPlayerStats PreviousWeek { get; }
        public long BestWeekLinesCleared { get; }
        public IReadOnlyList<RecentGameResult> RecentGames { get; }
        public IReadOnlyList<AchievementProgress> Achievements { get; }
        public IReadOnlyList<PlayerInsight> Insights { get; }

        public GameCenterSnapshot(
            long revision,
            long createdAtUnixSeconds,
            long updatedAtUnixSeconds,
            PlayerStats lifetimeStats,
            WeeklyPlayerStats currentWeek,
            WeeklyPlayerStats previousWeek,
            long bestWeekLinesCleared,
            IEnumerable<RecentGameResult> recentGames,
            IEnumerable<AchievementProgress> achievements,
            IEnumerable<PlayerInsight> insights,
            int schemaVersion = PlayerDataSchema.CurrentVersion)
        {
            SchemaVersion = schemaVersion;
            Revision = revision;
            CreatedAtUnixSeconds = Math.Max(0L, createdAtUnixSeconds);
            UpdatedAtUnixSeconds = Math.Max(0L, updatedAtUnixSeconds);
            LifetimeStats = lifetimeStats ?? new PlayerStats(0, 0, 0, 0, 0, 0, 0);
            CurrentWeek = currentWeek ?? new WeeklyPlayerStats(0, 0, 0, 0, 0, 0, 0);
            PreviousWeek = previousWeek ?? new WeeklyPlayerStats(0, 0, 0, 0, 0, 0, 0);
            BestWeekLinesCleared = Math.Max(0L, bestWeekLinesCleared);

            var recentList = new List<RecentGameResult>();
            if (recentGames != null)
            {
                foreach (var r in recentGames)
                {
                    if (r != null) recentList.Add(r);
                }
            }
            if (recentList.Count > MaxRecentGames)
            {
                recentList = recentList.GetRange(recentList.Count - MaxRecentGames, MaxRecentGames);
            }
            RecentGames = recentList.AsReadOnly();

            var achList = new List<AchievementProgress>();
            if (achievements != null)
            {
                foreach (var a in achievements)
                {
                    if (a != null) achList.Add(a);
                }
            }
            Achievements = achList.AsReadOnly();

            var insightList = new List<PlayerInsight>();
            if (insights != null)
            {
                foreach (var i in insights)
                {
                    if (i != null) insightList.Add(i);
                }
            }
            if (insightList.Count > MaxInsights)
            {
                insightList = insightList.GetRange(0, MaxInsights);
            }
            Insights = insightList.AsReadOnly();
        }
    }
}
