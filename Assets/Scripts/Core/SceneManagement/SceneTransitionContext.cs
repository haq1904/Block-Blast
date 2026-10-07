using UnityEngine.SceneManagement;

/// <summary>
/// Read-only snapshot of scene transition metadata delivered to loading screens.
/// </summary>
public readonly struct SceneTransitionContext
{
    public int SourceSceneBuildIndex { get; }
    public string SourceSceneName { get; }
    public int TargetSceneBuildIndex { get; }
    public string TargetSceneName { get; }
    public LoadSceneMode LoadMode { get; }
    public bool IsReload { get; }

    public SceneTransitionContext(
        int sourceSceneBuildIndex,
        string sourceSceneName,
        int targetSceneBuildIndex,
        string targetSceneName,
        LoadSceneMode loadMode,
        bool isReload)
    {
        SourceSceneBuildIndex = sourceSceneBuildIndex;
        SourceSceneName = sourceSceneName ?? string.Empty;
        TargetSceneBuildIndex = targetSceneBuildIndex;
        TargetSceneName = targetSceneName ?? string.Empty;
        LoadMode = loadMode;
        IsReload = isReload;
    }

    public override string ToString() =>
        $"SceneTransitionContext({SourceSceneName}[{SourceSceneBuildIndex}] -> {TargetSceneName}[{TargetSceneBuildIndex}], Mode: {LoadMode}, Reload: {IsReload})";
}
