using System;
using UnityEngine.SceneManagement;

/// <summary>
/// Immutable specification for a scene loading request.
/// </summary>
public readonly struct SceneLoadRequest : IEquatable<SceneLoadRequest>
{
    /// <summary>
    /// Build index of the target scene in Unity Build Settings.
    /// </summary>
    public int SceneBuildIndex { get; }

    /// <summary>
    /// Loading mode (Single or Additive).
    /// In Single mode, Unity automatically makes the newly loaded scene the active scene upon activation.
    /// In Additive mode, the target scene is loaded alongside existing scenes, but the active scene is not automatically changed.
    /// </summary>
    public LoadSceneMode LoadMode { get; }

    /// <summary>
    /// Minimum time in realtime seconds the transition overlay remains visible.
    /// </summary>
    public float MinimumVisibleDuration { get; }

    public SceneLoadRequest(int sceneBuildIndex, LoadSceneMode loadMode = LoadSceneMode.Single, float minimumVisibleDuration = 0f)
    {
        if (sceneBuildIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sceneBuildIndex), "Scene build index must be non-negative.");
        }

        if (float.IsNaN(minimumVisibleDuration) || float.IsInfinity(minimumVisibleDuration) || minimumVisibleDuration < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumVisibleDuration), "Minimum visible duration must be a non-negative finite number.");
        }

        SceneBuildIndex = sceneBuildIndex;
        LoadMode = loadMode;
        MinimumVisibleDuration = minimumVisibleDuration;
    }

    public bool Equals(SceneLoadRequest other) =>
        SceneBuildIndex == other.SceneBuildIndex &&
        LoadMode == other.LoadMode &&
        MinimumVisibleDuration == other.MinimumVisibleDuration;

    public override bool Equals(object obj) => obj is SceneLoadRequest other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(SceneBuildIndex, LoadMode, MinimumVisibleDuration);

    public override string ToString() =>
        $"SceneLoadRequest(Index: {SceneBuildIndex}, Mode: {LoadMode}, MinDuration: {MinimumVisibleDuration}s)";
}
