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
            Assert.AreEqual(7, data.RecentGames.Count);
            Assert.AreEqual(3, data.Achievements.Count);
            Assert.AreEqual("-", data.RecentGames[0].Label);
            Assert.AreEqual(0f, data.RecentGames[0].NormalizedFill);
        }

        [Test]
        public void Map_ValidSnapshot_MapsOverviewStatsAndAverages()
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
            Assert.AreEqual("5", data.BestComboText);
            Assert.AreEqual("2,500", data.AverageScoreText); // 10000 / 4
            Assert.AreEqual("10,000", data.CurrentWeekScoreText);
            Assert.AreEqual("4,000", data.PreviousWeekScoreText);
            Assert.AreEqual("Synced", data.SyncStatusText);
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
        }

        [Test]
        public void Map_RecentGames_PadsToSevenSlots_AndNormalizesFill()
        {
            var stats = new PlayerStats(3, 1500, 1000, 10, 30, 2, 1);
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
            Assert.AreEqual(0.2f, data.RecentGames[0].NormalizedFill, 0.001f);

            Assert.AreEqual(500, data.RecentGames[1].Score);
            Assert.AreEqual(0.5f, data.RecentGames[1].NormalizedFill, 0.001f);

            Assert.AreEqual(1000, data.RecentGames[2].Score);
            Assert.AreEqual(1.0f, data.RecentGames[2].NormalizedFill, 0.001f);

            // Remaining slots 3..6 are padded with score 0, fill 0f, label "-"
            Assert.AreEqual(0, data.RecentGames[3].Score);
            Assert.AreEqual(0f, data.RecentGames[3].NormalizedFill);
            Assert.AreEqual("-", data.RecentGames[3].Label);
        }

        [Test]
        public void Map_Achievements_MapsCatalogDefinitions_AndClampsFill()
        {
            var achievements = new[]
            {
                new AchievementProgress(AchievementCatalog.ScoreMaster, 250000L, 500000L, 0L),
                new AchievementProgress(AchievementCatalog.ComboCollector, 15L, 15L, 1000L)
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

            Assert.AreEqual(3, data.Achievements.Count);

            // Row 0: Score Master
            Assert.AreEqual(AchievementCatalog.ScoreMaster, data.Achievements[0].Id);
            Assert.AreEqual("Score Master", data.Achievements[0].Title);
            Assert.AreEqual("250,000/500,000", data.Achievements[0].ProgressText);
            Assert.AreEqual(0.5f, data.Achievements[0].NormalizedFill, 0.001f);
            Assert.IsFalse(data.Achievements[0].IsUnlocked);

            // Row 1: Combo Collector
            Assert.AreEqual(AchievementCatalog.ComboCollector, data.Achievements[1].Id);
            Assert.AreEqual("COMPLETED", data.Achievements[1].ProgressText);
            Assert.AreEqual(1.0f, data.Achievements[1].NormalizedFill, 0.001f);
            Assert.IsTrue(data.Achievements[1].IsUnlocked);

            // Row 2: Line Cleaner 3 (missing in snapshot -> defaulted)
            Assert.AreEqual(AchievementCatalog.LineCleaner3, data.Achievements[2].Id);
            Assert.AreEqual("Line Cleaner III", data.Achievements[2].Title);
            Assert.AreEqual("0/1,000", data.Achievements[2].ProgressText);
            Assert.AreEqual(0f, data.Achievements[2].NormalizedFill);
            Assert.IsFalse(data.Achievements[2].IsUnlocked);
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
    }
}
