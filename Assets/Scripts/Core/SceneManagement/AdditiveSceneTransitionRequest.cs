using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Immutable specification for an additive scene transition batch request.
/// </summary>
public sealed class AdditiveSceneTransitionRequest : IEquatable<AdditiveSceneTransitionRequest>
{
    /// <summary>
    /// Build index of the target content scene for transition context and telemetry.
    /// </summary>
    public int TargetSceneBuildIndex { get; }

    /// <summary>
    /// Ordered list of scene build indexes to ensure loaded additively.
    /// </summary>
    public IReadOnlyList<int> LoadBuildIndexes { get; }

    /// <summary>
    /// Ordered list of scene build indexes to unload after commit point.
    /// </summary>
    public IReadOnlyList<int> UnloadBuildIndexes { get; }

    /// <summary>
    /// Scene build index to set as active before unloading source scenes.
    /// </summary>
    public int ActiveSceneBuildIndex { get; }

    /// <summary>
    /// Minimum time in realtime seconds the transition overlay remains visible.
    /// </summary>
    public float MinimumVisibleDuration { get; }

    /// <summary>
    /// Visual presentation mode to use during the transition.
    /// </summary>
    public SceneTransitionPresentation Presentation { get; }

    public AdditiveSceneTransitionRequest(
        int targetSceneBuildIndex,
        IEnumerable<int> loadBuildIndexes,
        IEnumerable<int> unloadBuildIndexes,
        int activeSceneBuildIndex,
        float minimumVisibleDuration = 0f,
        SceneTransitionPresentation presentation = SceneTransitionPresentation.LoadingScreen)
    {
        if (targetSceneBuildIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetSceneBuildIndex), "Target scene build index must be non-negative.");
        }

        if (activeSceneBuildIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(activeSceneBuildIndex), "Active scene build index must be non-negative.");
        }

        if (loadBuildIndexes == null)
        {
            throw new ArgumentNullException(nameof(loadBuildIndexes), "Load build indexes cannot be null.");
        }

        if (unloadBuildIndexes == null)
        {
            throw new ArgumentNullException(nameof(unloadBuildIndexes), "Unload build indexes cannot be null.");
        }

        if (float.IsNaN(minimumVisibleDuration) || float.IsInfinity(minimumVisibleDuration) || minimumVisibleDuration < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumVisibleDuration), "Minimum visible duration must be a non-negative finite number.");
        }

        var loadList = new List<int>();
        var loadSet = new HashSet<int>();
        foreach (int index in loadBuildIndexes)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(loadBuildIndexes), $"Scene build index in load list must be non-negative. Found: {index}");
            }
            if (!loadSet.Add(index))
            {
                throw new ArgumentException($"Duplicate scene build index {index} in load list.", nameof(loadBuildIndexes));
            }
            loadList.Add(index);
        }

        var unloadList = new List<int>();
        var unloadSet = new HashSet<int>();
        foreach (int index in unloadBuildIndexes)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(unloadBuildIndexes), $"Scene build index in unload list must be non-negative. Found: {index}");
            }
            if (!unloadSet.Add(index))
            {
                throw new ArgumentException($"Duplicate scene build index {index} in unload list.", nameof(unloadBuildIndexes));
            }
            unloadList.Add(index);
        }

        if (loadSet.Overlaps(unloadSet))
        {
            throw new ArgumentException("Load and unload build indexes must not overlap.", nameof(unloadBuildIndexes));
        }

        if (unloadSet.Contains(activeSceneBuildIndex))
        {
            throw new ArgumentException($"Active scene build index {activeSceneBuildIndex} cannot be present in the unload list.", nameof(activeSceneBuildIndex));
        }

        TargetSceneBuildIndex = targetSceneBuildIndex;
        LoadBuildIndexes = loadList.AsReadOnly();
        UnloadBuildIndexes = unloadList.AsReadOnly();
        ActiveSceneBuildIndex = activeSceneBuildIndex;
        MinimumVisibleDuration = minimumVisibleDuration;
        Presentation = presentation;
    }

    public bool Equals(AdditiveSceneTransitionRequest other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        if (TargetSceneBuildIndex != other.TargetSceneBuildIndex ||
            ActiveSceneBuildIndex != other.ActiveSceneBuildIndex ||
            Math.Abs(MinimumVisibleDuration - other.MinimumVisibleDuration) > 0.0001f ||
            Presentation != other.Presentation ||
            LoadBuildIndexes.Count != other.LoadBuildIndexes.Count ||
            UnloadBuildIndexes.Count != other.UnloadBuildIndexes.Count)
        {
            return false;
        }

        for (int i = 0; i < LoadBuildIndexes.Count; i++)
        {
            if (LoadBuildIndexes[i] != other.LoadBuildIndexes[i]) return false;
        }

        for (int i = 0; i < UnloadBuildIndexes.Count; i++)
        {
            if (UnloadBuildIndexes[i] != other.UnloadBuildIndexes[i]) return false;
        }

        return true;
    }

    public override bool Equals(object obj) => obj is AdditiveSceneTransitionRequest other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(TargetSceneBuildIndex);
        hash.Add(ActiveSceneBuildIndex);
        hash.Add(MinimumVisibleDuration);
        hash.Add(Presentation);
        foreach (int index in LoadBuildIndexes) hash.Add(index);
        foreach (int index in UnloadBuildIndexes) hash.Add(index);
        return hash.ToHashCode();
    }

    public override string ToString() =>
        $"AdditiveSceneTransitionRequest(Target: {TargetSceneBuildIndex}, Active: {ActiveSceneBuildIndex}, Load: [{string.Join(", ", LoadBuildIndexes)}], Unload: [{string.Join(", ", UnloadBuildIndexes)}], MinDuration: {MinimumVisibleDuration}s, Presentation: {Presentation})";
}
