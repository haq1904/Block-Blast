using System.Collections.Generic;
using BlockBlast.Core.Persistence;
using NUnit.Framework;

namespace BlockBlast.Features.WorldCustomization.Test
{
    [TestFixture]
    public class WorldSelectionStoreTest
    {
        private FakeKeyValueStore _fakeStore;
        private WorldSelectionStore _selectionStore;

        [SetUp]
        public void SetUp()
        {
            _fakeStore = new FakeKeyValueStore();
            _selectionStore = new WorldSelectionStore(_fakeStore);
        }

        [Test]
        public void GetSelectedWorldId_WhenEmpty_ReturnsDefaultWorldId()
        {
            string selected = _selectionStore.GetSelectedWorldId("default");
            Assert.AreEqual("default", selected);
        }

        [Test]
        public void SaveSelectedWorldId_PersistsValue_AndLoadsSameValue()
        {
            _selectionStore.SaveSelectedWorldId("world_desert");
            string reloaded = _selectionStore.GetSelectedWorldId("default");

            Assert.AreEqual("world_desert", reloaded);
            Assert.IsTrue(_fakeStore.SaveCalled);
        }

        [Test]
        public void SaveSelectedWorldId_EmptyOrNull_DoesNotOverwrite()
        {
            _selectionStore.SaveSelectedWorldId("world_forest");
            _selectionStore.SaveSelectedWorldId(null);
            _selectionStore.SaveSelectedWorldId("");

            Assert.AreEqual("world_forest", _selectionStore.GetSelectedWorldId("default"));
        }

        private sealed class FakeKeyValueStore : IKeyValueStore
        {
            private readonly Dictionary<string, string> _strings = new Dictionary<string, string>();
            private readonly Dictionary<string, int> _ints = new Dictionary<string, int>();
            public bool SaveCalled { get; private set; }

            public bool HasKey(string key) => _strings.ContainsKey(key) || _ints.ContainsKey(key);
            public int GetInt(string key, int defaultValue = 0) => _ints.TryGetValue(key, out var v) ? v : defaultValue;
            public void SetInt(string key, int value) => _ints[key] = value;
            public string GetString(string key, string defaultValue = null) => _strings.TryGetValue(key, out var v) ? v : defaultValue;
            public void SetString(string key, string value) => _strings[key] = value;
            public void DeleteKey(string key) { _strings.Remove(key); _ints.Remove(key); }
            public void Save() => SaveCalled = true;
        }
    }
}
