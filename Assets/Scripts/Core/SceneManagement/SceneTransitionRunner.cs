using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Pure C# orchestrator coordinating the scene cover, load, progress, activation, and reveal lifecycle.
/// </summary>
public sealed class SceneTransitionRunner
{
    private readonly ISceneLoadingBackend _backend;
    private readonly ILoadingScreen _loadingScreen;
    private readonly ISceneTimeProvider _timeProvider;

    /// <summary>
    /// True while an asynchronous scene transition is currently running.
    /// </summary>
    public bool IsTransitioning { get; private set; }

    public SceneTransitionRunner(
        ISceneLoadingBackend backend,
        ILoadingScreen loadingScreen,
        ISceneTimeProvider timeProvider = null)
    {
        _backend = backend ?? throw new ArgumentNullException(nameof(backend));
        _loadingScreen = loadingScreen ?? NullLoadingScreen.Instance;
        _timeProvider = timeProvider ?? (backend as ISceneTimeProvider) ?? new DefaultSceneTimeProvider();
    }

    /// <summary>
    /// Executes the full transition sequence.
    /// </summary>
    public async Task RunAsync(SceneLoadRequest request, CancellationToken cancellationToken = default)
    {
        if (IsTransitioning)
        {
            throw new InvalidOperationException("A scene transition is already in progress.");
        }

        // 1. Validate target build index
        if (!_backend.IsBuildIndexValid(request.SceneBuildIndex))
        {
            throw new ArgumentException(
                $"Target scene build index {request.SceneBuildIndex} is not within Build Settings range.",
                nameof(request));
        }

        IsTransitioning = true;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            // 2. Build transition context
            int sourceIndex = _backend.ActiveSceneBuildIndex;
            string sourceName = _backend.ActiveSceneName;
            int targetIndex = request.SceneBuildIndex;
            string targetName = _backend.GetSceneNameByBuildIndex(targetIndex);
            bool isReload = sourceIndex == targetIndex;

            var context = new SceneTransitionContext(
                sourceIndex,
                sourceName,
                targetIndex,
                targetName,
                request.LoadMode,
                isReload);

            // 3. Prepare loading screen
            _loadingScreen.Prepare(context);

            // 4. Show loading screen to fully cover the view
            await _loadingScreen.ShowAsync(context, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            // 5. Begin asynchronous scene loading only after cover is complete
            float startTime = _timeProvider.RealtimeSinceStartup;
            ISceneLoadOperation operation = _backend.LoadSceneAsync(request.SceneBuildIndex, request.LoadMode);
            if (operation == null)
            {
                throw new InvalidOperationException(
                    $"Failed to start loading scene with build index {request.SceneBuildIndex}. Backend returned null operation.");
            }

            // 6. Prevent premature scene activation
            operation.AllowSceneActivation = false;

            // 7. Poll progress until ready (Unity raw progress reaches 0.9f)
            float rawProgress = operation.Progress;
            while (rawProgress < 0.9f)
            {
                float normalized = Mathf.Clamp01(rawProgress / 0.9f);
                _loadingScreen.SetProgress(normalized);
                await _timeProvider.YieldAsync(CancellationToken.None);
                rawProgress = operation.Progress;
            }

            // 8. Ensure minimum visible duration is met using realtime clock
            float elapsed = _timeProvider.RealtimeSinceStartup - startTime;
            float remaining = request.MinimumVisibleDuration - elapsed;
            if (remaining > 0f)
            {
                await _timeProvider.DelayAsync(remaining, CancellationToken.None);
            }

            // 9. Send completion progress before activation
            _loadingScreen.SetProgress(1f);

            // 10. Enable activation and wait for operation to finish
            operation.AllowSceneActivation = true;
            while (!operation.IsDone)
            {
                await _timeProvider.YieldAsync(CancellationToken.None);
            }

            // 11. Reveal newly activated target scene
            await _loadingScreen.HideAsync(context, cancellationToken);
        }
        catch
        {
            _loadingScreen.ResetImmediate();
            throw;
        }
        finally
        {
            IsTransitioning = false;
        }
    }

    private sealed class DefaultSceneTimeProvider : ISceneTimeProvider
    {
        public float RealtimeSinceStartup => Time.realtimeSinceStartup;

        public async Task DelayAsync(float seconds, CancellationToken cancellationToken)
        {
            if (seconds <= 0f) return;
            await Task.Delay(TimeSpan.FromSeconds(seconds), cancellationToken);
        }

        public async Task YieldAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();
        }
    }
}
