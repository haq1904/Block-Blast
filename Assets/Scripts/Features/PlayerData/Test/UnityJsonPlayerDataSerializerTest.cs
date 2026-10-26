using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace BlockBlast.Features.PlayerData.Test
{
    [TestFixture]
    public class UnityJsonPlayerDataSerializerTest
    {
        private UnityJsonPlayerDataSerializer _serializer;

        [SetUp]
        public void SetUp()
        {
            _serializer = new UnityJsonPlayerDataSerializer();
        }

        [Test]
        public void GameResult_RoundTripsAccurately()
        {
            var original = new GameResult("session_abc", 450, 6, 24, 4, 1712345678L);
            string json = _serializer.SerializeGameResult(original);

            Assert.IsNotNull(json);
            Assert.IsTrue(json.Contains("session_abc"));
            Assert.IsTrue(json.Contains("450"));

            var deserialized = _serializer.DeserializeGameResult(json);
            Assert.AreEqual(original.SchemaVersion, deserialized.SchemaVersion);
            Assert.AreEqual(original.GameSessionId, deserialized.GameSessionId);
            Assert.AreEqual(original.Score, deserialized.Score);
            Assert.AreEqual(original.LinesCleared, deserialized.LinesCleared);
            Assert.AreEqual(original.BlocksPlaced, deserialized.BlocksPlaced);
            Assert.AreEqual(original.BestCombo, deserialized.BestCombo);
            Assert.AreEqual(original.CompletedAtUnixSeconds, deserialized.CompletedAtUnixSeconds);
        }

        [Test]
        public void GameCenterSnapshot_RoundTripsAccurately()
        {
            var lifetime = new PlayerStats(12, 54000, 15000, 120, 360, 5, 2);
            var currentWeek = new WeeklyPlayerStats(1712000000L, 5, 20000, 8000, 45, 130, 1);
            var previousWeek = new WeeklyPlayerStats(1711395200L, 7, 34000, 15000, 75, 230, 1);

            var recent = new List<RecentGameResult>
            {
                new RecentGameResult("s1", 1000, 1712000100L),
                new RecentGameResult("s2", 2500, 1712000200L)
            };

            var achievements = new List<AchievementProgress>
            {
                new AchievementProgress(AchievementCatalog.FirstSteps, 5, 5, 1712000150L),
                new AchievementProgress(AchievementCatalog.ScoreStarter, 8000, 10000, 0)
            };

            var insights = new List<PlayerInsight>
            {
                new PlayerInsight(AchievementCatalog.Insights.RecentAverage, 1750.0, 1500.0)
            };

            var snapshot = new GameCenterSnapshot(
                42L, 1711000000L, 1712000300L,
                lifetime, currentWeek, previousWeek, 80L,
                recent, achievements, insights);

            string json = _serializer.SerializeSnapshot(snapshot);
            Assert.IsNotNull(json);

            var deserialized = _serializer.DeserializeSnapshot(json);
            Assert.AreEqual(snapshot.Revision, deserialized.Revision);
            Assert.AreEqual(snapshot.CreatedAtUnixSeconds, deserialized.CreatedAtUnixSeconds);
            Assert.AreEqual(snapshot.UpdatedAtUnixSeconds, deserialized.UpdatedAtUnixSeconds);
            Assert.AreEqual(snapshot.BestWeekLinesCleared, deserialized.BestWeekLinesCleared);

            Assert.AreEqual(lifetime.GamesPlayed, deserialized.LifetimeStats.GamesPlayed);
            Assert.AreEqual(lifetime.TotalScore, deserialized.LifetimeStats.TotalScore);
            Assert.AreEqual(lifetime.BestScore, deserialized.LifetimeStats.BestScore);

            Assert.AreEqual(currentWeek.WeekStartUnixSeconds, deserialized.CurrentWeek.WeekStartUnixSeconds);
            Assert.AreEqual(previousWeek.TotalLinesCleared, deserialized.PreviousWeek.TotalLinesCleared);

            Assert.AreEqual(2, deserialized.RecentGames.Count);
            Assert.AreEqual("s1", deserialized.RecentGames[0].GameSessionId);
            Assert.AreEqual(2500, deserialized.RecentGames[1].Score);

            Assert.AreEqual(2, deserialized.Achievements.Count);
            Assert.IsTrue(deserialized.Achievements[0].IsUnlocked);
            Assert.IsFalse(deserialized.Achievements[1].IsUnlocked);

            Assert.AreEqual(1, deserialized.Insights.Count);
            Assert.AreEqual(AchievementCatalog.Insights.RecentAverage, deserialized.Insights[0].InsightId);
            Assert.AreEqual(1750.0, deserialized.Insights[0].CurrentValue);
        }

        [Test]
        public void InvalidJson_ThrowsExpectedException()
        {
            Assert.Throws<ArgumentException>(() => _serializer.DeserializeGameResult(""));
            Assert.Throws<FormatException>(() => _serializer.DeserializeGameResult("not json"));
            Assert.Throws<ArgumentException>(() => _serializer.DeserializeSnapshot(null));
        }
    }
}
