using System.Threading.Tasks;
using BlockBlast.Features.Lobby;
using DG.Tweening;
using NUnit.Framework;
using UnityEngine;

namespace BlockBlast.Features.GameFlow.Navigation.Test
{
    public partial class LobbyControllerTest
    {
        [SetUp]
        public void SetUpPressMotionTests()
        {
            DOTween.Init();
        }

        [TearDown]
        public void TearDownPressMotionTests()
        {
            DOTween.KillAll();
        }

        [Test]
        public async Task RequestPlayAsync_WorldPlay_WaitsForPressMotionBeforeFogAndCamera()
        {
            var feedback = CreatePlayFeedback();
            _controller.WorldPlayDelayAfterPress = 0f;
            feedback.PlayPressSequence();

            Task request = _controller.RequestPlayAsync(isWorldPlay: true);

            Assert.AreEqual(0, _fakeTransitionVfx.CoverCallCount);
            Assert.AreEqual(0, _fakeCamera.MoveToPlayCallCount);

            DOTween.Complete(_btnPlay.transform, true);
            await request;

            Assert.AreEqual(1, _fakeTransitionVfx.CoverCallCount);
            Assert.AreEqual(1, _fakeCamera.MoveToPlayCallCount);
        }

        [Test]
        public async Task RequestPlayAsync_WorldPlay_IgnoresParticleTailAfterPressMotion()
        {
            var feedback = CreatePlayFeedback();
            var particleObject = new GameObject("LongParticleTail");
            _createdObjects.Add(particleObject);
            var particles = particleObject.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startLifetime = 10f;
            feedback.EdgeEffects = new[]
            {
                new LobbyWorldButtonFeedbackView.EdgeEffect { Debris = particles }
            };
            _controller.WorldPlayDelayAfterPress = 0f;
            feedback.PlayPressSequence();

            Task request = _controller.RequestPlayAsync(isWorldPlay: true);
            DOTween.Complete(_btnPlay.transform, true);
            await request;

            Assert.AreEqual(1, _fakeTransitionVfx.CoverCallCount);
            Assert.AreEqual(1, _fakeCamera.MoveToPlayCallCount);
            Assert.IsTrue(particles.isPlaying, "The particle tail may continue after fog and camera transition start.");
        }

        [Test]
        public async Task RequestPlayAsync_WorldPlay_PostPressDelayCancellation_DoesNotStartTransition()
        {
            var feedback = CreatePlayFeedback();
            _controller.WorldPlayDelayAfterPress = 10f;
            feedback.PlayPressSequence();

            Task request = _controller.RequestPlayAsync(isWorldPlay: true);
            DOTween.Complete(_btnPlay.transform, true);
            await Task.Yield();

            Assert.AreEqual(0, _fakeTransitionVfx.CoverCallCount);
            Assert.AreEqual(0, _fakeCamera.MoveToPlayCallCount);

            InvokeOnDisable();
            await request;

            Assert.AreEqual(0, _fakeTransitionVfx.CoverCallCount);
            Assert.AreEqual(0, _fakeCamera.MoveToPlayCallCount);
        }

        private LobbyWorldButtonFeedbackView CreatePlayFeedback()
        {
            var feedback = _btnPlay.gameObject.AddComponent<LobbyWorldButtonFeedbackView>();
            feedback.ButtonView = _btnPlay;
            feedback.AnimatedRoot = _btnPlay.transform;
            feedback.CaptureBaseState(force: true);
            _controller.PlayWorldButtonFeedback = feedback;
            return feedback;
        }
    }
}
