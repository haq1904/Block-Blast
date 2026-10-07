using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Null-object implementation of ILoadingScreen enabling transitions without visual presentation.
/// </summary>
public sealed class NullLoadingScreen : ILoadingScreen
{
    public static readonly NullLoadingScreen Instance = new NullLoadingScreen();

    public void Prepare(SceneTransitionContext context) { }

    public Task ShowAsync(SceneTransitionContext context, CancellationToken cancellationToken) => Task.CompletedTask;

    public void SetProgress(float normalizedProgress) { }

    public Task HideAsync(SceneTransitionContext context, CancellationToken cancellationToken) => Task.CompletedTask;

    public void ResetImmediate() { }
}
