using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace BlockBlast.Features.GameFlow.Audio
{
    /// <summary>
    /// Feature director managing semantic background music transitions between Lobby and Playing states.
    /// Acts as the bridge between game flow orchestration and the generic MusicManager engine.
    /// </summary>
    public sealed class GameMusicDirector : MonoBehaviour, IGameMusicService
    {
        [Header("Engine Reference")]
        [SerializeField] private MusicManager _musicManager;

        [Header("Audio Clips")]
        [SerializeField] private AudioClip _lobbyBgm;
        [SerializeField] private AudioClip _playingBgm;

        [Header("Transition Durations (Seconds)")]
        [SerializeField] private float _fadeOutSeconds = 0.65f;
        [SerializeField] private float _fadeInSeconds = 0.85f;

        private bool _ownsRegistration;

        public float FadeOutSeconds => Mathf.Max(0f, _fadeOutSeconds);
        public float FadeInSeconds => Mathf.Max(0f, _fadeInSeconds);
        public AudioClip LobbyBgm => _lobbyBgm;
        public AudioClip PlayingBgm => _playingBgm;
        public bool OwnsRegistration => _ownsRegistration;

        private void Awake()
        {
            if (_musicManager == null)
            {
                _musicManager = GetComponent<MusicManager>();
            }

            if (ServiceLocator.TryGet<IGameMusicService>(out _))
            {
                Debug.LogWarning("[GameMusicDirector] Another IGameMusicService is already registered. Disabling duplicate host.", this);
                enabled = false;
                return;
            }

            ServiceLocator.Register<IGameMusicService>(this);
            _ownsRegistration = true;
        }

        public void PlayLobby()
        {
            if (_musicManager != null && _lobbyBgm != null)
            {
                _musicManager.PlayImmediate(_lobbyBgm);
            }
        }

        public Task TransitionToPlayingAsync(CancellationToken cancellationToken = default)
        {
            if (_musicManager == null || _playingBgm == null)
            {
                return Task.CompletedTask;
            }

            return _musicManager.TransitionToAsync(_playingBgm, FadeOutSeconds, FadeInSeconds, cancellationToken);
        }

        public void RestoreLobbyImmediate()
        {
            if (_musicManager != null && _lobbyBgm != null)
            {
                _musicManager.PlayImmediate(_lobbyBgm);
            }
        }

        private void OnDestroy()
        {
            if (_ownsRegistration)
            {
                ServiceLocator.Unregister<IGameMusicService>(this);
                _ownsRegistration = false;
            }
        }
    }
}
