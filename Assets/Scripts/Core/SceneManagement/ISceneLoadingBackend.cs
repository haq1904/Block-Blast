using System.Threading;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

/// <summary>
/// Abstraction representing an active asynchronous scene loading operation.
/// </summary>
public interface ISceneLoadOperation
{
    /// <summary>
    /// Raw loading progress from the underlying engine (0 to 1, or 0 to 0.9 before activation).
    /// </summary>
    float Progress { get; }

    /// <summary>
    /// True when the scene load and activation have fully finished.
    /// </summary>
    bool IsDone { get; }

    /// <summary>
    /// Governs whether the scene is allowed to activate once loaded into memory.
    /// </summary>
    bool AllowSceneActivation { get; set; }
}

/// <summary>
/// Abstraction representing an active asynchronous scene unloading operation.
/// </summary>
public interface ISceneUnloadOperation
{
    /// <summary>
    /// Raw unloading progress from the underlying engine (0 to 1).
    /// </summary>
    float Progress { get; }

    /// <summary>
    /// True when the scene unload has fully finished.
    /// </summary>
    bool IsDone { get; }
}

/// <summary>
/// Abstraction around engine-level scene querying and loading APIs.
/// </summary>
public interface ISceneLoadingBackend
{
    /// <summary>
    /// Build index of the currently active scene.
    /// </summary>
    int ActiveSceneBuildIndex { get; }

    /// <summary>
    /// Name of the currently active scene.
    /// </summary>
    string ActiveSceneName { get; }

    /// <summary>
    /// Checks whether the target build index is valid and present in Build Settings.
    /// </summary>
    bool IsBuildIndexValid(int buildIndex);

    /// <summary>
    /// Retrieves the scene name associated with the given build index.
    /// </summary>
    string GetSceneNameByBuildIndex(int buildIndex);

    /// <summary>
    /// Initiates an asynchronous scene load operation.
    /// In Single mode, Unity activates the target scene as active.
    /// In Additive mode, the target scene is loaded alongside current scenes without automatically changing the active scene.
    /// </summary>
    ISceneLoadOperation LoadSceneAsync(int buildIndex, LoadSceneMode mode);

    /// <summary>
    /// Checks whether a scene at the given build index is currently loaded in memory.
    /// </summary>
    bool IsSceneLoaded(int buildIndex);

    /// <summary>
    /// Initiates an asynchronous scene unload operation by build index.
    /// </summary>
    ISceneUnloadOperation UnloadSceneAsync(int buildIndex);

    /// <summary>
    /// Attempts to set the active scene by build index.
    /// Returns true if successful, false if the scene is invalid or not loaded.
    /// </summary>
    bool TrySetActiveScene(int buildIndex);
}

/// <summary>
/// Injectable timing and async delay abstraction for testing without wall-clock waits.
/// </summary>
public interface ISceneTimeProvider
{
    /// <summary>
    /// Realtime elapsed since application or test start.
    /// </summary>
    float RealtimeSinceStartup { get; }

    /// <summary>
    /// Asynchronously delays execution for the given realtime duration.
    /// </summary>
    Task DelayAsync(float seconds, CancellationToken cancellationToken);

    /// <summary>
    /// Yields execution to the next frame or dispatch cycle.
    /// </summary>
    Task YieldAsync(CancellationToken cancellationToken);
}
