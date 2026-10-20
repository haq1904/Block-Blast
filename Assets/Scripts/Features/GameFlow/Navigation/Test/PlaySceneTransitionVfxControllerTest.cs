using System.Threading.Tasks;
using System.Reflection;
using BlockBlast.Features.GameFlow.Play;
using NUnit.Framework;
using UnityEngine;

namespace BlockBlast.Features.GameFlow.Navigation.Test
{
    [TestFixture]
    public sealed class PlaySceneTransitionVfxControllerTest
    {
        private GameObject _controllerObject;
        private PlaySceneTransitionVfxController _controller;

        [SetUp]
        public void SetUp()
        {
            ServiceLocator.ClearAll();
            _controllerObject = new GameObject("TestPlaySceneTransitionVfxController");
            _controller = _controllerObject.AddComponent<PlaySceneTransitionVfxController>();
            InvokeLifecycle(_controller, "OnEnable");
        }

        [TearDown]
        public void TearDown()
        {
            if (_controllerObject != null)
            {
                Object.DestroyImmediate(_controllerObject);
            }

            ServiceLocator.ClearAll();
        }

        [Test]
        public void OnEnable_RegistersTransitionService()
        {
            bool found = ServiceLocator.TryGet<IPlaySceneTransitionVfxService>(out var service);

            Assert.IsTrue(found);
            Assert.AreSame(_controller, service);
        }

        [Test]
        public void OnDisable_OwnerSafeUnregisterPreservesReplacementService()
        {
            var replacementObject = new GameObject("ReplacementPlaySceneTransitionVfxController");
            var replacement = replacementObject.AddComponent<PlaySceneTransitionVfxController>();
            InvokeLifecycle(replacement, "OnEnable");

            InvokeLifecycle(_controller, "OnDisable");
            Object.DestroyImmediate(_controllerObject);
            _controllerObject = null;

            bool found = ServiceLocator.TryGet<IPlaySceneTransitionVfxService>(out var service);
            Assert.IsTrue(found);
            Assert.AreSame(replacement, service);

            Object.DestroyImmediate(replacementObject);
        }

        private static void InvokeLifecycle(PlaySceneTransitionVfxController controller, string methodName)
        {
            typeof(PlaySceneTransitionVfxController)
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(controller, null);
        }

        [Test]
        public async Task CoverTableAsync_WithoutView_CompletesAndArmsReveal()
        {
            Task coverTask = _controller.CoverTableAsync();
            await coverTask;

            Assert.IsTrue(_controller.IsCoverRequested);
            Assert.IsTrue(_controller.IsCoverCompleted);

            _controller.HandleActiveSceneChanged(default, _controller.gameObject.scene);

            Assert.IsFalse(_controller.IsCoverRequested);
            Assert.IsFalse(_controller.IsCoverCompleted);
        }

        [Test]
        public async Task ResetImmediate_ClearsPendingCoverState()
        {
            await _controller.CoverTableAsync();

            _controller.ResetImmediate();

            Assert.IsFalse(_controller.IsCoverRequested);
            Assert.IsFalse(_controller.IsCoverCompleted);
        }
    }
}
