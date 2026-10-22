using System;
using System.Threading.Tasks;
using BlockBlast.Features.GameFlow.Play;
using BlockBlast.Features.Lobby;
using DG.Tweening;
using NUnit.Framework;
using UnityEngine;

namespace BlockBlast.Features.GameFlow.Navigation.Test
{
    public partial class LobbyControllerTest
    {
        [Test]
        public async Task RequestPlayAsync_WithExitView_WaitsForFogCoverBeforeStartingExit()
        {
            var exitView = CreateExitView();
            var fogTcs = new TaskCompletionSource<bool>();
            _fakeTransitionVfx.CoverOverride = fogTcs.Task;

            Task request = _controller.RequestPlayAsync(isWorldPlay: false);

            Assert.AreEqual(0, exitView.PlayExitCallCount, "Exit sequence must not start before fog cover completes.");
            Assert.AreEqual(1, _fakeCamera.MoveToPlayCallCount, "Camera should begin moving immediately.");
            Assert.AreEqual(1, _fakeTransitionVfx.CoverCallCount, "Fog cover should begin immediately.");

            fogTcs.SetResult(true);
            await Task.Yield();

            Assert.AreEqual(1, exitView.PlayExitCallCount, "Exit sequence should begin as soon as fog cover completes.");

            DOTween.Complete(exitView.gameObject, true);
            await request;

            Assert.AreEqual(1, _fakeNavigator.LoadCount, "Navigator should load Play after exit completes.");
        }

        [Test]
        public async Task RequestPlayAsync_WorldPlay_StartsFogAndCameraImmediatelyWhilePressPending()
        {
            var exitView = CreateExitView();
            var playFeedback = _btnPlay.GetComponent<LobbyWorldButtonFeedbackView>();
            playFeedback.PlayPressSequence();

            var fogTcs = new TaskCompletionSource<bool>();
            _fakeTransitionVfx.CoverOverride = fogTcs.Task;

            Task request = _controller.RequestPlayAsync(isWorldPlay: true);

            Assert.AreEqual(1, _fakeTransitionVfx.CoverCallCount, "Fog cover must start immediately on world Play.");
            Assert.AreEqual(1, _fakeCamera.MoveToPlayCallCount, "Camera move must start immediately on world Play.");
            Assert.AreEqual(0, exitView.PlayExitCallCount, "Exit sequence must wait for fog cover even on world Play.");
            Assert.AreEqual(0, _fakeNavigator.LoadCount, "Navigator must wait for fog cover and exit.");

            fogTcs.SetResult(true);
            await Task.Yield();

            Assert.AreEqual(1, exitView.PlayExitCallCount, "Exit sequence begins after fog completes.");

            DOTween.Complete(exitView.gameObject, true);
            DOTween.Complete(_btnPlay.transform, true);
            await request;

            Assert.AreEqual(1, _fakeNavigator.LoadCount, "Navigator loads after exit completes.");
        }

        [Test]
        public async Task RequestPlayAsync_CameraCompletedFirst_WaitsForExitBeforeLoadingPlay()
        {
            var exitView = CreateExitView();
            exitView.ShrinkDuration = 0.5f;

            Task request = _controller.RequestPlayAsync(isWorldPlay: false);

            Assert.AreEqual(0, _fakeNavigator.LoadCount, "Navigator must wait for exit sequence to finish.");
            Assert.AreEqual(1, exitView.PlayExitCallCount);

            DOTween.Complete(exitView.gameObject, true);
            await request;

            Assert.AreEqual(1, _fakeNavigator.LoadCount, "Navigator should load Play once exit sequence completes.");
        }

        [Test]
        public async Task RequestPlayAsync_FogFails_DoesNotStartExitOrResets_AndDoesNotNavigate()
        {
            var exitView = CreateExitView();
            _fakeTransitionVfx.ThrowOnCover = new InvalidOperationException("Fog cover failure");

            await _controller.RequestPlayAsync(isWorldPlay: false);

            Assert.AreEqual(0, exitView.PlayExitCallCount, "Exit sequence must not be called when fog fails.");
            Assert.AreEqual(0, _fakeNavigator.LoadCount, "Navigator must not navigate on transition failure.");
            Assert.IsTrue(_fakeCamera.SnapToLobbyCalled, "Camera should snap back to Lobby on transition failure.");
            Assert.AreEqual(1, _fakeTransitionVfx.ResetCallCount, "Fog service should be reset on transition failure.");
        }

