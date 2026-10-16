using System.Threading;
using System.Threading.Tasks;

namespace BlockBlast.Features.GameFlow.Navigation
{
    /// <summary>
    /// Game-specific navigation interface abstracting route destinations and presentation modes.
    /// </summary>
    public interface IGameSceneNavigator
    {
        /// <summary>
        /// True while an asynchronous scene transition is currently running.
        /// </summary>
        bool IsTransitioning { get; }

        /// <summary>
        /// Loads the specified logical game route asynchronously.
        /// </summary>
        Task LoadAsync(
            GameSceneRoute route,
            SceneTransitionPresentation presentation,
            float minimumVisibleDuration = 0f,
            CancellationToken cancellationToken = default);
    }
}
