using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace BlockBlast.Features.PlayerData.Test
{
    [TestFixture]
    public class PlayerDataModelTest
    {
        [Test]
        public void GameResult_ConstructsCorrectly_AndValidatesInvariants()
        {
            var result = new GameResult("session_123", 100, 4, 12, 3, 1700000000L);

            Assert.AreEqual(PlayerDataSchema.CurrentVersion, result.SchemaVersion);
            Assert.AreEqual("session_123", result.GameSessionId);
            Assert.AreEqual(100, result.Score);
            Assert.AreEqual(4, result.LinesCleared);
            Assert.AreEqual(12, result.BlocksPlaced);
            Assert.AreEqual(3, result.BestCombo);
            Assert.AreEqual(1700000000L, result.CompletedAtUnixSeconds);

            Assert.Throws<ArgumentException>(() => new GameResult(null, 0, 0, 0, 0, 0));
            Assert.Throws<ArgumentException>(() => new GameResult("", 0, 0, 0, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new GameResult("s", -1, 0, 0, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new GameResult("s", 0, -1, 0, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new GameResult("s", 0, 0, -1, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new GameResult("s", 0, 0, 0, -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new GameResult("s", 0, 0, 0, 0, -1));
        }

        [Test]
        public void PlayerStats_And_WeeklyStats_ClampNegatives()
        {
            var stats = new PlayerStats(-10, -5, -1, -2, -3, -4, -1);
            Assert.AreEqual(0L, stats.GamesPlayed);
            Assert.AreEqual(0L, stats.TotalScore);
            Assert.AreEqual(0, stats.BestScore);
            Assert.AreEqual(0L, stats.TotalLinesCleared);
            Assert.AreEqual(0L, stats.TotalBlocksPlaced);
            Assert.AreEqual(0, stats.BestCombo);
            Assert.AreEqual(0L, stats.PersonalBestBreakCount);

            var weekly = new WeeklyPlayerStats(1000L, -1, -1, -1, -1, -1, -1);
            Assert.AreEqual(1000L, weekly.WeekStartUnixSeconds);
            Assert.AreEqual(0L, weekly.GamesPlayed);
        }

        [Test]
        public void AchievementProgress_ValidatesFields()
        {
            var p = new AchievementProgress("score_starter", 5000, 10000, 0);
            Assert.AreEqual("score_starter", p.AchievementId);
            Assert.AreEqual(5000, p.CurrentValue);
            Assert.AreEqual(10000, p.TargetValue);
            Assert.IsFalse(p.IsUnlocked);

            var unlocked = new AchievementProgress("score_starter", 10000, 10000, 1700000000L);
            Assert.IsTrue(unlocked.IsUnlocked);

            Assert.Throws<ArgumentException>(() => new AchievementProgress(null, 0, 0));
        }

        [Test]
        public void GameCenterSnapshot_EnforcesMaxRecentGames_AndMaxInsights()
        {
            var recent = new List<RecentGameResult>();
            for (int i = 0; i < 10; i++)
            {
                recent.Add(new RecentGameResult("s_" + i, i * 10, 1700000000L + i));
            }

            var insights = new List<PlayerInsight>
            {
                new PlayerInsight("i1", 1, 0),
                new PlayerInsight("i2", 2, 1),
                new PlayerInsight("i3", 3, 2)
            };

            var snapshot = new GameCenterSnapshot(
                1L, 100L, 200L, null, null, null, 15L, recent, null, insights);

            Assert.AreEqual(GameCenterSnapshot.MaxRecentGames, snapshot.RecentGames.Count);
            // Must keep the most recent 7 (s_3 to s_9)
            Assert.AreEqual("s_3", snapshot.RecentGames[0].GameSessionId);
            Assert.AreEqual("s_9", snapshot.RecentGames[6].GameSessionId);

            Assert.AreEqual(GameCenterSnapshot.MaxInsights, snapshot.Insights.Count);
            Assert.AreEqual("i1", snapshot.Insights[0].InsightId);
            Assert.AreEqual("i2", snapshot.Insights[1].InsightId);
        }

        [Test]
        public void AchievementCatalog_ContainsExactly18Achievements()
        {
            Assert.AreEqual(18, AchievementCatalog.Definitions.Count);
            Assert.IsTrue(AchievementCatalog.Definitions.ContainsKey(AchievementCatalog.FirstSteps));
            Assert.AreEqual(5L, AchievementCatalog.Definitions[AchievementCatalog.FirstSteps]);
            Assert.IsTrue(AchievementCatalog.Definitions.ContainsKey(AchievementCatalog.NewHeights));
            Assert.AreEqual(1L, AchievementCatalog.Definitions[AchievementCatalog.NewHeights]);
        }
    }
}
