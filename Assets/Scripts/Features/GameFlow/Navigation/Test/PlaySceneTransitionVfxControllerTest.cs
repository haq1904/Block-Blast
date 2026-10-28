using System.Reflection;
using System.Threading.Tasks;
using BlockBlast.Features.GameFlow.Play;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

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
                InvokeLifecycle(_controller, "OnDisable");
                InvokeLifecycle(_controller, "OnDestroy");
                Object.DestroyImmediate(_controllerObject);
            }

            ServiceLocator.ClearAll();
        }

        [Test]
        public void OnEnable_DesignatedOwner_RegistersTransitionService()
        {
            bool found = ServiceLocator.TryGet<IPlaySceneTransitionVfxService>(out var service);

            Assert.IsTrue(found);
            Assert.AreSame(_controller, service);
            Assert.IsTrue(_controller.IsServiceRegistered);
            Assert.IsTrue(_controller.IsActiveSceneSubscribed);
        }

        [Test]
        public void OnEnable_RepeatedCallsOnSameOwner_IsIdempotent()
        {
            // Invoke OnEnable again on the already enabled controller
            InvokeLifecycle(_controller, "OnEnable");

            bool found = ServiceLocator.TryGet<IPlaySceneTransitionVfxService>(out var service);
            Assert.IsTrue(found);
            Assert.AreSame(_controller, service);
            Assert.IsTrue(_controller.IsServiceRegistered);
            Assert.IsTrue(_controller.IsActiveSceneSubscribed);
        }

        [Test]
        public void OnEnable_PresentationOnly_DoesNotRegisterService()
        {
            var presentationObject = new GameObject("PresentationOnlyFog");
            var presentationController = presentationObject.AddComponent<PlaySceneTransitionVfxController>();
            presentationController.RegisterAsTransitionService = false;
            InvokeLifecycle(presentationController, "OnEnable");

            Assert.IsFalse(presentationController.IsServiceRegistered);
            Assert.IsTrue(presentationController.IsActiveSceneSubscribed);

            bool found = ServiceLocator.TryGet<IPlaySceneTransitionVfxService>(out var service);
            Assert.IsTrue(found);
            Assert.AreSame(_controller, service, "Canonical service provider must remain untouched.");

            InvokeLifecycle(presentationController, "OnDisable");
            InvokeLifecycle(presentationController, "OnDestroy");
            Object.DestroyImmediate(presentationObject);

            Assert.IsTrue(ServiceLocator.TryGet<IPlaySceneTransitionVfxService>(out var afterDestroyService));
            Assert.AreSame(_controller, afterDestroyService, "Destroying non-owner must not unregister canonical service.");
        }

        [Test]
        public void OnEnable_DuplicateDesignatedOwner_PreservesFirstProviderAndDisablesDuplicate()
        {
            LogAssert.Expect(LogType.Warning, "[PlaySceneTransitionVfxController] An active IPlaySceneTransitionVfxService is already registered. Disabling duplicate controller.");

            var duplicateObject = new GameObject("DuplicateOwnerController");
            var duplicateController = duplicateObject.AddComponent<PlaySceneTransitionVfxController>();
            InvokeLifecycle(duplicateController, "OnEnable");

            Assert.IsFalse(duplicateController.enabled, "Duplicate controller must disable itself.");
            Assert.IsFalse(duplicateController.IsServiceRegistered, "Duplicate controller must not be registered.");
            Assert.IsFalse(duplicateController.IsActiveSceneSubscribed, "Duplicate controller must not remain subscribed.");

            bool found = ServiceLocator.TryGet<IPlaySceneTransitionVfxService>(out var service);
            Assert.IsTrue(found);
            Assert.AreSame(_controller, service, "First designated owner must remain registered.");

            InvokeLifecycle(duplicateController, "OnDisable");
            InvokeLifecycle(duplicateController, "OnDestroy");
            Object.DestroyImmediate(duplicateObject);

            Assert.IsTrue(ServiceLocator.TryGet<IPlaySceneTransitionVfxService>(out var afterDestroyService));
            Assert.AreSame(_controller, afterDestroyService, "Destroying duplicate must not unregister original owner.");
        }

        [Test]
        public void OnDisable_OwnerUnregistersService_WhenMatchingExpectedOwner()
        {
            InvokeLifecycle(_controller, "OnDisable");

            Assert.IsFalse(_controller.IsServiceRegistered);
            Assert.IsFalse(_controller.IsActiveSceneSubscribed);
            Assert.IsFalse(ServiceLocator.TryGet<IPlaySceneTransitionVfxService>(out _), "Service must be unregistered when owner disables.");
        }

        [Test]
        public void OnDisable_NonOwnerCleanup_PreservesCanonicalService()
        {
            var nonOwnerObject = new GameObject("NonOwnerController");
            var nonOwner = nonOwnerObject.AddComponent<PlaySceneTransitionVfxController>();
            nonOwner.RegisterAsTransitionService = false;
            InvokeLifecycle(nonOwner, "OnEnable");

            InvokeLifecycle(nonOwner, "OnDisable");
            InvokeLifecycle(nonOwner, "OnDestroy");
            Object.DestroyImmediate(nonOwnerObject);

            bool found = ServiceLocator.TryGet<IPlaySceneTransitionVfxService>(out var service);
            Assert.IsTrue(found);
            Assert.AreSame(_controller, service, "Disabling/destroying non-owner must not unregister canonical service.");
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

        [Test]
        public void HandleActiveSceneChanged_AfterDestroy_DoesNotThrow()
        {
            var targetController = _controller;
            Object.DestroyImmediate(_controllerObject);
            _controllerObject = null;
            _controller = null;

            Assert.DoesNotThrow(() =>
            {
                targetController.HandleActiveSceneChanged(default, default);
            });
        }

        private static void InvokeLifecycle(PlaySceneTransitionVfxController controller, string methodName)
        {
            typeof(PlaySceneTransitionVfxController)
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(controller, null);
        }
    }
}
