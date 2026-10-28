using System;
using System.Reflection;
using BlockBlast.Features.Lobby;
using BlockBlast.Features.PlayerData;
using NUnit.Framework;
using UnityEngine;

namespace BlockBlast.Features.GameFlow.Navigation.Test
{
    public partial class LobbyControllerTest
    {
        private class FakeScoreService : IScoreService
        {
            public int CurrentScore => 0;
            public int HighScore { get; set; } = 8888;
            public int CurrentCombo => 0;
            public int MaxCombo => 0;
            public bool IsPrewarmed => false;
            public int TurnsRemaining => 0;

#pragma warning disable CS0067
            public event Action<int, int> OnScoreChanged;
            public event Action<int> OnComboChanged;
            public event Action<int> OnHighScoreChanged;
#pragma warning restore CS0067

            public void ResetScore() { }
        }

        private class FakeLocalStore : ILocalPlayerDataStore
        {
            public int HighScoreToReturn = 4321;
            public int LoadCount;

            public int LoadHighScore()
            {
                LoadCount++;
                return HighScoreToReturn;
            }

            public bool SaveHighScore(int newScore) => true;
        }

        [Test]
        public void UpdateHighScore_WhenScoreServiceRegistered_UsesScoreService()
        {
            var fakeScore = new FakeScoreService { HighScore = 9999 };
            ServiceLocator.Register<IScoreService>(fakeScore);

            var transitionObj = new GameObject("TransitionView");
            _createdObjects.Add(transitionObj);
            var transitionView = transitionObj.AddComponent<LobbyTransitionView>();

            var homeObj = new GameObject("HomeView");
            _createdObjects.Add(homeObj);
            var homeView = homeObj.AddComponent<LobbyHomeView>();

            typeof(LobbyTransitionView).GetField("_homeView", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(transitionView, homeView);

            var controllerObj = new GameObject("LobbyController");
            _createdObjects.Add(controllerObj);
            var controller = controllerObj.AddComponent<LobbyController>();
            typeof(LobbyController).GetField("_transitionView", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(controller, transitionView);

            var fakeStore = new FakeLocalStore();
            controller.SetLocalPlayerDataStoreForTest(fakeStore);

            controller.RefreshHighScoreForTest();

            Assert.AreEqual(0, fakeStore.LoadCount, "Should not fallback to local store when IScoreService is registered.");
        }

        [Test]
        public void UpdateHighScore_WhenNoScoreService_FallsBackToInjectedLocalStore()
        {
            if (ServiceLocator.TryGet<IScoreService>(out _))
            {
                ServiceLocator.Unregister<IScoreService>();
            }

            var transitionObj = new GameObject("TransitionView");
            _createdObjects.Add(transitionObj);
            var transitionView = transitionObj.AddComponent<LobbyTransitionView>();

            var homeObj = new GameObject("HomeView");
            _createdObjects.Add(homeObj);
            var homeView = homeObj.AddComponent<LobbyHomeView>();

            typeof(LobbyTransitionView).GetField("_homeView", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(transitionView, homeView);

            var controllerObj = new GameObject("LobbyController");
            _createdObjects.Add(controllerObj);
            var controller = controllerObj.AddComponent<LobbyController>();
            typeof(LobbyController).GetField("_transitionView", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(controller, transitionView);

            var fakeStore = new FakeLocalStore { HighScoreToReturn = 7777 };
            controller.SetLocalPlayerDataStoreForTest(fakeStore);

            controller.RefreshHighScoreForTest();

            Assert.AreEqual(1, fakeStore.LoadCount, "Must read from injected local store.");
        }
    }
}
