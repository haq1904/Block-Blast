using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Presentation contract for transition loading screens.
/// </summary>
public interface ILoadingScreen
{
    /// <summary>
    /// Resets presentation state and prepares visuals for the impending transition.
    /// </summary>
    void Prepare(SceneTransitionContext context);

    /// <summary>
    /// Animates the loading screen to fully cover the view.
    /// Completes only when the previous scene can be safely unloaded out of sight.
    /// </summary>
    Task ShowAsync(SceneTransitionContext context, CancellationToken cancellationToken);

    /// <summary>
    /// Updates normalized loading progress in [0, 1].
    /// </summary>
    void SetProgress(float normalizedProgress);

    /// <summary>
    /// Animates the loading screen to reveal the newly activated target scene.
    /// </summary>
    Task HideAsync(SceneTransitionContext context, CancellationToken cancellationToken);

    /// <summary>
    /// Idempotent, immediate reset of all visual elements to their canonical hidden state.
    /// Called during failure, cancellation, or destruction.
    /// </summary>
    void ResetImmediate();
}
