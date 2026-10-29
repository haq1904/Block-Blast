using System;
using System.Collections.Generic;
using BlockBlast.Features.Lobby.GameCenter;
using BlockBlast.Features.PlayerData;
using NUnit.Framework;

namespace BlockBlast.Features.GameFlow.Navigation.Test
{
    [TestFixture]
    public sealed class GameCenterViewDataMapperTest
    {
        [Test]
        public void Map_NullSnapshot_ReturnsSafeEmptyViewData()
        {
            var data = GameCenterViewDataMapper.Map(null, statusOverride: "CustomOffline");

            Assert.IsNotNull(data);
            Assert.IsFalse(data.HasData);
            Assert.AreEqual("CustomOffline", data.SyncStatusText);
            Assert.AreEqual("0", data.BestScoreText);
            Assert.AreEqual("0", data.AverageScoreText);
            Assert.AreEqual("0.0", data.LinesPerGameText);
            Assert.AreEqual("0.0", data.LinesPerBlockText);
            Assert.AreEqual(7, data.RecentGames.Count);
            Assert.AreEqual(18, data.Achievements.Count);
            Assert.AreEqual("-", data.RecentGames[0].Label);
            Assert.AreEqual(0f, data.RecentGames[0].NormalizedFill);
        }

        [Test]
        public void Map_ValidSnapshot_MapsOverviewStatsAndCoreRatios()
        {
            var stats = new PlayerStats(
                gamesPlayed: 4,
                totalScore: 10000,
                bestScore: 3500,
                totalLinesCleared: 50,
                totalBlocksPlaced: 200,
                bestCombo: 5,
                personalBestBreakCount: 2);

            var currentWeek = new WeeklyPlayerStats(1000L, 4, 10000, 3500, 50, 200, 2);
            var prevWeek = new WeeklyPlayerStats(500L, 2, 4000, 2000, 20, 80, 1);

            var snapshot = new GameCenterSnapshot(
                revision: 10L,
                createdAtUnixSeconds: 1000L,
                updatedAtUnixSeconds: 1000L,
                lifetimeStats: stats,
                currentWeek: currentWeek,
                previousWeek: prevWeek,
                bestWeekLinesCleared: 50L,
                recentGames: Array.Empty<RecentGameResult>(),
                achievements: Array.Empty<AchievementProgress>(),
                insights: Array.Empty<PlayerInsight>(),
                schemaVersion: PlayerDataSchema.CurrentVersion);

            var data = GameCenterViewDataMapper.Map(snapshot);

            Assert.IsTrue(data.HasData);
            Assert.AreEqual("3,500", data.BestScoreText);
            Assert.AreEqual("4", data.TotalGamesText);
            Assert.AreEqual("50", data.TotalLinesText);
            Assert.AreEqual("200", data.TotalBlocksText);
            Assert.AreEqual("5", data.BestComboText);
            Assert.AreEqual("2,500", data.AverageScoreText); // 10000 / 4
            Assert.AreEqual("12.5", data.LinesPerGameText); // 50 / 4
            Assert.AreEqual("0.3", data.LinesPerBlockText); // 50 / 200 = 0.25 -> 0.3
            Assert.AreEqual("10,000", data.CurrentWeekScoreText);
            Assert.AreEqual("4,000", data.PreviousWeekScoreText);
            Assert.AreEqual("Synced", data.SyncStatusText);
            Assert.AreEqual("New personal best this week", data.PerformanceTrendText);
        }

        [Test]
        public void Map_DivideByZeroGuarded_WhenZeroGamesPlayed()
        {
            var stats = new PlayerStats(0, 0, 0, 0, 0, 0, 0);
            var snapshot = new GameCenterSnapshot(
                revision: 0L,
                createdAtUnixSeconds: 1000L,
                updatedAtUnixSeconds: 1000L,
                lifetimeStats: stats,
                currentWeek: null,
                previousWeek: null,
                bestWeekLinesCleared: 0L,
                recentGames: Array.Empty<RecentGameResult>(),
                achievements: Array.Empty<AchievementProgress>(),
                insights: Array.Empty<PlayerInsight>(),
                schemaVersion: PlayerDataSchema.CurrentVersion);

            var data = GameCenterViewDataMapper.Map(snapshot);

            Assert.AreEqual("0", data.AverageScoreText);
            Assert.AreEqual("0", data.BestScoreText);
            Assert.AreEqual("0.0", data.LinesPerGameText);
            Assert.AreEqual("0.0", data.LinesPerBlockText);
        }

