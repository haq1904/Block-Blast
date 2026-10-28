using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    /// Caller cancellation is honored up until the engine load operation begins.
    /// Once the engine operation is underway, the pipeline will complete scene activation
    /// and reveal without cancellation to prevent abandoned background operations.
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
        ISceneLoadOperation operation = null;
        ILoadingScreen activePresenter = request.Presentation == SceneTransitionPresentation.None
            ? NullLoadingScreen.Instance
            : _loadingScreen;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            // 2. Build transition context
            int sourceIndex = _backend.ActiveSceneBuildIndex;
            string sourceName = _backend.ActiveSceneName;
            int targetIndex = request.SceneBuildIndex;
            string targetName = _backend.GetSceneNameByBuildIndex(targetIndex);

            var context = new SceneTransitionContext(
                sourceIndex,
                sourceName,
                targetIndex,
                targetName,
                request.LoadMode);

            // 3. Prepare loading screen
            activePresenter.Prepare(context);

            // 4. Show loading screen to fully cover the view
            await activePresenter.ShowAsync(context, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            // 5. Begin asynchronous scene loading only after cover is complete
            float startTime = _timeProvider.RealtimeSinceStartup;
            operation = _backend.LoadSceneAsync(request.SceneBuildIndex, request.LoadMode);
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
                activePresenter.SetProgress(normalized);
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
            activePresenter.SetProgress(1f);

            // 10. Enable activation and wait for operation to finish
            operation.AllowSceneActivation = true;
            while (!operation.IsDone)
            {
                await _timeProvider.YieldAsync(CancellationToken.None);
            }

            // 11. Reveal newly activated target scene
            await activePresenter.HideAsync(context, CancellationToken.None);
        }
        catch
        {
            TryReleaseActivation(operation);
            activePresenter.ResetImmediate();
            throw;
        }
        finally
        {
            IsTransitioning = false;
        }
    }

    /// <summary>
    /// Executes an additive scene transition batch.
    /// Ensures target scenes are loaded, sets the active scene at commit point, and unloads requested source scenes.
    /// Rolls back only newly loaded scenes if failure occurs before commit.
    /// </summary>
    public async Task RunAdditiveAsync(AdditiveSceneTransitionRequest request, CancellationToken cancellationToken = default)
    {
        if (IsTransitioning)
        {
            throw new InvalidOperationException("A scene transition is already in progress.");
        }

        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        // 1. Validate build indexes with backend
        if (!_backend.IsBuildIndexValid(request.TargetSceneBuildIndex))
        {
            throw new ArgumentException(
                $"Target scene build index {request.TargetSceneBuildIndex} is not within Build Settings range.",
                nameof(request));
        }

        if (!_backend.IsBuildIndexValid(request.ActiveSceneBuildIndex))
        {
            throw new ArgumentException(
                $"Active scene build index {request.ActiveSceneBuildIndex} is not within Build Settings range.",
                nameof(request));
        }

        foreach (int idx in request.LoadBuildIndexes)
        {
            if (!_backend.IsBuildIndexValid(idx))
            {
                throw new ArgumentException(
                    $"Load scene build index {idx} is not within Build Settings range.",
                    nameof(request));
            }
        }

        foreach (int idx in request.UnloadBuildIndexes)
        {
            if (!_backend.IsBuildIndexValid(idx))
            {
                throw new ArgumentException(
                    $"Unload scene build index {idx} is not within Build Settings range.",
                    nameof(request));
            }
        }

        bool isActiveLoaded = _backend.IsSceneLoaded(request.ActiveSceneBuildIndex);
        bool isActiveInLoadList = false;
        for (int i = 0; i < request.LoadBuildIndexes.Count; i++)
        {
            if (request.LoadBuildIndexes[i] == request.ActiveSceneBuildIndex)
            {
                isActiveInLoadList = true;
                break;
            }
        }

        if (!isActiveLoaded && !isActiveInLoadList)
        {
            throw new ArgumentException(
                $"Active scene build index {request.ActiveSceneBuildIndex} must be already loaded or in the load list.",
                nameof(request));
        }

        int previousActiveScene = _backend.ActiveSceneBuildIndex;

        // 2. Resolve concrete lists to load and unload
        var scenesToLoad = new List<int>();
        for (int i = 0; i < request.LoadBuildIndexes.Count; i++)
        {
            int idx = request.LoadBuildIndexes[i];
            if (!_backend.IsSceneLoaded(idx))
            {
                scenesToLoad.Add(idx);
            }
        }

        var scenesToUnload = new List<int>();
        for (int i = 0; i < request.UnloadBuildIndexes.Count; i++)
        {
            int idx = request.UnloadBuildIndexes[i];
            if (_backend.IsSceneLoaded(idx))
            {
                scenesToUnload.Add(idx);
            }
        }

        // No-op check: if nothing needs to load, unload, or change active scene, exit immediately without showing curtain
        if (scenesToLoad.Count == 0 && scenesToUnload.Count == 0 && _backend.ActiveSceneBuildIndex == request.ActiveSceneBuildIndex)
        {
            return;
        }

        IsTransitioning = true;
        bool isCommitted = false;
        var operations = new List<(int buildIndex, ISceneLoadOperation op)>(scenesToLoad.Count);
        var scenesLoadedByThisRequest = new List<int>();

        ILoadingScreen activePresenter = request.Presentation == SceneTransitionPresentation.None
            ? NullLoadingScreen.Instance
            : _loadingScreen;

        SceneTransitionContext context = default;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            int sourceIndex = previousActiveScene;
            string sourceName = _backend.ActiveSceneName;
            int targetIndex = request.TargetSceneBuildIndex;
            string targetName = _backend.GetSceneNameByBuildIndex(targetIndex);

            context = new SceneTransitionContext(
                sourceIndex,
                sourceName,
                targetIndex,
                targetName,
                LoadSceneMode.Additive);

            activePresenter.Prepare(context);
            await activePresenter.ShowAsync(context, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            float startTime = _timeProvider.RealtimeSinceStartup;
            int totalLoadCount = scenesToLoad.Count;
            float maxReportedProgress = 0f;

            // Sequentially load each missing scene in request order
            for (int i = 0; i < totalLoadCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                int buildIndex = scenesToLoad[i];
                var op = _backend.LoadSceneAsync(buildIndex, LoadSceneMode.Additive);
                if (op == null)
                {
                    throw new InvalidOperationException(
                        $"Failed to start loading scene with build index {buildIndex}. Backend returned null operation.");
                }

                operations.Add((buildIndex, op));
                op.AllowSceneActivation = false;

                // Poll current op progress to 0.9f
                while (op.Progress < 0.9f)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    float currentNorm = Mathf.Clamp01(op.Progress / 0.9f);
                    float aggregate = (i + currentNorm) / totalLoadCount;
                    if (aggregate > maxReportedProgress)
                    {
                        maxReportedProgress = aggregate;
                        activePresenter.SetProgress(maxReportedProgress);
                    }
                    await _timeProvider.YieldAsync(CancellationToken.None);
                }

                // Release activation immediately for this operation
                op.AllowSceneActivation = true;

                // Wait for operation to complete
                while (!op.IsDone)
                {
                    await _timeProvider.YieldAsync(CancellationToken.None);
                    cancellationToken.ThrowIfCancellationRequested();
                }

                cancellationToken.ThrowIfCancellationRequested();

                if (!_backend.IsSceneLoaded(buildIndex))
                {
                    throw new InvalidOperationException($"Scene with build index {buildIndex} completed operation but is not loaded.");
                }

                scenesLoadedByThisRequest.Add(buildIndex);
                float stepProgress = (float)(i + 1) / totalLoadCount;
                if (stepProgress > maxReportedProgress)
                {
                    maxReportedProgress = stepProgress;
                    activePresenter.SetProgress(maxReportedProgress);
                }
            }

            // Ensure minimum visible duration
            float elapsed = _timeProvider.RealtimeSinceStartup - startTime;
            float remaining = request.MinimumVisibleDuration - elapsed;
            if (remaining > 0f)
            {
                await _timeProvider.DelayAsync(remaining, cancellationToken);
            }

            activePresenter.SetProgress(1f);
            cancellationToken.ThrowIfCancellationRequested();

            // Commit point: set active scene
            bool activeSetOk = _backend.TrySetActiveScene(request.ActiveSceneBuildIndex);
            if (!activeSetOk)
            {
                throw new InvalidOperationException(
                    $"Failed to set active scene to build index {request.ActiveSceneBuildIndex}.");
            }

            isCommitted = true;

            // Unload source scenes only after commit point
            List<int> failedUnloads = null;
            for (int i = 0; i < scenesToUnload.Count; i++)
            {
                int unloadIndex = scenesToUnload[i];
                if (_backend.IsSceneLoaded(unloadIndex))
                {
                    var unloadOp = _backend.UnloadSceneAsync(unloadIndex);
                    if (unloadOp == null)
                    {
                        if (failedUnloads == null) failedUnloads = new List<int>();
                        failedUnloads.Add(unloadIndex);
                        continue;
                    }

                    while (!unloadOp.IsDone)
                    {
                        await _timeProvider.YieldAsync(CancellationToken.None);
                    }
                }
            }

            await activePresenter.HideAsync(context, CancellationToken.None);

            if (failedUnloads != null && failedUnloads.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Failed to unload scenes after commit. Unload operation was null for build index(es): {string.Join(", ", failedUnloads)}.");
            }
        }
        catch (Exception transitionError)
        {
            if (!isCommitted)
            {
                var cleanupFailures = new List<Exception>();
                for (int i = 0; i < operations.Count; i++)
                {
                    TryReleaseActivation(operations[i].op);
                }

                for (int i = 0; i < operations.Count; i++)
                {
                    while (!operations[i].op.IsDone)
                    {
                        try
                        {
                            await _timeProvider.YieldAsync(CancellationToken.None);
                        }
                        catch
                        {
                            cleanupFailures.Add(new InvalidOperationException(
                                $"Rollback could not wait for scene {operations[i].buildIndex} to finish loading."));
                            break;
                        }
                    }

                    if (_backend.IsSceneLoaded(operations[i].buildIndex) && !scenesLoadedByThisRequest.Contains(operations[i].buildIndex))
                    {
                        scenesLoadedByThisRequest.Add(operations[i].buildIndex);
                    }
                }

                for (int i = scenesLoadedByThisRequest.Count - 1; i >= 0; i--)
                {
                    int loadedIndex = scenesLoadedByThisRequest[i];
                    try
                    {
                        if (_backend.IsSceneLoaded(loadedIndex))
                        {
                            var unloadOp = _backend.UnloadSceneAsync(loadedIndex);
                            if (unloadOp != null)
                            {
                                while (!unloadOp.IsDone)
                                {
                                    await _timeProvider.YieldAsync(CancellationToken.None);
                                }
                            }
                            else
                            {
                                cleanupFailures.Add(new InvalidOperationException(
                                    $"Rollback unload returned no operation for scene {loadedIndex}."));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        cleanupFailures.Add(new InvalidOperationException(
                            $"Rollback failed to unload scene {loadedIndex}.", ex));
                    }
                }

                if (_backend.IsSceneLoaded(previousActiveScene))
                {
                    try
                    {
                        _backend.TrySetActiveScene(previousActiveScene);
                    }
                    catch
                    {
                        // Best-effort active restoration
                    }
                }

                activePresenter.ResetImmediate();
                if (cleanupFailures.Count > 0)
                {
                    cleanupFailures.Insert(0, transitionError);
                    throw new AggregateException("Pre-commit scene rollback was incomplete.", cleanupFailures);
                }
            }
            else
            {
                activePresenter.ResetImmediate();
            }

            throw;
        }
        finally
        {
            IsTransitioning = false;
        }
    }

    /// <summary>
    /// Executes an operation fully covered by the loading screen.
    /// Follows: Prepare -> ShowAsync -> coveredOperation -> HideAsync.
    /// Exclusivity is guarded via IsTransitioning.
    /// On error or cancellation, resets the loading screen immediately.
    /// </summary>
    public async Task RunCoveredOperationAsync(
        Func<CancellationToken, Task> coveredOperation,
        CancellationToken cancellationToken = default)
    {
        if (IsTransitioning)
        {
            throw new InvalidOperationException("A scene transition or covered operation is already in progress.");
        }

        IsTransitioning = true;
        ILoadingScreen activePresenter = _loadingScreen ?? NullLoadingScreen.Instance;
        int activeIndex = _backend.ActiveSceneBuildIndex;
        string activeName = _backend.ActiveSceneName;
        var context = new SceneTransitionContext(activeIndex, activeName, activeIndex, activeName, LoadSceneMode.Single);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            activePresenter.Prepare(context);
            await activePresenter.ShowAsync(context, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            if (coveredOperation != null)
            {
                await coveredOperation(cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();
            await activePresenter.HideAsync(context, CancellationToken.None);
        }
        catch
        {
            activePresenter.ResetImmediate();
            throw;
        }
        finally
        {
            IsTransitioning = false;
        }
    }

    private static void TryReleaseActivation(ISceneLoadOperation operation)
    {
        if (operation == null) return;
        try
        {
            if (!operation.IsDone)
            {
                operation.AllowSceneActivation = true;
            }
        }
        catch
        {
            // Best-effort release to prevent underlying scene operation from staying locked
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
