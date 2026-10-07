using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Abstract base MonoBehaviour for game-specific loading screen components.
/// Subclasses in Features can utilize DOTween or UI toolkits.
/// </summary>
public abstract class BaseLoadingScreen : MonoBehaviour, ILoadingScreen
{
    public abstract void Prepare(SceneTransitionContext context);
    public abstract Task ShowAsync(SceneTransitionContext context, CancellationToken cancellationToken);
    public abstract void SetProgress(float normalizedProgress);
    public abstract Task HideAsync(SceneTransitionContext context, CancellationToken cancellationToken);
    public abstract void ResetImmediate();
}
