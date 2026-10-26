using System;
using System.Collections.Generic;
using BlockBlast.Core.Persistence;
using NUnit.Framework;

namespace BlockBlast.Features.PlayerData.Test
{
    [TestFixture]
    public class LocalPlayerDataStoreTest
    {
        private class FakeKeyValueStore : IKeyValueStore
        {
            public readonly Dictionary<string, int> Ints = new Dictionary<string, int>();
            public readonly Dictionary<string, string> Strings = new Dictionary<string, string>();
            public int SaveCount;

            public bool HasKey(string key) => Ints.ContainsKey(key) || Strings.ContainsKey(key);

            public int GetInt(string key, int defaultValue = 0) =>
                Ints.TryGetValue(key, out int val) ? val : defaultValue;

            public void SetInt(string key, int value) => Ints[key] = value;

            public string GetString(string key, string defaultValue = null) =>
                Strings.TryGetValue(key, out string val) ? val : defaultValue;

            public void SetString(string key, string value) => Strings[key] = value;

            public void DeleteKey(string key)
            {
                Ints.Remove(key);
                Strings.Remove(key);
            }

            public void Save() => SaveCount++;
        }

        private FakeKeyValueStore _fakeStore;
        private LocalPlayerDataStore _store;

        [SetUp]
        public void SetUp()
        {
            _fakeStore = new FakeKeyValueStore();
            _store = new LocalPlayerDataStore(_fakeStore);
        }

        [Test]
        public void Migration_TakesMaxOfLegacyKeys_DeletesLegacy_AndSetsVersion()
        {
            _fakeStore.SetInt(PlayerDataKeys.LegacyScoreControllerHighScore, 1500);
            _fakeStore.SetInt(PlayerDataKeys.LegacyLobbyHighScore, 3200);

            int loaded = _store.LoadHighScore();

            Assert.AreEqual(3200, loaded);
            Assert.AreEqual(3200, _fakeStore.GetInt(PlayerDataKeys.HighScoreV1));
            Assert.AreEqual(1, _fakeStore.GetInt(PlayerDataKeys.LocalSchemaVersion));
            Assert.IsFalse(_fakeStore.HasKey(PlayerDataKeys.LegacyScoreControllerHighScore));
            Assert.IsFalse(_fakeStore.HasKey(PlayerDataKeys.LegacyLobbyHighScore));
            Assert.AreEqual(1, _fakeStore.SaveCount, "Migration must flush once.");
        }

        [Test]
        public void Migration_IsIdempotent_DoesNotFlushTwice()
        {
            _fakeStore.SetInt(PlayerDataKeys.LegacyScoreControllerHighScore, 500);

            _store.LoadHighScore();
            Assert.AreEqual(1, _fakeStore.SaveCount);

            _store.LoadHighScore();
            Assert.AreEqual(1, _fakeStore.SaveCount, "Subsequent reads must not re-migrate or flush.");
        }

        [Test]
        public void SaveHighScore_OnlySaves_WhenHigherThanCurrent()
        {
            _store.SaveHighScore(1000);
            Assert.AreEqual(1000, _store.LoadHighScore());

            bool savedLower = _store.SaveHighScore(500);
            Assert.IsFalse(savedLower);
            Assert.AreEqual(1000, _store.LoadHighScore());

            bool savedEqual = _store.SaveHighScore(1000);
            Assert.IsFalse(savedEqual);
            Assert.AreEqual(1000, _store.LoadHighScore());

            bool savedHigher = _store.SaveHighScore(2000);
            Assert.IsTrue(savedHigher);
            Assert.AreEqual(2000, _store.LoadHighScore());
        }

        [Test]
        public void FutureVersion_DoesNotMutateData()
        {
            _fakeStore.SetInt(PlayerDataKeys.LocalSchemaVersion, 999);
            _fakeStore.SetInt(PlayerDataKeys.HighScoreV1, 8888);
            _fakeStore.SetInt(PlayerDataKeys.LegacyScoreControllerHighScore, 9999);

            var store = new LocalPlayerDataStore(_fakeStore);
            bool saved = store.SaveHighScore(100000);

            Assert.IsFalse(saved);
            Assert.AreEqual(8888, _fakeStore.GetInt(PlayerDataKeys.HighScoreV1));
            Assert.IsTrue(_fakeStore.HasKey(PlayerDataKeys.LegacyScoreControllerHighScore), "Legacy keys must not be deleted on future version.");
        }

        [Test]
        public void Metadata_RoundTripsAccurately()
        {
            _store.SaveLastCloudSyncUnixSeconds(1715000000L);
            _store.SaveCachedSnapshotRevision(105L);

            Assert.AreEqual(1715000000L, _store.LoadLastCloudSyncUnixSeconds());
            Assert.AreEqual(105L, _store.LoadCachedSnapshotRevision());
        }
    }
}
