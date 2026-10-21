using System;
using BlockBlast.Features.GameFlow.Camera;
using BlockBlast.Features.GameFlow.Navigation;
using NUnit.Framework;
using UnityEngine;

namespace BlockBlast.Features.GameFlow.Navigation.Test
{
    [TestFixture]
    public class ServiceOwnershipTest
    {
        private GameObject _rootA;
        private GameObject _rootB;

        [SetUp]
        public void SetUp()
        {
            ServiceLocator.ClearAll();
            _rootA = new GameObject("ServiceRoot_A");
            _rootB = new GameObject("ServiceRoot_B");
        }

        [TearDown]
        public void TearDown()
        {
            if (_rootA != null) UnityEngine.Object.DestroyImmediate(_rootA);
            if (_rootB != null) UnityEngine.Object.DestroyImmediate(_rootB);
            ServiceLocator.ClearAll();
        }

        [Test]
        public void ServiceLocator_UnregisterWithOwner_OnlyRemovesIfOwnerMatches()
        {
            var owner1 = new object();
            var owner2 = new object();

            ServiceLocator.Register(owner1);

            // Attempt unregister with wrong owner
            bool unregWrong = ServiceLocator.Unregister(owner2);
            Assert.IsFalse(unregWrong, "Unregistering with wrong owner must return false.");
            Assert.IsTrue(ServiceLocator.TryGet<object>(out var current));
            Assert.AreSame(owner1, current, "Original owner must remain registered.");

            // Unregister with correct owner
            bool unregCorrect = ServiceLocator.Unregister(owner1);
            Assert.IsTrue(unregCorrect, "Unregistering with correct owner must return true.");
            Assert.IsFalse(ServiceLocator.TryGet<object>(out _), "Service must be unregistered.");
        }

        [Test]
        public void GameSceneNavigator_DuplicateInstance_DoesNotUnregisterActiveInstanceWhenDestroyed()
        {
            var navA = _rootA.AddComponent<GameSceneNavigator>();
            var awake = typeof(GameSceneNavigator).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var onDestroy = typeof(GameSceneNavigator).GetMethod("OnDestroy", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            awake?.Invoke(navA, null);
            Assert.IsTrue(ServiceLocator.TryGet<IGameSceneNavigator>(out var registeredNav));
            Assert.AreSame(navA, registeredNav);

            // Add duplicate navigator
            var navB = _rootB.AddComponent<GameSceneNavigator>();
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("already registered"));
            awake?.Invoke(navB, null);

            // navA must still be registered
            Assert.IsTrue(ServiceLocator.TryGet<IGameSceneNavigator>(out var stillRegistered));
            Assert.AreSame(navA, stillRegistered, "Original GameSceneNavigator must remain registered after duplicate destruction.");
        }

        [Test]
        public void WorldCameraController_DuplicateInstance_DoesNotUnregisterActiveInstanceWhenDestroyed()
        {
            var camA = _rootA.AddComponent<WorldCameraController>();
            var awake = typeof(WorldCameraController).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var onDestroy = typeof(WorldCameraController).GetMethod("OnDestroy", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            awake?.Invoke(camA, null);
            Assert.IsTrue(ServiceLocator.TryGet<IWorldCameraTransitionService>(out var registeredCam));
            Assert.AreSame(camA, registeredCam);

            // Add duplicate camera controller
            var camB = _rootB.AddComponent<WorldCameraController>();
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("already registered"));
            awake?.Invoke(camB, null);
            Assert.IsFalse(camB.enabled, "Duplicate WorldCameraController should disable itself.");

            // Destroy duplicate
            onDestroy?.Invoke(camB, null);
            UnityEngine.Object.DestroyImmediate(camB);

            // camA must still be registered
            Assert.IsTrue(ServiceLocator.TryGet<IWorldCameraTransitionService>(out var stillRegistered));
            Assert.AreSame(camA, stillRegistered, "Original WorldCameraController must remain registered after duplicate destruction.");
        }

        [Test]
        public void PlaySceneTransitionVfxController_DuplicateInstance_DoesNotUnregisterActiveInstanceWhenDestroyed()
        {
            var vfxA = _rootA.AddComponent<BlockBlast.Features.GameFlow.Play.PlaySceneTransitionVfxController>();
            var onEnable = typeof(BlockBlast.Features.GameFlow.Play.PlaySceneTransitionVfxController).GetMethod("OnEnable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var onDisable = typeof(BlockBlast.Features.GameFlow.Play.PlaySceneTransitionVfxController).GetMethod("OnDisable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            onEnable?.Invoke(vfxA, null);
            Assert.IsTrue(ServiceLocator.TryGet<BlockBlast.Features.GameFlow.Play.IPlaySceneTransitionVfxService>(out var registeredVfx));
            Assert.AreSame(vfxA, registeredVfx);

            // Add duplicate vfx controller
            var vfxB = _rootB.AddComponent<BlockBlast.Features.GameFlow.Play.PlaySceneTransitionVfxController>();
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("already registered"));
            onEnable?.Invoke(vfxB, null);
            Assert.IsFalse(vfxB.enabled, "Duplicate PlaySceneTransitionVfxController should disable itself.");

            // Destroy duplicate
            onDisable?.Invoke(vfxB, null);
            UnityEngine.Object.DestroyImmediate(vfxB);

            // vfxA must still be registered
            Assert.IsTrue(ServiceLocator.TryGet<BlockBlast.Features.GameFlow.Play.IPlaySceneTransitionVfxService>(out var stillRegistered));
            Assert.AreSame(vfxA, stillRegistered, "Original PlaySceneTransitionVfxController must remain registered after duplicate destruction.");
        }
    }
}
