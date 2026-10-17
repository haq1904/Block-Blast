using System.Threading;
using System.Threading.Tasks;

namespace BlockBlast.Features.GameFlow.Camera
{
    /// <summary>
    /// Service contract for animating and positioning the canonical World camera between Lobby and Play poses.
    /// </summary>
    public interface IWorldCameraTransitionService
    {
        /// <summary>
        /// True while the camera is actively animating between poses.
        /// </summary>
        bool IsMoving { get; }

        /// <summary>
        /// Smoothly tweens the World camera from its current pose to the Play pose.
        /// </summary>
        Task MoveToPlayAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Smoothly tweens the World camera from its current pose back to the Lobby pose.
        /// </summary>
        Task MoveToLobbyAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Instantly snaps the World camera to the Lobby pose without animation.
        /// </summary>
        void SnapToLobby();
    }
}
