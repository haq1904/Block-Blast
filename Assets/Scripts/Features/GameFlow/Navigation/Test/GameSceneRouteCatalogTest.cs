using System;
using NUnit.Framework;
using UnityEngine;

namespace BlockBlast.Features.GameFlow.Navigation.Test
{
    [TestFixture]
    public class GameSceneRouteCatalogTest
    {
        private GameSceneRouteCatalogSO _catalog;

        [SetUp]
        public void SetUp()
        {
            _catalog = ScriptableObject.CreateInstance<GameSceneRouteCatalogSO>();
            _catalog.SetIndexesForTesting(1, 2, 3);
        }

        [TearDown]
        public void TearDown()
        {
            if (_catalog != null)
            {
                UnityEngine.Object.DestroyImmediate(_catalog);
            }
        }

        [Test]
        public void GetBuildIndex_ValidRoutes_ReturnsConfiguredIndexes()
        {
            Assert.AreEqual(1, _catalog.GetBuildIndex(GameSceneRoute.Intro));
            Assert.AreEqual(2, _catalog.GetBuildIndex(GameSceneRoute.Lobby));
            Assert.AreEqual(3, _catalog.GetBuildIndex(GameSceneRoute.Play));
        }

        [Test]
        public void GetBuildIndex_NegativeIndex_ThrowsInvalidOperationException()
        {
            _catalog.SetIndexesForTesting(-1, 2, 3);
            Assert.Throws<InvalidOperationException>(() => _catalog.GetBuildIndex(GameSceneRoute.Intro));
        }

        [Test]
        public void GetBuildIndex_UnsupportedRoute_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _catalog.GetBuildIndex((GameSceneRoute)999));
        }
    }
}
