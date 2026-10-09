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
    /// In Single mode, the target scene becomes the active scene upon activation.
    /// In Additive mode, the target scene is loaded alongside existing scenes without automatically changing the active scene.
    /// Caller cancellation is honored until the engine operation begins; once started, the transition completes activation safely.
    /// </summary>
    Task LoadSceneAsync(SceneLoadRequest request, CancellationToken cancellationToken = default);
}
