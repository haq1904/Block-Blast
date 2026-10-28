using System;
using System.IO;
using BlockBlast.Features.PlayerData;
using BlockBlast.Features.PlayerCloud.Persistence;
using NUnit.Framework;

namespace BlockBlast.Features.PlayerCloud.Test
{
    [TestFixture]
    public sealed class FilePlayerCloudSnapshotCacheTest
    {
        private string _tempDir;
        private UnityJsonPlayerDataSerializer _serializer;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "PlayerCloudTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
            _serializer = new UnityJsonPlayerDataSerializer();
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir))
            {
                try
                {
                    Directory.Delete(_tempDir, true);
                }
                catch
                {
                }
            }
        }

        [Test]
        public void SaveAndLoad_RoundTripsValidSnapshot_UnderScope()
        {
            var cache = new FilePlayerCloudSnapshotCache(_tempDir, _serializer);
            var scope = new PlayerCloudCacheScope("production", "player_123");
            var snapshot = CreateSnapshotWithRevision(1L);

            bool saved = cache.Save(scope, snapshot);
            Assert.IsTrue(saved);

            string expectedFilePath = Path.Combine(_tempDir, "production", "player_123", "game_center_snapshot_v1.json");
            Assert.IsTrue(File.Exists(expectedFilePath));

            bool loaded = cache.TryLoad(scope, out var loadedSnapshot);
            Assert.IsTrue(loaded);
            Assert.IsNotNull(loadedSnapshot);
            Assert.AreEqual(1L, loadedSnapshot.Revision);
        }

        [Test]
        public void Save_OlderRevision_DoesNotRegressOrOverwrite_PerScope()
        {
            var cache = new FilePlayerCloudSnapshotCache(_tempDir, _serializer);
            var scope = new PlayerCloudCacheScope("production", "player_123");
            var snapshot1 = CreateSnapshotWithRevision(5L);
            cache.Save(scope, snapshot1);

            var olderSnapshot = CreateSnapshotWithRevision(3L);
            bool saved = cache.Save(scope, olderSnapshot);

            Assert.IsFalse(saved);

            cache.TryLoad(scope, out var current);
            Assert.AreEqual(5L, current.Revision);
        }

        [Test]
        public void DifferentScopes_AreIsolated_AndDoNotCollide()
        {
            var cache = new FilePlayerCloudSnapshotCache(_tempDir, _serializer);
            var scopeA = new PlayerCloudCacheScope("production", "player_a");
            var scopeB = new PlayerCloudCacheScope("production", "player_b");
            var scopeC = new PlayerCloudCacheScope("development", "player_a");

            cache.Save(scopeA, CreateSnapshotWithRevision(10L));
            cache.Save(scopeB, CreateSnapshotWithRevision(20L));
            cache.Save(scopeC, CreateSnapshotWithRevision(30L));

            Assert.IsTrue(cache.TryLoad(scopeA, out var snapA));
            Assert.IsTrue(cache.TryLoad(scopeB, out var snapB));
            Assert.IsTrue(cache.TryLoad(scopeC, out var snapC));

            Assert.AreEqual(10L, snapA.Revision);
            Assert.AreEqual(20L, snapB.Revision);
            Assert.AreEqual(30L, snapC.Revision);
        }

        [Test]
        public void TryLoad_RecoversFromBackup_WhenTargetIsCorrupted()
        {
            var cache = new FilePlayerCloudSnapshotCache(_tempDir, _serializer);
            var scope = new PlayerCloudCacheScope("production", "player_123");
            var snapshot1 = CreateSnapshotWithRevision(1L);
            cache.Save(scope, snapshot1);

            var snapshot2 = CreateSnapshotWithRevision(2L);
            cache.Save(scope, snapshot2);

            string targetFile = Path.Combine(_tempDir, "production", "player_123", "game_center_snapshot_v1.json");
            File.WriteAllText(targetFile, "{ corrupt invalid json !!!");

            bool loaded = cache.TryLoad(scope, out var recoveredSnapshot);

            Assert.IsTrue(loaded);
            Assert.IsNotNull(recoveredSnapshot);
            Assert.AreEqual(1L, recoveredSnapshot.Revision);
        }

        [Test]
        public void TryLoad_ReturnsFalse_WhenNoValidCacheExists()
        {
            var cache = new FilePlayerCloudSnapshotCache(_tempDir, _serializer);
            var scope = new PlayerCloudCacheScope("production", "player_nonexistent");

            bool loaded = cache.TryLoad(scope, out var snapshot);

            Assert.IsFalse(loaded);
            Assert.IsNull(snapshot);
        }

        [Test]
        public void Clear_DeletesCacheFiles_ForSpecificScopeOnly()
        {
            var cache = new FilePlayerCloudSnapshotCache(_tempDir, _serializer);
            var scope1 = new PlayerCloudCacheScope("production", "player_1");
            var scope2 = new PlayerCloudCacheScope("production", "player_2");

            cache.Save(scope1, CreateSnapshotWithRevision(10L));
            cache.Save(scope2, CreateSnapshotWithRevision(20L));

            cache.Clear(scope1);

            Assert.IsFalse(cache.TryLoad(scope1, out _));
            Assert.IsTrue(cache.TryLoad(scope2, out var snap2));
            Assert.AreEqual(20L, snap2.Revision);
        }

        [Test]
        public void LegacyUnscopedFile_CleanedUp_OnConstruction()
        {
            string legacyFile = Path.Combine(_tempDir, "game_center_snapshot_v1.json");
            string legacyTmp = Path.Combine(_tempDir, "game_center_snapshot_v1.json.tmp");
            string legacyBak = Path.Combine(_tempDir, "game_center_snapshot_v1.json.bak");
            File.WriteAllText(legacyFile, "{}");
            File.WriteAllText(legacyTmp, "{}");
            File.WriteAllText(legacyBak, "{}");

            _ = new FilePlayerCloudSnapshotCache(_tempDir, _serializer);

            Assert.IsFalse(File.Exists(legacyFile));
            Assert.IsFalse(File.Exists(legacyTmp));
            Assert.IsFalse(File.Exists(legacyBak));
        }

        private static GameCenterSnapshot CreateSnapshotWithRevision(long revision)
        {
            return new GameCenterSnapshot(
                revision,
                1000L,
                1000L,
                new PlayerStats(1, 100, 100, 2, 5, 1, 1),
                new WeeklyPlayerStats(1000L, 1, 100, 100, 2, 5, 1),
                new WeeklyPlayerStats(500L, 0, 0, 0, 0, 0, 0),
                2L,
                Array.Empty<RecentGameResult>(),
                Array.Empty<AchievementProgress>(),
                Array.Empty<PlayerInsight>(),
                PlayerDataSchema.CurrentVersion);
        }
    }
}
