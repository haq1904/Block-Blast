using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Service interface for decoupled, asynchronous scene transitions.
/// </summary>
public interface ISceneService
{
    /// <summary>
    /// True while an asynchronous scene transition is currently running.
    /// </summary>
    bool IsTransitioning { get; }

    /// <summary>
    /// Loads the requested scene asynchronously through the transition pipeline.
    /// Throws InvalidOperationException if a transition is already in progress.
    /// </summary>
    Task LoadSceneAsync(SceneLoadRequest request, CancellationToken cancellationToken = default);
}
