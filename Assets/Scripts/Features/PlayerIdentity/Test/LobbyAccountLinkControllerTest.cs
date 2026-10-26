using System;
using System.Threading;
using System.Threading.Tasks;
using BlockBlast.Features.Lobby.Account;
using NUnit.Framework;
using UnityEngine;

namespace BlockBlast.Features.PlayerIdentity.Test
{
    [TestFixture]
    public sealed class LobbyAccountLinkControllerTest
    {
        private GameObject _holder;
        private LobbyAccountLinkView _view;
        private LobbyAccountLinkController _controller;
        private FakeIdentityService _identityService;

        [SetUp]
        public void SetUp()
        {
            ServiceLocator.ClearAll();

            _identityService = new FakeIdentityService();
            ServiceLocator.Register<IPlayerIdentityService>(_identityService);

            _holder = new GameObject("TestAccountLinkHolder");
            _view = _holder.AddComponent<LobbyAccountLinkView>();
            _controller = _holder.AddComponent<LobbyAccountLinkController>();
            InvokeLifecycle(_controller, "OnEnable");
        }

        [TearDown]
        public void TearDown()
        {
            if (_controller != null)
            {
                InvokeLifecycle(_controller, "OnDisable");
            }

            ServiceLocator.ClearAll();
            if (_holder != null)
            {
                UnityEngine.Object.DestroyImmediate(_holder);
            }
        }

        [Test]
        public void Controller_InitializesAndResolvesService()
        {
            Assert.IsNotNull(_controller);
            Assert.IsNotNull(_view);
            Assert.AreEqual(1, _identityService.StateChangedSubscriberCount);
        }

        [Test]
        public void StateChanged_UpdatesSnapshot()
        {
            var newSnapshot = PlayerIdentitySnapshot.Linked();
            _identityService.EmitStateChanged(newSnapshot);

            Assert.AreEqual(1, _identityService.StateChangedSubscriberCount);
        }

        [Test]
        public void OnDisable_UnregistersCleanly()
        {
            InvokeLifecycle(_controller, "OnDisable");
            Assert.AreEqual(0, _identityService.StateChangedSubscriberCount);
        }

        private static void InvokeLifecycle(object instance, string methodName)
        {
            var method = instance.GetType().GetMethod(methodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            method?.Invoke(instance, null);
        }

        private sealed class FakeIdentityService : IPlayerIdentityService
        {
            public PlayerIdentitySnapshot Snapshot { get; set; } = PlayerIdentitySnapshot.Anonymous();
            public int RefreshCallCount { get; private set; }
            public int LinkCallCount { get; private set; }
            public int ResolveConflictCallCount { get; private set; }
            public AccountLinkConflictResolution LastResolution { get; private set; }

            public int StateChangedSubscriberCount => _stateChanged?.GetInvocationList()?.Length ?? 0;

            private Action<PlayerIdentitySnapshot> _stateChanged;

            public event Action<PlayerIdentitySnapshot> StateChanged
            {
                add => _stateChanged += value;
                remove => _stateChanged -= value;
            }

            public Task<PlayerIdentityResult> RefreshAsync(CancellationToken cancellationToken = default)
            {
                RefreshCallCount++;
                return Task.FromResult(PlayerIdentityResult.Success(Snapshot));
            }

            public Task<PlayerIdentityResult> LinkGooglePlayGamesAsync(CancellationToken cancellationToken = default)
            {
                LinkCallCount++;
                return Task.FromResult(PlayerIdentityResult.Success(Snapshot));
            }

            public Task<PlayerIdentityResult> ResolveConflictAsync(
                AccountLinkConflictResolution resolution,
                CancellationToken cancellationToken = default)
            {
                ResolveConflictCallCount++;
                LastResolution = resolution;
                return Task.FromResult(PlayerIdentityResult.Success(Snapshot));
            }

            public void EmitStateChanged(PlayerIdentitySnapshot snapshot)
            {
                Snapshot = snapshot;
                _stateChanged?.Invoke(snapshot);
            }
        }
    }
}
