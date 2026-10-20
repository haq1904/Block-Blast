using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BlockBlast.Features.GameFlow.Camera;
using BlockBlast.Features.GameFlow.Navigation;
using BlockBlast.Features.GameFlow.Play;
using BlockBlast.Features.Lobby;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BlockBlast.Features.GameFlow.Navigation.Test
{
    [TestFixture]
    public class LobbyControllerTest
    {
        private readonly List<GameObject> _createdObjects = new List<GameObject>();
        private GameObject _controllerObj;
        private LobbyController _controller;
        private LobbyWorldButtonView _btnPlay;
        private LobbyWorldButtonView _btnCustom;
        private LobbyWorldButtonView _btnGameCenter;
        private LobbyWorldButtonView _btnSettings;
        private FakeCameraService _fakeCamera;
        private FakeNavigator _fakeNavigator;
        private FakeTransitionVfxService _fakeTransitionVfx;
        private int _lobbyLayer;

        private LobbyWorldButtonView CreateButton(string name, LobbyWorldButtonAction action, bool addCollider = true, bool correctLayer = true)
        {
            var go = new GameObject(name);
            _createdObjects.Add(go);
            if (correctLayer)
            {
                go.layer = _lobbyLayer != -1 ? _lobbyLayer : 8;
            }
            Collider col = null;
            if (addCollider)
            {
                col = go.AddComponent<BoxCollider>();
            }
            var view = go.AddComponent<LobbyWorldButtonView>();
            view.Action = action;
            view.HitCollider = col;
            return view;
        }

        [SetUp]
        public void SetUp()
        {
            ServiceLocator.ClearAll();
            _createdObjects.Clear();

            _fakeCamera = new FakeCameraService();
            _fakeNavigator = new FakeNavigator();
            _fakeTransitionVfx = new FakeTransitionVfxService();

            ServiceLocator.Register<IWorldCameraTransitionService>(_fakeCamera);
            ServiceLocator.Register<IGameSceneNavigator>(_fakeNavigator);
            ServiceLocator.Register<IPlaySceneTransitionVfxService>(_fakeTransitionVfx);

            _lobbyLayer = LayerMask.NameToLayer("LobbyInteractable");
            if (_lobbyLayer == -1) _lobbyLayer = 8;

            _btnPlay = CreateButton("TestBtnPlay", LobbyWorldButtonAction.Play);
            _btnCustom = CreateButton("TestBtnCustom", LobbyWorldButtonAction.Customization);
            _btnGameCenter = CreateButton("TestBtnGameCenter", LobbyWorldButtonAction.GameCenter);
            _btnSettings = CreateButton("TestBtnSettings", LobbyWorldButtonAction.Settings);

            _controllerObj = new GameObject("TestLobbyController");
            _createdObjects.Add(_controllerObj);
            _controller = _controllerObj.AddComponent<LobbyController>();
            _controller.WorldButtons = new[] { _btnPlay, _btnCustom, _btnGameCenter, _btnSettings };
            _controller.PlayTransitionDelay = 0f;

            var awake = typeof(LobbyController).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var onEnable = typeof(LobbyController).GetMethod("OnEnable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            awake?.Invoke(_controller, null);
            onEnable?.Invoke(_controller, null);
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _createdObjects.Count; i++)
            {
                if (_createdObjects[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(_createdObjects[i]);
                }
            }
            _createdObjects.Clear();
            ServiceLocator.ClearAll();
        }

        [Test]
        public void EventLifecycle_DisableThenEnable_PreservesSingleBinding()
        {
            int clickCount = 0;
            _fakeCamera.OnMoveToPlay = () => clickCount++;

            var onDisable = typeof(LobbyController).GetMethod("OnDisable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var onEnable = typeof(LobbyController).GetMethod("OnEnable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            // Disable then re-enable controller
            onDisable?.Invoke(_controller, null);
            onEnable?.Invoke(_controller, null);

            // Trigger button click
            _btnPlay.OnPointerClick(null);

            Assert.AreEqual(1, clickCount, "Click handler must be invoked exactly once after disable/enable cycle.");
        }

        [Test]
        public async Task RequestPlayAsync_DoubleClick_OnlyTriggersOneNavigationRequest()
        {
            var tcs = new TaskCompletionSource<bool>();
            _fakeNavigator.LoadOverride = tcs.Task;

            var task1 = _controller.RequestPlayAsync();
            var task2 = _controller.RequestPlayAsync();

            tcs.SetResult(true);
            await Task.WhenAll(task1, task2);

            Assert.AreEqual(1, _fakeNavigator.LoadCount, "Double click must only trigger a single LoadAsync request.");
        }

        [Test]
        public async Task RequestPlayAsync_CameraCompletesBeforeNavigatorLoad()
        {
            var executionOrder = new List<string>();

            _fakeCamera.OnMoveToPlay = () => executionOrder.Add("CameraCompleted");
            _fakeNavigator.OnLoad = () => executionOrder.Add("NavigatorStarted");

            await _controller.RequestPlayAsync();

            Assert.AreEqual(2, executionOrder.Count);
            Assert.AreEqual("CameraCompleted", executionOrder[0]);
            Assert.AreEqual("NavigatorStarted", executionOrder[1]);
        }

        [Test]
        public async Task RequestPlayAsync_WaitsForFogCoverAndCameraBeforeNavigatorLoad()
        {
            var coverCompletion = new TaskCompletionSource<bool>();
            var cameraCompletion = new TaskCompletionSource<bool>();
            _fakeTransitionVfx.CoverOverride = coverCompletion.Task;
            _fakeCamera.MoveToPlayOverride = cameraCompletion.Task;

            Task request = _controller.RequestPlayAsync();

            Assert.AreEqual(1, _fakeTransitionVfx.CoverCallCount);
            Assert.AreEqual(1, _fakeCamera.MoveToPlayCallCount);
            Assert.AreEqual(0, _fakeNavigator.LoadCount);

            cameraCompletion.SetResult(true);
            await Task.Yield();
            Assert.AreEqual(0, _fakeNavigator.LoadCount, "Navigator must wait for fog cover.");

            coverCompletion.SetResult(true);
            await request;

            Assert.AreEqual(1, _fakeNavigator.LoadCount);
        }

        [Test]
        public async Task RequestPlayAsync_WhenFogCoverFails_ResetsFogAndDoesNotNavigate()
        {
            _fakeTransitionVfx.ThrowOnCover = new InvalidOperationException("Fog cover failed");
            LogAssert.Expect(LogType.Warning, new Regex("Play transition failed"));

            await _controller.RequestPlayAsync();

            Assert.AreEqual(1, _fakeTransitionVfx.ResetCallCount);
            Assert.IsTrue(_fakeCamera.SnapToLobbyCalled);
            Assert.AreEqual(0, _fakeNavigator.LoadCount);
        }

        [Test]
        public async Task RequestPlayAsync_UsesPlayRouteWithoutLoadingScreen()
        {
            await _controller.RequestPlayAsync();

            Assert.AreEqual(GameSceneRoute.Play, _fakeNavigator.LastRoute);
            Assert.AreEqual(SceneTransitionPresentation.None, _fakeNavigator.LastPresentation);
            Assert.AreEqual(0f, _fakeNavigator.LastMinimumVisibleDuration, 0.0001f);
        }

        [Test]
        public async Task RequestPlayAsync_DelayLocksInputBeforeCameraAndCancellationStopsFlow()
        {
            _controller.PlayTransitionDelay = 10f;
            Task request = _controller.RequestPlayAsync();

            Assert.IsFalse(_btnPlay.IsInteractable, "World input must be disabled before waiting.");
            Assert.AreEqual(0, _fakeCamera.MoveToPlayCallCount, "Camera must wait for the configured delay.");
            Assert.AreEqual(0, _fakeNavigator.LoadCount, "Navigation must wait for the configured delay.");

            typeof(LobbyController).GetMethod("OnDisable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.Invoke(_controller, null);
            await request;

            Assert.AreEqual(0, _fakeCamera.MoveToPlayCallCount);
            Assert.AreEqual(0, _fakeNavigator.LoadCount);
        }

        [Test]
        public async Task RequestPlayAsync_WhenCameraFails_SnapsCameraToLobbyAndRestoresInput()
        {
            _fakeCamera.ThrowOnMoveToPlay = new InvalidOperationException("Camera tween exploded");

            await _controller.RequestPlayAsync();

            Assert.IsTrue(_fakeCamera.SnapToLobbyCalled, "Camera must snap back to Lobby on failure.");
            Assert.IsTrue(_btnPlay.IsInteractable, "Buttons must be restored to interactable on camera failure.");
            Assert.AreEqual(0, _fakeNavigator.LoadCount, "Navigator must not be called when camera fails.");
        }

        [Test]
        public async Task RequestPlayAsync_WhenNavigatorFails_MovesCameraBackToLobbyAndRestoresInput()
        {
            _fakeNavigator.ThrowOnLoad = new InvalidOperationException("Scene transition failed");
            LogAssert.Expect(LogType.Exception, new Regex("Scene transition failed"));

            await _controller.RequestPlayAsync();

            Assert.IsTrue(_fakeCamera.MoveToLobbyCalled, "Camera must move back to Lobby when navigation fails.");
            Assert.IsTrue(_btnPlay.IsInteractable, "Buttons must be restored to interactable on navigation failure.");
        }

        [Test]
        public void ValidateWorldButtons_WhenValid_ReturnsTrue()
        {
            Assert.IsTrue(_controller.ValidateWorldButtons());
        }

        [Test]
        public void ValidateWorldButtons_WhenCountNotFour_ReturnsFalseAndLogsError()
        {
            LogAssert.Expect(LogType.Error, new Regex("must contain exactly 4"));
            _controller.WorldButtons = new[] { _btnPlay, _btnCustom };

            bool valid = _controller.ValidateWorldButtons();

            Assert.IsFalse(valid);
        }

        [Test]
        public void ValidateWorldButtons_WhenNullEntry_ReturnsFalseAndLogsError()
        {
            LogAssert.Expect(LogType.Error, new Regex("World button at index 1 is null"));
            _controller.WorldButtons = new[] { _btnPlay, null, _btnGameCenter, _btnSettings };

            bool valid = _controller.ValidateWorldButtons();

            Assert.IsFalse(valid);
        }

        [Test]
        public void ValidateWorldButtons_WhenDuplicateView_ReturnsFalseAndLogsError()
        {
            LogAssert.Expect(LogType.Error, new Regex("Duplicate button view reference"));
            _controller.WorldButtons = new[] { _btnPlay, _btnPlay, _btnGameCenter, _btnSettings };

            bool valid = _controller.ValidateWorldButtons();

            Assert.IsFalse(valid);
        }

        [Test]
        public void ValidateWorldButtons_WhenDuplicateAction_ReturnsFalseAndLogsError()
        {
            LogAssert.Expect(LogType.Error, new Regex("Duplicate action"));
            var dupPlay = CreateButton("DupPlay", LobbyWorldButtonAction.Play);
            _controller.WorldButtons = new[] { _btnPlay, dupPlay, _btnGameCenter, _btnSettings };

            bool valid = _controller.ValidateWorldButtons();

            Assert.IsFalse(valid);
        }

        [Test]
        public void ValidateWorldButtons_WhenMissingAction_ReturnsFalseAndLogsError()
        {
            LogAssert.Expect(LogType.Error, new Regex("must contain actions: Play, Customization, GameCenter, and Settings"));
            var unknownActionBtn = CreateButton("UnknownAction", (LobbyWorldButtonAction)99);
            _controller.WorldButtons = new[] { _btnPlay, _btnCustom, _btnGameCenter, unknownActionBtn };

            bool valid = _controller.ValidateWorldButtons();

            Assert.IsFalse(valid);
        }

        [Test]
        public void ValidateWorldButtons_WhenColliderMissing_ReturnsFalseAndLogsError()
        {
            LogAssert.Expect(LogType.Error, new Regex("missing a Collider"));
            var noCol = CreateButton("NoCol", LobbyWorldButtonAction.Play, addCollider: false);
            _controller.WorldButtons = new[] { noCol, _btnCustom, _btnGameCenter, _btnSettings };

            bool valid = _controller.ValidateWorldButtons();

            Assert.IsFalse(valid);
        }

        [Test]
        public void ValidateWorldButtons_WhenColliderBelongsToDifferentObject_ReturnsFalseAndLogsError()
        {
            LogAssert.Expect(LogType.Error, new Regex("belongs to another GameObject"));
            var otherObj = new GameObject("OtherObj");
            _createdObjects.Add(otherObj);
            var otherCol = otherObj.AddComponent<BoxCollider>();

            var badButton = CreateButton("BadColButton", LobbyWorldButtonAction.Play, addCollider: false);
            badButton.HitCollider = otherCol;

            _controller.WorldButtons = new[] { badButton, _btnCustom, _btnGameCenter, _btnSettings };

            bool valid = _controller.ValidateWorldButtons();

            Assert.IsFalse(valid);
        }

        [Test]
        public void ValidateWorldButtons_WhenWrongLayer_ReturnsFalseAndLogsError()
        {
            var wrongLayerBtn = CreateButton("WrongLayerBtn", LobbyWorldButtonAction.Play, correctLayer: false);
            wrongLayerBtn.gameObject.layer = 0; // Default layer

            LogAssert.Expect(LogType.Error, new Regex("expected 'LobbyInteractable'"));
            _controller.WorldButtons = new[] { wrongLayerBtn, _btnCustom, _btnGameCenter, _btnSettings };

            bool valid = _controller.ValidateWorldButtons();

            Assert.IsFalse(valid);
        }

        [Test]
        public void ValidateWorldButtons_WhenColliderDisabled_ReturnsFalseAndLogsError()
        {
            _btnPlay.HitCollider.enabled = false;
            LogAssert.Expect(LogType.Error, new Regex("collider is disabled"));

            Assert.IsFalse(_controller.ValidateWorldButtons());
        }

        [Test]
        public void OnEnable_WhenInitialColliderDisabled_DoesNotBindUntilFixed()
        {
            var onDisable = typeof(LobbyController).GetMethod("OnDisable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var onEnable = typeof(LobbyController).GetMethod("OnEnable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            onDisable?.Invoke(_controller, null);
            typeof(LobbyController).GetField("_hasBoundValidButtons", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(_controller, false);
            _btnPlay.HitCollider.enabled = false;
            LogAssert.Expect(LogType.Error, new Regex("collider is disabled"));
            onEnable?.Invoke(_controller, null);

            _btnPlay.HitCollider.enabled = true;
            int clickCount = 0;
            _fakeCamera.OnMoveToPlay = () => clickCount++;
            _btnPlay.OnPointerClick(null);
            Assert.AreEqual(0, clickCount);

            onDisable?.Invoke(_controller, null);
            onEnable?.Invoke(_controller, null);
            _btnPlay.OnPointerClick(null);
            Assert.AreEqual(1, clickCount);
        }

        [Test]
        public void OnEnable_WhenPreviouslyBoundColliderTemporarilyDisabled_AllowsRecovery()
        {
            var onDisable = typeof(LobbyController).GetMethod("OnDisable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var onEnable = typeof(LobbyController).GetMethod("OnEnable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            onDisable?.Invoke(_controller, null);
            _btnPlay.HitCollider.enabled = false;
            onEnable?.Invoke(_controller, null);

            int clickCount = 0;
            _fakeCamera.OnMoveToPlay = () => clickCount++;
            _btnPlay.OnPointerClick(null);
            Assert.AreEqual(0, clickCount);

            _btnPlay.HitCollider.enabled = true;
            _btnPlay.OnPointerClick(null);
            Assert.AreEqual(1, clickCount);
        }

        [Test]
        public void OnEnable_WhenValidationFails_AbortsBindingAndDoesNotTriggerClick()
        {
            var onDisable = typeof(LobbyController).GetMethod("OnDisable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var onEnable = typeof(LobbyController).GetMethod("OnEnable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            onDisable?.Invoke(_controller, null);

            // Assign invalid configuration
            _controller.WorldButtons = new[] { _btnPlay };
            LogAssert.Expect(LogType.Error, new Regex("must contain exactly 4"));

            onEnable?.Invoke(_controller, null);

            int clickCount = 0;
            _fakeCamera.OnMoveToPlay = () => clickCount++;

            _btnPlay.OnPointerClick(null);
            Assert.AreEqual(0, clickCount, "Click must not trigger transition when OnEnable validation failed.");

            // Restore valid configuration and re-enable
            onDisable?.Invoke(_controller, null);
            _controller.WorldButtons = new[] { _btnPlay, _btnCustom, _btnGameCenter, _btnSettings };
            onEnable?.Invoke(_controller, null);

            _btnPlay.OnPointerClick(null);
            Assert.AreEqual(1, clickCount, "Click must trigger transition once re-enabled with valid configuration.");
        }

        [Test]
        public void WorldButton_Customization_SwitchesTabToCustomization()
        {
            _btnCustom.OnPointerClick(null);
            Assert.AreEqual(LobbyTabId.Customization, _controller.CurrentTab);
        }

        [Test]
        public void WorldButton_GameCenter_SwitchesTabToGameCenter()
        {
            _btnGameCenter.OnPointerClick(null);
            Assert.AreEqual(LobbyTabId.GameCenter, _controller.CurrentTab);
        }

        [Test]
        public void LobbyWorldButtonView_WhenColliderMissing_IsInteractableIsFalse()
        {
            var btn = CreateButton("BtnNoCol", LobbyWorldButtonAction.Play, addCollider: false);
            Assert.IsFalse(btn.IsInteractable);

            LogAssert.Expect(LogType.Error, new Regex("Missing Collider on BtnNoCol"));
            btn.OnPointerClick(null);
        }

        [Test]
        public void LobbyWorldButtonView_WhenColliderDisabled_IsInteractableIsFalse()
        {
            _btnPlay.HitCollider.enabled = false;
            Assert.IsFalse(_btnPlay.IsInteractable);
        }

        [Test]
        public void LobbyWorldButtonView_WhenColliderBelongsToDifferentObject_IsInteractableIsFalse()
        {
            var otherObj = new GameObject("OtherObj");
            _createdObjects.Add(otherObj);
            var otherCol = otherObj.AddComponent<BoxCollider>();

            _btnPlay.HitCollider = otherCol;
            Assert.IsFalse(_btnPlay.IsInteractable);

            _btnPlay.SetInteractable(false);
            Assert.IsTrue(otherCol.enabled, "A button must not toggle a Collider owned by another GameObject.");

            LogAssert.Expect(LogType.Error, new Regex("belongs to another GameObject"));
            _btnPlay.OnPointerClick(null);
        }

        // --- Fakes ---

        private sealed class FakeCameraService : IWorldCameraTransitionService
        {
            public bool IsMoving { get; set; }
            public bool SnapToLobbyCalled { get; private set; }
            public bool MoveToLobbyCalled { get; private set; }
            public int MoveToPlayCallCount { get; private set; }
            public Action OnMoveToPlay { get; set; }
            public Exception ThrowOnMoveToPlay { get; set; }
            public Task MoveToPlayOverride { get; set; }

            public Task MoveToPlayAsync(CancellationToken cancellationToken = default)
            {
                if (ThrowOnMoveToPlay != null) throw ThrowOnMoveToPlay;
                MoveToPlayCallCount++;
                OnMoveToPlay?.Invoke();
                return MoveToPlayOverride ?? Task.CompletedTask;
            }

            public Task MoveToLobbyAsync(CancellationToken cancellationToken = default)
            {
                MoveToLobbyCalled = true;
                return Task.CompletedTask;
            }

            public void SnapToLobby()
            {
                SnapToLobbyCalled = true;
            }
        }

        private sealed class FakeTransitionVfxService : IPlaySceneTransitionVfxService
        {
            public int CoverCallCount { get; private set; }
            public int ResetCallCount { get; private set; }
            public Task CoverOverride { get; set; }
            public Exception ThrowOnCover { get; set; }

            public Task CoverTableAsync(CancellationToken cancellationToken = default)
            {
                CoverCallCount++;
                if (ThrowOnCover != null) throw ThrowOnCover;
                return CoverOverride ?? Task.CompletedTask;
            }

            public void ResetImmediate()
            {
                ResetCallCount++;
            }
        }

        private sealed class FakeNavigator : IGameSceneNavigator
        {
            public bool IsTransitioning { get; set; }
            public int LoadCount { get; private set; }
            public Action OnLoad { get; set; }
            public Task LoadOverride { get; set; }
            public Exception ThrowOnLoad { get; set; }
            public GameSceneRoute LastRoute { get; private set; }
            public SceneTransitionPresentation LastPresentation { get; private set; }
            public float LastMinimumVisibleDuration { get; private set; }

            public async Task LoadAsync(
                GameSceneRoute route,
                SceneTransitionPresentation presentation,
                float minimumVisibleDuration = 0,
                CancellationToken cancellationToken = default)
            {
                LoadCount++;
                LastRoute = route;
                LastPresentation = presentation;
                LastMinimumVisibleDuration = minimumVisibleDuration;
                OnLoad?.Invoke();
                if (ThrowOnLoad != null) throw ThrowOnLoad;
                if (LoadOverride != null) await LoadOverride;
            }
        }
    }
}
