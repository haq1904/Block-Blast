using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace BlockBlast.Features.WorldCustomization.Test
{
    [TestFixture]
    public class WorldCatalogTest
    {
        private GameObject _fakePrefab;

        [SetUp]
        public void SetUp()
        {
            _fakePrefab = new GameObject("FakePrefab");
        }

        [TearDown]
        public void TearDown()
        {
            if (_fakePrefab != null)
            {
                Object.DestroyImmediate(_fakePrefab);
            }
        }

        private WorldDefinition CreateValidDefinition(string id)
        {
            return new WorldDefinition(
                id,
                "Display " + id,
                null,
                new WorldScenePrefabReferences(_fakePrefab),
                new LobbyScenePrefabReferences(_fakePrefab, _fakePrefab),
                new PlayScenePrefabReferences(_fakePrefab, _fakePrefab)
            );
        }

        [Test]
        public void ValidateCatalog_ValidSingleDefaultWorld_ReturnsTrue()
        {
            var catalog = ScriptableObject.CreateInstance<WorldCatalogSO>();
            var def = CreateValidDefinition("default");
            catalog.InitializeForTest("default", new List<WorldDefinition> { def });

            bool isValid = catalog.ValidateCatalog(out var error);

            Assert.IsTrue(isValid, error);
            Assert.IsNull(error);
            Assert.IsTrue(catalog.TryGetDefaultWorld(out var result));
            Assert.AreEqual("default", result.Id);
        }

        [Test]
        public void ValidateCatalog_MissingDefault_ReturnsFalse()
        {
            var catalog = ScriptableObject.CreateInstance<WorldCatalogSO>();
            var def = CreateValidDefinition("world_a");
            catalog.InitializeForTest("default", new List<WorldDefinition> { def });

            bool isValid = catalog.ValidateCatalog(out var error);

            Assert.IsFalse(isValid);
            StringAssert.Contains("default", error);
        }

        [Test]
        public void ValidateCatalog_DuplicateWorldId_ReturnsFalse()
        {
            var catalog = ScriptableObject.CreateInstance<WorldCatalogSO>();
            var def1 = CreateValidDefinition("default");
            var def2 = CreateValidDefinition("default");
            catalog.InitializeForTest("default", new List<WorldDefinition> { def1, def2 });

            bool isValid = catalog.ValidateCatalog(out var error);

            Assert.IsFalse(isValid);
            StringAssert.Contains("Duplicate", error);
        }

        [Test]
        public void ValidateCatalog_MissingRequiredPart_ReturnsFalse()
        {
            var catalog = ScriptableObject.CreateInstance<WorldCatalogSO>();
            var def = new WorldDefinition(
                "default",
                "Default",
                null,
                new WorldScenePrefabReferences(null), // missing ground
                new LobbyScenePrefabReferences(_fakePrefab, _fakePrefab),
                new PlayScenePrefabReferences(_fakePrefab, _fakePrefab)
            );
            catalog.InitializeForTest("default", new List<WorldDefinition> { def });

            bool isValid = catalog.ValidateCatalog(out var error);

            Assert.IsFalse(isValid);
            StringAssert.Contains("missing required WorldGround", error);
        }

        [Test]
        public void TryGetWorld_CaseInsensitiveMatch_ReturnsTrue()
        {
            var catalog = ScriptableObject.CreateInstance<WorldCatalogSO>();
            var def = CreateValidDefinition("Default");
            catalog.InitializeForTest("Default", new List<WorldDefinition> { def });

            Assert.IsTrue(catalog.TryGetWorld("default", out var found));
            Assert.AreEqual("Default", found.Id);
        }
    }
}
