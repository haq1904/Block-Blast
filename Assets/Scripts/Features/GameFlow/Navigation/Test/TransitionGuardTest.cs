using System;
using System.Threading;
using System.Threading.Tasks;
using BlockBlast.Features.GameFlow.Navigation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BlockBlast.Features.GameFlow.Navigation.Test
{
    [TestFixture]
    public class TransitionGuardTest
    {
        private FakeSceneService _fakeSceneService;
        private GameSceneRouteCatalogSO _catalog;
        private GameObject _navigatorObj;
        private GameSceneNavigator _navigator;

        [SetUp]
        public void SetUp()
        {
            ServiceLocator.ClearAll();

            _fakeSceneService = new FakeSceneService();
            ServiceLocator.Register<ISceneService>(_fakeSceneService);

            _catalog = ScriptableObject.CreateInstance<GameSceneRouteCatalogSO>();
            _catalog.SetIndexesForTesting(1, 2, 3);

            _navigatorObj = new GameObject("TestNavigator");
            _navigator = _navigatorObj.AddComponent<GameSceneNavigator>();
            _navigator.RouteCatalog = _catalog;
        }

        [TearDown]
        public void TearDown()
        {
            if (_navigatorObj != null)
            {
                UnityEngine.Object.DestroyImmediate(_navigatorObj);
            }

            if (_catalog != null)
            {
                UnityEngine.Object.DestroyImmediate(_catalog);
            }

            ServiceLocator.ClearAll();
        }

        [Test]
        public async Task LoadAsync_ForwardsCorrectRequestToSceneService()
        {
            await _navigator.LoadAsync(GameSceneRoute.Intro, SceneTransitionPresentation.None, 0f);

            Assert.AreEqual(1, _fakeSceneService.LoadCallCount);
            Assert.AreEqual(1, _fakeSceneService.LastRequest.SceneBuildIndex);
            Assert.AreEqual(SceneTransitionPresentation.None, _fakeSceneService.LastRequest.Presentation);
        }

        [Test]
        public async Task LoadAsync_LobbyWithLoadingScreen_ForwardsAdditiveRequestWithWorld()
        {
            await _navigator.LoadAsync(GameSceneRoute.Lobby, SceneTransitionPresentation.LoadingScreen, 0.5f);

            Assert.AreEqual(1, _fakeSceneService.AdditiveCallCount);
            Assert.AreEqual(2, _fakeSceneService.LastAdditiveRequest.TargetSceneBuildIndex);
            Assert.AreEqual(4, _fakeSceneService.LastAdditiveRequest.ActiveSceneBuildIndex);
            CollectionAssert.AreEqual(new[] { 4, 2 }, _fakeSceneService.LastAdditiveRequest.LoadBuildIndexes);
            CollectionAssert.AreEqual(new[] { 1, 3 }, _fakeSceneService.LastAdditiveRequest.UnloadBuildIndexes);
            Assert.AreEqual(SceneTransitionPresentation.LoadingScreen, _fakeSceneService.LastAdditiveRequest.Presentation);
            Assert.AreEqual(0.5f, _fakeSceneService.LastAdditiveRequest.MinimumVisibleDuration, 0.0001f);
        }

        [Test]
        public async Task LoadAsync_PlayWithLoadingScreen_ForwardsAdditiveRequestWithWorld()
        {
            await _navigator.LoadAsync(GameSceneRoute.Play, SceneTransitionPresentation.LoadingScreen, 1.0f);

            Assert.AreEqual(1, _fakeSceneService.AdditiveCallCount);
            Assert.AreEqual(3, _fakeSceneService.LastAdditiveRequest.TargetSceneBuildIndex);
            Assert.AreEqual(4, _fakeSceneService.LastAdditiveRequest.ActiveSceneBuildIndex);
            CollectionAssert.AreEqual(new[] { 4, 3 }, _fakeSceneService.LastAdditiveRequest.LoadBuildIndexes);
            CollectionAssert.AreEqual(new[] { 2, 1 }, _fakeSceneService.LastAdditiveRequest.UnloadBuildIndexes);
            Assert.AreEqual(SceneTransitionPresentation.LoadingScreen, _fakeSceneService.LastAdditiveRequest.Presentation);
            Assert.AreEqual(1.0f, _fakeSceneService.LastAdditiveRequest.MinimumVisibleDuration, 0.0001f);
        }

        private sealed class FakeSceneService : ISceneService
        {
            public bool IsTransitioning { get; set; }
            public int LoadCallCount { get; private set; }
            public SceneLoadRequest LastRequest { get; private set; }
            public int AdditiveCallCount { get; private set; }
            public AdditiveSceneTransitionRequest LastAdditiveRequest { get; private set; }

            public Task LoadSceneAsync(SceneLoadRequest request, CancellationToken cancellationToken = default)
            {
                LoadCallCount++;
                LastRequest = request;
                return Task.CompletedTask;
            }

            public Task TransitionAdditiveAsync(AdditiveSceneTransitionRequest request, CancellationToken cancellationToken = default)
            {
                AdditiveCallCount++;
                LastAdditiveRequest = request;
                return Task.CompletedTask;
            }
        }
    }
}
