using System.Threading;
using System.Threading.Tasks;

namespace BlockBlast.Features.GameFlow.Audio
{
    /// <summary>
    /// Feature-level music orchestration contract mapping semantic states (Lobby, Playing)
    /// to game audio playback transitions.
    /// </summary>
    public interface IGameMusicService
    {
        void PlayLobby();
        Task TransitionToPlayingAsync(CancellationToken cancellationToken = default);
        void RestoreLobbyImmediate();
    }
}