        [Test]
        public void Map_RecentGames_NormalizesByRecentWindowMax_NotLifetimeBest()
        {
            // Lifetime best is 10,000, but recent games max is 1,000
            var stats = new PlayerStats(3, 1500, 10000, 10, 30, 2, 1);
            var recent = new[]
            {
                new RecentGameResult("s1", 200, 100L),
                new RecentGameResult("s2", 500, 200L),
                new RecentGameResult("s3", 1000, 300L)
            };

            var snapshot = new GameCenterSnapshot(
                revision: 1L,
                createdAtUnixSeconds: 1000L,
                updatedAtUnixSeconds: 1000L,
                lifetimeStats: stats,
                currentWeek: null,
                previousWeek: null,
                bestWeekLinesCleared: 10L,
                recentGames: recent,
                achievements: Array.Empty<AchievementProgress>(),
                insights: Array.Empty<PlayerInsight>(),
                schemaVersion: PlayerDataSchema.CurrentVersion);

            var data = GameCenterViewDataMapper.Map(snapshot);

            Assert.AreEqual(7, data.RecentGames.Count);
            // 3 games are filled into slots 0, 1, 2
            Assert.AreEqual(200, data.RecentGames[0].Score);
            Assert.AreEqual("200", data.RecentGames[0].Label);
            Assert.AreEqual(0.2f, data.RecentGames[0].NormalizedFill, 0.001f); // 200 / 1000

            Assert.AreEqual(500, data.RecentGames[1].Score);
            Assert.AreEqual(0.5f, data.RecentGames[1].NormalizedFill, 0.001f); // 500 / 1000

            Assert.AreEqual(1000, data.RecentGames[2].Score);
            Assert.AreEqual(1.0f, data.RecentGames[2].NormalizedFill, 0.001f); // 1000 / 1000

            // Remaining slots 3..6 are padded with score 0, fill 0f, label "-"
            Assert.AreEqual(0, data.RecentGames[3].Score);
            Assert.AreEqual(0f, data.RecentGames[3].NormalizedFill);
            Assert.AreEqual("-", data.RecentGames[3].Label);
        }

        [Test]
        public void Map_InsightGate_ShowsRemainingGames_WhenLessThanFiveGamesPlayed()
        {
            var stats = new PlayerStats(2, 500, 300, 5, 20, 1, 0);
            var snapshot = new GameCenterSnapshot(
                revision: 1L,
                createdAtUnixSeconds: 1000L,
                updatedAtUnixSeconds: 1000L,
                lifetimeStats: stats,
                currentWeek: null,
                previousWeek: null,
                bestWeekLinesCleared: 5L,
                recentGames: Array.Empty<RecentGameResult>(),
                achievements: Array.Empty<AchievementProgress>(),
                insights: Array.Empty<PlayerInsight>(),
                schemaVersion: PlayerDataSchema.CurrentVersion);

            var data = GameCenterViewDataMapper.Map(snapshot);

            Assert.AreEqual(1, data.Highlights.Count);
            Assert.AreEqual("Play Style", data.Highlights[0].Title);
            Assert.IsTrue(data.Highlights[0].Description.Contains("Play 3 more games to discover your play style."));
        }