        [Test]
        public async Task RequestPlayAsync_WithoutFogService_DoesNotStartExit()
        {
            var exitView = CreateExitView();
            ServiceLocator.Unregister<IPlaySceneTransitionVfxService>();

            await _controller.RequestPlayAsync(isWorldPlay: false);

            Assert.AreEqual(0, exitView.PlayExitCallCount, "Exit animation must not run without fog cover.");
            Assert.AreEqual(1, _fakeCamera.MoveToPlayCallCount);
            Assert.AreEqual(1, _fakeNavigator.LoadCount);
        }

        [Test]
        public async Task ExitView_PlayExitAsync_DeactivatesRoots_AndResetImmediateRestoresRoots()
        {
            var exitView = CreateExitView();
            Assert.IsTrue(_btnPlay.gameObject.activeSelf);
            Assert.IsTrue(_btnCustom.gameObject.activeSelf);
            Assert.IsTrue(_btnGameCenter.gameObject.activeSelf);
            Assert.IsTrue(_btnSettings.gameObject.activeSelf);

            Task exitTask = exitView.PlayExitAsync();
            DOTween.Complete(exitView.gameObject, true);
            await exitTask;

            Assert.IsFalse(_btnPlay.gameObject.activeSelf, "Play button root must be deactivated after exit.");
            Assert.IsFalse(_btnCustom.gameObject.activeSelf, "Customization button root must be deactivated after exit.");
            Assert.IsFalse(_btnGameCenter.gameObject.activeSelf, "GameCenter button root must be deactivated after exit.");
            Assert.IsFalse(_btnSettings.gameObject.activeSelf, "Settings button root must be deactivated after exit.");

            exitView.ResetImmediate();

            Assert.IsTrue(_btnPlay.gameObject.activeSelf, "Play button root must be restored after reset.");
            Assert.IsTrue(_btnCustom.gameObject.activeSelf, "Customization button root must be restored after reset.");
            Assert.IsTrue(_btnGameCenter.gameObject.activeSelf, "GameCenter button root must be restored after reset.");
            Assert.IsTrue(_btnSettings.gameObject.activeSelf, "Settings button root must be restored after reset.");
            Assert.AreEqual(Vector3.one, _btnPlay.transform.localScale);
        }

        [Test]
        public async Task RequestPlayAsync_CancelledDuringExit_ResetsRootsAndDoesNotNavigate()
        {
            var exitView = CreateExitView();
            exitView.ShrinkDuration = 5f;

            Task request = _controller.RequestPlayAsync(isWorldPlay: false);
            await Task.Yield();

            Assert.AreEqual(1, exitView.PlayExitCallCount);
            Assert.AreEqual(0, _fakeNavigator.LoadCount);

            InvokeOnDisable();
            await request;

            Assert.AreEqual(0, _fakeNavigator.LoadCount);
            Assert.IsTrue(_btnPlay.gameObject.activeSelf);
            Assert.IsTrue(_btnCustom.gameObject.activeSelf);
            Assert.IsTrue(_btnGameCenter.gameObject.activeSelf);
            Assert.IsTrue(_btnSettings.gameObject.activeSelf);
            Assert.AreEqual(Vector3.one, _btnPlay.transform.localScale);
        }

        private LobbyWorldButtonsExitView CreateExitView()
        {
            var exitView = _controllerObj.AddComponent<LobbyWorldButtonsExitView>();
            var fbPlay = CreateButtonFeedback(_btnPlay);
            var fbCustom = CreateButtonFeedback(_btnCustom);
            var fbGameCenter = CreateButtonFeedback(_btnGameCenter);
            var fbSettings = CreateButtonFeedback(_btnSettings);

            exitView.FeedbackViews = new[] { fbPlay, fbCustom, fbGameCenter, fbSettings };
            _controller.WorldButtonsExitView = exitView;
            return exitView;
        }

        private LobbyWorldButtonFeedbackView CreateButtonFeedback(LobbyWorldButtonView button)
        {
            var feedback = button.GetComponent<LobbyWorldButtonFeedbackView>();
            if (feedback == null)
            {
                feedback = button.gameObject.AddComponent<LobbyWorldButtonFeedbackView>();
                feedback.ButtonView = button;
                feedback.AnimatedRoot = button.transform;
                feedback.CaptureBaseState(force: true);
            }
            return feedback;
        }
    }
}
