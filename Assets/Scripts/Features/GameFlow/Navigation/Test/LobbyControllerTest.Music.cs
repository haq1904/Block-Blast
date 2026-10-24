using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BlockBlast.Features.GameFlow.Audio;
using BlockBlast.Features.Lobby;
using NUnit.Framework;

namespace BlockBlast.Features.GameFlow.Navigation.Test
{
    public partial class LobbyControllerTest
    {
        private class FakeGameMusicService : IGameMusicService
        {
            public int PlayLobbyCallCount { get; private set; }
            public int TransitionToPlayingCallCount { get; private set; }
            public int RestoreLobbyImmediateCallCount { get; private set; }

            public TaskCompletionSource<bool> TransitionSignal { get; set; }
            public Exception TransitionException { get; set; }

            public void PlayLobby()
            {
                PlayLobbyCallCount++;
            }

            public async Task TransitionToPlayingAsync(CancellationToken cancellationToken = default)
            {
                TransitionToPlayingCallCount++;

                if (TransitionSignal != null)
                {
                    await TransitionSignal.Task;
                }

                if (TransitionException != null)
                {
                    throw TransitionException;
                }

                await Task.Yield();
            }

            public void RestoreLobbyImmediate()
            {
                RestoreLobbyImmediateCallCount++;
            }
        }

        [Test]
        public void Start_WhenMusicServiceRegistered_CallsPlayLobbyOnce()
        {
            var fakeMusic = new FakeGameMusicService();
            ServiceLocator.Register<IGameMusicService>(fakeMusic);

            var startMethod = typeof(LobbyController).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic);
            startMethod?.Invoke(_controller, null);

            Assert.AreEqual(1, fakeMusic.PlayLobbyCallCount);
        }

        [Test]
        public void Start_WithoutMusicService_DoesNotThrow()
        {
            ServiceLocator.Unregister<IGameMusicService>();

            var startMethod = typeof(LobbyController).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.DoesNotThrow(() => startMethod?.Invoke(_controller, null));
        }

        [Test]
        public async Task RequestPlayAsync_TriggersTransitionToPlayingOnce()
        {
            var fakeMusic = new FakeGameMusicService();
            ServiceLocator.Register<IGameMusicService>(fakeMusic);

            await _controller.RequestPlayAsync();

            Assert.AreEqual(1, fakeMusic.TransitionToPlayingCallCount);
            Assert.AreEqual(0, fakeMusic.RestoreLobbyImmediateCallCount);
        }

        [Test]
        public async Task RequestPlayAsync_WhileTransitionRunning_IgnoresDuplicateRequest()
        {
            var fakeMusic = new FakeGameMusicService
            {
                TransitionSignal = new TaskCompletionSource<bool>()
            };
            ServiceLocator.Register<IGameMusicService>(fakeMusic);

            Task task1 = _controller.RequestPlayAsync();
            Task task2 = _controller.RequestPlayAsync();

            fakeMusic.TransitionSignal.SetResult(true);
            await Task.WhenAll(task1, task2);

            Assert.AreEqual(1, fakeMusic.TransitionToPlayingCallCount, "Duplicate RequestPlayAsync must not invoke music transition twice.");
        }

        [Test]
        public async Task RequestPlayAsync_WhenTransitionFails_RestoresLobbyMusic()
        {
            var fakeMusic = new FakeGameMusicService
            {
                TransitionException = new InvalidOperationException("Simulated music failure")
            };
            ServiceLocator.Register<IGameMusicService>(fakeMusic);

            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Warning, new System.Text.RegularExpressions.Regex("Play transition failed"));

            await _controller.RequestPlayAsync();

            Assert.AreEqual(1, fakeMusic.TransitionToPlayingCallCount);
            Assert.IsTrue(fakeMusic.RestoreLobbyImmediateCallCount >= 1, "Lobby music must be restored when transition fails.");
            Assert.IsTrue(_btnPlay.IsInteractable, "World buttons should be restored after transition failure.");
        }

        [Test]
        public async Task RequestPlayAsync_WhenNavigatorFails_RestoresLobbyMusic()
        {
            var fakeMusic = new FakeGameMusicService();
            ServiceLocator.Register<IGameMusicService>(fakeMusic);

            _fakeNavigator.ThrowOnLoad = new InvalidOperationException("Simulated navigation failure");
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Exception, new System.Text.RegularExpressions.Regex("Simulated navigation failure"));

            await _controller.RequestPlayAsync();

            Assert.AreEqual(1, fakeMusic.TransitionToPlayingCallCount);
            Assert.IsTrue(fakeMusic.RestoreLobbyImmediateCallCount >= 1, "Lobby music must be restored when navigator fails.");
            Assert.IsTrue(_btnPlay.IsInteractable, "World buttons should be restored after navigator failure.");
        }
    }
}
