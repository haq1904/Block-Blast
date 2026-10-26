using System;
using System.IO;
using BlockBlast.Features.PlayerData;
using BlockBlast.Features.PlayerCloud.Persistence;
using BlockBlast.Features.PlayerCloud.Time;
using NUnit.Framework;

namespace BlockBlast.Features.PlayerCloud.Test
{
    [TestFixture]
    public sealed class FilePlayerCloudSnapshotCacheTest
    {
        private string _tempDir;
        private string _cachePath;
        private UnityJsonPlayerDataSerializer _serializer;
        private FakeLocalStore _localStore;
        private FakeClock _clock;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "PlayerCloudTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
            _cachePath = Path.Combine(_tempDir, "game_center_snapshot_v1.json");
            _serializer = new UnityJsonPlayerDataSerializer();
            _localStore = new FakeLocalStore();
            _clock = new FakeClock(1700000000L);
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
        public void SaveAndLoad_RoundTripsValidSnapshot()
        {
            var cache = new FilePlayerCloudSnapshotCache(_cachePath, _serializer, _localStore, _clock);
            var snapshot = CreateSnapshotWithRevision(1L);

            bool saved = cache.Save(snapshot);
            Assert.IsTrue(saved);
            Assert.IsTrue(File.Exists(_cachePath));
            Assert.AreEqual(1L, _localStore.CachedRevision);
            Assert.AreEqual(1700000000L, _localStore.LastSyncUnix);

            bool loaded = cache.TryLoad(out var loadedSnapshot);
            Assert.IsTrue(loaded);
            Assert.IsNotNull(loadedSnapshot);
            Assert.AreEqual(1L, loadedSnapshot.Revision);
        }

        [Test]
        public void Save_OlderRevision_DoesNotRegressOrOverwrite()
        {
            var cache = new FilePlayerCloudSnapshotCache(_cachePath, _serializer, _localStore, _clock);
            var snapshot1 = CreateSnapshotWithRevision(5L);
            cache.Save(snapshot1);

            var olderSnapshot = CreateSnapshotWithRevision(3L);
            bool saved = cache.Save(olderSnapshot);

            Assert.IsFalse(saved);
            Assert.AreEqual(5L, _localStore.CachedRevision);

            cache.TryLoad(out var current);
            Assert.AreEqual(5L, current.Revision);
        }

        [Test]
        public void TryLoad_RecoversFromBackup_WhenTargetIsCorrupted()
        {
            var cache = new FilePlayerCloudSnapshotCache(_cachePath, _serializer, _localStore, _clock);
            var snapshot1 = CreateSnapshotWithRevision(1L);
            cache.Save(snapshot1);

            var snapshot2 = CreateSnapshotWithRevision(2L);
            cache.Save(snapshot2);

            // Now _cachePath has rev 2 and _cachePath.bak has rev 1.
            // Corrupt the target file:
            File.WriteAllText(_cachePath, "{ corrupt invalid json !!!");

            bool loaded = cache.TryLoad(out var recoveredSnapshot);

            Assert.IsTrue(loaded);
            Assert.IsNotNull(recoveredSnapshot);
            Assert.AreEqual(1L, recoveredSnapshot.Revision);
        }

        [Test]
        public void TryLoad_ReturnsFalse_WhenNoValidCacheExists()
        {
            var cache = new FilePlayerCloudSnapshotCache(_cachePath, _serializer, _localStore, _clock);

            bool loaded = cache.TryLoad(out var snapshot);

            Assert.IsFalse(loaded);
            Assert.IsNull(snapshot);
        }

        [Test]
        public void Clear_DeletesCacheFiles_AndResetsRevisionMetadata()
        {
            var cache = new FilePlayerCloudSnapshotCache(_cachePath, _serializer, _localStore, _clock);
            var snapshot = CreateSnapshotWithRevision(10L);

            cache.Save(snapshot);
            Assert.IsTrue(File.Exists(_cachePath));
            Assert.AreEqual(10L, _localStore.CachedRevision);
            Assert.AreEqual(1700000000L, _localStore.LastSyncUnix);

            cache.Clear();

            Assert.IsFalse(File.Exists(_cachePath));
            Assert.IsFalse(File.Exists(_cachePath + ".tmp"));
            Assert.IsFalse(File.Exists(_cachePath + ".bak"));
            Assert.AreEqual(0L, _localStore.CachedRevision);
            Assert.AreEqual(0L, _localStore.LastSyncUnix);

            bool loadedAfterClear = cache.TryLoad(out var loadedSnapshot);
            Assert.IsFalse(loadedAfterClear);
            Assert.IsNull(loadedSnapshot);
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

        private sealed class FakeLocalStore : ILocalPlayerDataStore
        {
            public long CachedRevision { get; set; } = 0L;
            public long LastSyncUnix { get; set; } = 0L;

            public int LoadHighScore() => 0;
            public bool SaveHighScore(int newScore) => true;
            public long LoadLastCloudSyncUnixSeconds() => LastSyncUnix;
            public void SaveLastCloudSyncUnixSeconds(long unixSeconds) => LastSyncUnix = unixSeconds;
            public long LoadCachedSnapshotRevision() => CachedRevision;
            public void SaveCachedSnapshotRevision(long revision) => CachedRevision = revision;
        }

        private sealed class FakeClock : IPlayerCloudClock
        {
            public long NowUnixSeconds { get; set; }

            public FakeClock(long now)
            {
                NowUnixSeconds = now;
            }
        }
    }
}
