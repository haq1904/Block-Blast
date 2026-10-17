using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Production backend adapter communicating directly with Unity's SceneManager.
/// </summary>
public sealed class UnitySceneLoadingBackend : ISceneLoadingBackend, ISceneTimeProvider
{
    public int ActiveSceneBuildIndex => SceneManager.GetActiveScene().buildIndex;
    public string ActiveSceneName => SceneManager.GetActiveScene().name;

    public bool IsBuildIndexValid(int buildIndex) =>
        buildIndex >= 0 && buildIndex < SceneManager.sceneCountInBuildSettings;

    public string GetSceneNameByBuildIndex(int buildIndex)
    {
        if (!IsBuildIndexValid(buildIndex)) return string.Empty;
        string scenePath = SceneUtility.GetScenePathByBuildIndex(buildIndex);
        return Path.GetFileNameWithoutExtension(scenePath);
    }

    public ISceneLoadOperation LoadSceneAsync(int buildIndex, LoadSceneMode mode)
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(buildIndex, mode);
        return op != null ? new UnitySceneLoadOperation(op) : null;
    }

    public bool IsSceneLoaded(int buildIndex)
    {
        if (!IsBuildIndexValid(buildIndex)) return false;
        Scene scene = SceneManager.GetSceneByBuildIndex(buildIndex);
        return scene.IsValid() && scene.isLoaded;
    }

    public ISceneUnloadOperation UnloadSceneAsync(int buildIndex)
    {
        if (!IsBuildIndexValid(buildIndex)) return null;
        Scene scene = SceneManager.GetSceneByBuildIndex(buildIndex);
        if (!scene.IsValid() || !scene.isLoaded) return null;
        AsyncOperation op = SceneManager.UnloadSceneAsync(scene);
        return op != null ? new UnitySceneUnloadOperation(op) : null;
    }

    public bool TrySetActiveScene(int buildIndex)
    {
        if (!IsBuildIndexValid(buildIndex)) return false;
        Scene scene = SceneManager.GetSceneByBuildIndex(buildIndex);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogWarning($"[UnitySceneLoadingBackend] Scene with build index {buildIndex} is not loaded or invalid.");
            return false;
        }

        if (SceneManager.GetActiveScene() == scene)
        {
            return true;
        }

        return SceneManager.SetActiveScene(scene);
    }

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

    private sealed class UnitySceneLoadOperation : ISceneLoadOperation
    {
        private readonly AsyncOperation _operation;

        public UnitySceneLoadOperation(AsyncOperation operation)
        {
            _operation = operation;
        }

        public float Progress => _operation.progress;
        public bool IsDone => _operation.isDone;
        public bool AllowSceneActivation
        {
            get => _operation.allowSceneActivation;
            set => _operation.allowSceneActivation = value;
        }
    }

    private sealed class UnitySceneUnloadOperation : ISceneUnloadOperation
    {
        private readonly AsyncOperation _operation;

        public UnitySceneUnloadOperation(AsyncOperation operation)
        {
            _operation = operation;
        }

        public float Progress => _operation.progress;
        public bool IsDone => _operation.isDone;
    }
}