        [Test]
        public void Map_InsightGate_MapsServerInsights_WhenFiveOrMoreGamesPlayed()
        {
            var stats = new PlayerStats(6, 12000, 4000, 30, 100, 4, 1);
            var insights = new[]
            {
                new PlayerInsight(AchievementCatalog.Insights.WeeklyAverageChange, 25.0, 0.0),
                new PlayerInsight(AchievementCatalog.Insights.BestComboNextGoal, 8.0, 4.0)
            };

            var snapshot = new GameCenterSnapshot(
                revision: 2L,
                createdAtUnixSeconds: 1000L,
                updatedAtUnixSeconds: 1000L,
                lifetimeStats: stats,
                currentWeek: null,
                previousWeek: null,
                bestWeekLinesCleared: 30L,
                recentGames: Array.Empty<RecentGameResult>(),
                achievements: Array.Empty<AchievementProgress>(),
                insights: insights,
                schemaVersion: PlayerDataSchema.CurrentVersion);

            var data = GameCenterViewDataMapper.Map(snapshot);

            Assert.AreEqual(2, data.Highlights.Count);
            Assert.AreEqual("Weekly Momentum", data.Highlights[0].Title);
            Assert.AreEqual("Combo Goal", data.Highlights[1].Title);
        }

        [Test]
        public void Map_Achievements_MapsAllEighteenCatalogDefinitions_AndChecksServerUnlockTime()
        {
            var achievements = new[]
            {
                // Unlocked (has UnlockedAtUnixSeconds > 0)
                new AchievementProgress(AchievementCatalog.ScoreMaster, 500000L, 500000L, 1700000000L),
                // Current value reached target, but server has NOT marked unlocked (UnlockedAtUnixSeconds == 0)
                new AchievementProgress(AchievementCatalog.ComboCollector, 15L, 15L, 0L)
            };

            var snapshot = new GameCenterSnapshot(
                revision: 1L,
                createdAtUnixSeconds: 1000L,
                updatedAtUnixSeconds: 1000L,
                lifetimeStats: new PlayerStats(1, 100, 100, 1, 5, 1, 0),
                currentWeek: null,
                previousWeek: null,
                bestWeekLinesCleared: 1L,
                recentGames: Array.Empty<RecentGameResult>(),
                achievements: achievements,
                insights: Array.Empty<PlayerInsight>(),
                schemaVersion: PlayerDataSchema.CurrentVersion);

            var data = GameCenterViewDataMapper.Map(snapshot);

            Assert.AreEqual(18, data.Achievements.Count);

            // Find Score Master
            var scoreMaster = FindAchievement(data.Achievements, AchievementCatalog.ScoreMaster);
            Assert.IsNotNull(scoreMaster);
            Assert.AreEqual("Score Master", scoreMaster.Value.Title);
            Assert.AreEqual("COMPLETED", scoreMaster.Value.ProgressText);
            Assert.AreEqual(1.0f, scoreMaster.Value.NormalizedFill, 0.001f);
            Assert.IsTrue(scoreMaster.Value.IsUnlocked);

            // Find Combo Collector -> NOT unlocked because UnlockedAtUnixSeconds == 0
            var comboCollector = FindAchievement(data.Achievements, AchievementCatalog.ComboCollector);
            Assert.IsNotNull(comboCollector);
            Assert.AreEqual("Combo Collector", comboCollector.Value.Title);
            Assert.AreEqual("15/15", comboCollector.Value.ProgressText);
            Assert.AreEqual(1.0f, comboCollector.Value.NormalizedFill, 0.001f);
            Assert.IsFalse(comboCollector.Value.IsUnlocked);
        }

        [Test]
        public void Map_StatusText_ShowsPendingCount_WhenPendingCountPositive()
        {
            var snapshot = new GameCenterSnapshot(
                revision: 1L,
                createdAtUnixSeconds: 1000L,
                updatedAtUnixSeconds: 1000L,
                lifetimeStats: new PlayerStats(1, 100, 100, 1, 5, 1, 0),
                currentWeek: null,
                previousWeek: null,
                bestWeekLinesCleared: 1L,
                recentGames: Array.Empty<RecentGameResult>(),
                achievements: Array.Empty<AchievementProgress>(),
                insights: Array.Empty<PlayerInsight>(),
                schemaVersion: PlayerDataSchema.CurrentVersion);

            var data = GameCenterViewDataMapper.Map(snapshot, pendingCount: 3);

            Assert.AreEqual("Offline (3 pending)", data.SyncStatusText);
        }

        private static AchievementRowViewData? FindAchievement(IReadOnlyList<AchievementRowViewData> list, string id)
        {
            foreach (var item in list)
            {
                if (string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase))
                {
                    return item;
                }
            }
            return null;
        }
    }
}
