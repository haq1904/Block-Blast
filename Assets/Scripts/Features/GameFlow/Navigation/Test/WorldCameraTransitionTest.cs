using System;
using System.Threading;
using System.Threading.Tasks;
using BlockBlast.Features.GameFlow.Camera;
using DG.Tweening;
using NUnit.Framework;
using UnityEngine;

namespace BlockBlast.Features.GameFlow.Navigation.Test
{
    [TestFixture]
    public class WorldCameraTransitionTest
    {
        private GameObject _viewObj;
        private WorldCameraTransitionView _view;
        private GameObject _camObj;
        private GameObject _lobbyPoseObj;
        private GameObject _playPoseObj;

        [SetUp]
        public void SetUp()
        {
            DOTween.Init();

            _viewObj = new GameObject("TestCameraTransitionView");
            _view = _viewObj.AddComponent<WorldCameraTransitionView>();

            _camObj = new GameObject("TestCamera");
            _lobbyPoseObj = new GameObject("LobbyPose");
            _lobbyPoseObj.transform.position = new Vector3(0, 38.8f, -8.6f);
            _playPoseObj = new GameObject("PlayPose");
            _playPoseObj.transform.position = new Vector3(0, 52.7f, -15.91f);

            _view.CameraTransform = _camObj.transform;
            _view.LobbyPose = _lobbyPoseObj.transform;
            _view.PlayPose = _playPoseObj.transform;
            _view.Duration = 0.05f; // Fast duration for test
        }

        [TearDown]
        public void TearDown()
        {
            DOTween.KillAll();

            if (_viewObj != null) UnityEngine.Object.DestroyImmediate(_viewObj);
            if (_camObj != null) UnityEngine.Object.DestroyImmediate(_camObj);
            if (_lobbyPoseObj != null) UnityEngine.Object.DestroyImmediate(_lobbyPoseObj);
            if (_playPoseObj != null) UnityEngine.Object.DestroyImmediate(_playPoseObj);
        }

        [Test]
        public void MoveToPlayAsync_WhenCameraTransformNull_ThrowsInvalidOperationException()
        {
            _view.CameraTransform = null;

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await _view.MoveToPlayAsync();
            });
        }

        [Test]
        public void MoveToPlayAsync_WhenPlayPoseNull_ThrowsInvalidOperationException()
        {
            _view.PlayPose = null;

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await _view.MoveToPlayAsync();
            });
        }

        [Test]
        public void MoveToPlayAsync_WhenAlreadyCancelled_ReturnsCanceledTask()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var task = _view.MoveToPlayAsync(cts.Token);
            Assert.IsTrue(task.IsCanceled, "Task must be canceled immediately if token was already canceled.");
        }

        [Test]
        public void MoveToPlayAsync_WhenSequenceKilledUnexpectedly_FaultsWithOperationCanceledException()
        {
            var task = _view.MoveToPlayAsync(CancellationToken.None);

            Assert.IsTrue(_view.IsMoving);

            // Kill camera transform tween unexpectedly
            _camObj.transform.DOKill();

            Assert.CatchAsync<OperationCanceledException>(async () =>
            {
                await task;
            });
        }

        [Test]
        public void SnapToLobby_SetsExactPositionAndRotation()
        {
            _camObj.transform.position = Vector3.one * 100f;
            _view.SnapToLobby();

            Assert.AreEqual(_lobbyPoseObj.transform.position, _camObj.transform.position);
            Assert.AreEqual(_lobbyPoseObj.transform.rotation, _camObj.transform.rotation);
        }
    }
}
