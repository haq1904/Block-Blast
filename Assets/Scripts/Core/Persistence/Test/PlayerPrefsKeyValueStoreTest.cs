using System;
using NUnit.Framework;
using UnityEngine;

namespace BlockBlast.Core.Persistence.Test
{
    [TestFixture]
    public class PlayerPrefsKeyValueStoreTest
    {
        private PlayerPrefsKeyValueStore _store;
        private string _testKeyInt;
        private string _testKeyString;

        [SetUp]
        public void SetUp()
        {
            _store = new PlayerPrefsKeyValueStore();
            _testKeyInt = "TEST_INT_" + Guid.NewGuid().ToString("N");
            _testKeyString = "TEST_STR_" + Guid.NewGuid().ToString("N");
        }

        [TearDown]
        public void TearDown()
        {
            if (PlayerPrefs.HasKey(_testKeyInt))
            {
                PlayerPrefs.DeleteKey(_testKeyInt);
            }
            if (PlayerPrefs.HasKey(_testKeyString))
            {
                PlayerPrefs.DeleteKey(_testKeyString);
            }
            PlayerPrefs.Save();
        }

        [Test]
        public void GetInt_WhenKeyDoesNotExist_ReturnsDefaultValue()
        {
            int val = _store.GetInt(_testKeyInt, 42);
            Assert.AreEqual(42, val);
        }

        [Test]
        public void SetInt_And_GetInt_StoresAndRetrievesValue()
        {
            _store.SetInt(_testKeyInt, 999);
            Assert.IsTrue(_store.HasKey(_testKeyInt));
            Assert.AreEqual(999, _store.GetInt(_testKeyInt));
        }

        [Test]
        public void GetString_WhenKeyDoesNotExist_ReturnsDefaultValue()
        {
            string val = _store.GetString(_testKeyString, "fallback");
            Assert.AreEqual("fallback", val);
        }

        [Test]
        public void SetString_And_GetString_StoresAndRetrievesValue()
        {
            _store.SetString(_testKeyString, "hello_world");
            Assert.IsTrue(_store.HasKey(_testKeyString));
            Assert.AreEqual("hello_world", _store.GetString(_testKeyString));
        }

        [Test]
        public void DeleteKey_RemovesKey()
        {
            _store.SetInt(_testKeyInt, 123);
            Assert.IsTrue(_store.HasKey(_testKeyInt));

            _store.DeleteKey(_testKeyInt);
            Assert.IsFalse(_store.HasKey(_testKeyInt));
        }

        [Test]
        public void Save_DoesNotThrow()
        {
            _store.SetInt(_testKeyInt, 555);
            Assert.DoesNotThrow(() => _store.Save());
        }

        [Test]
        public void InvalidKey_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _store.HasKey(null));
            Assert.Throws<ArgumentException>(() => _store.GetInt(""));
            Assert.Throws<ArgumentException>(() => _store.SetInt(null, 1));
            Assert.Throws<ArgumentException>(() => _store.GetString(""));
            Assert.Throws<ArgumentException>(() => _store.SetString(null, "val"));
            Assert.Throws<ArgumentException>(() => _store.DeleteKey(""));
        }
    }
}
