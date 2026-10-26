using System;
using BlockBlast.Features.PlayerData;
using NUnit.Framework;

namespace BlockBlast.Features.PlayerSession.Test
{
    [TestFixture]
    public sealed class GameSessionStatsTest
    {
        [Test]
        public void Constructor_GeneratesStableNonEmptySessionId()
        {
            var stats = new GameSessionStats();

            Assert.IsFalse(string.IsNullOrEmpty(stats.GameSessionId));
            Assert.AreEqual(0, stats.LinesCleared);
            Assert.AreEqual(0, stats.BlocksPlaced);
            Assert.AreEqual(0, stats.BestCombo);
            Assert.IsFalse(stats.IsCompleted);
        }

        [Test]
        public void RecordPlacement_AccumulatesTilesAndLinesCorrectly()
        {
            var stats = new GameSessionStats("test_session_1");

            stats.RecordPlacement(4, 1);
            stats.RecordPlacement(3, 2);

            Assert.AreEqual(7, stats.BlocksPlaced);
            Assert.AreEqual(3, stats.LinesCleared);
        }

        [Test]
        public void RecordPlacement_RejectsNegativeArguments()
        {
            var stats = new GameSessionStats("test_session_1");

            Assert.Throws<ArgumentOutOfRangeException>(() => stats.RecordPlacement(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => stats.RecordPlacement(3, -1));
        }

        [Test]
        public void Complete_ProducesValidGameResult_AndMarksCompleted()
        {
            var stats = new GameSessionStats("test_session_complete");
            stats.RecordPlacement(5, 2);

            var result = stats.Complete(1500, 3, 1700000000L);

            Assert.IsTrue(stats.IsCompleted);
            Assert.AreEqual("test_session_complete", result.GameSessionId);
            Assert.AreEqual(1500, result.Score);
            Assert.AreEqual(2, result.LinesCleared);
            Assert.AreEqual(5, result.BlocksPlaced);
            Assert.AreEqual(3, result.BestCombo);
            Assert.AreEqual(1700000000L, result.CompletedAtUnixSeconds);
            Assert.AreEqual(PlayerDataSchema.CurrentVersion, result.SchemaVersion);
        }

        [Test]
        public void Complete_CalledTwice_ThrowsInvalidOperationException()
        {
            var stats = new GameSessionStats("test_session_duplicate");
            stats.Complete(100, 1, 1000L);

            Assert.Throws<InvalidOperationException>(() => stats.Complete(200, 2, 2000L));
        }

        [Test]
        public void RecordPlacement_AfterCompletion_DoesNotMutateState()
        {
            var stats = new GameSessionStats("test_session_frozen");
            stats.RecordPlacement(4, 1);
            stats.Complete(100, 1, 1000L);

            stats.RecordPlacement(10, 5);

            Assert.AreEqual(4, stats.BlocksPlaced);
            Assert.AreEqual(1, stats.LinesCleared);
        }
    }
}
