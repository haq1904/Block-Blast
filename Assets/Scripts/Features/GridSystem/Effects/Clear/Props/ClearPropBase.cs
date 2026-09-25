using UnityEngine;

/// <summary>
/// Abstract / Virtual base ScriptableObject for line sweeper props (SawBlade, Axe, Hammer, Drill, etc.).
/// Encapsulates prop prefab reference, trajectory offsets, and prop-specific audio feedback.
/// Adheres strictly to dotween.md (Stateless SO) and visual_effects.md.
/// </summary>
[CreateAssetMenu(fileName = "NewClearProp", menuName = "Block Blast/Effects/Clear Prop")]
public class ClearPropBase : ScriptableObject
{
    [Header("Prop Prefab")]
    [Tooltip("The visual prefab spawned from pool in the scene (e.g. SawBlade.prefab).")]
    public GameObject propPrefab;

    [Header("Trajectory & Placement Settings")]
    [Tooltip("Cutting/contact height offset along the Y axis where the prop touches the blocks.")]
    public float heightOffset = 0.5f;

    [Tooltip("Vertical drop height above the cutting position before dropping down (e.g. +2.0 units).")]
    public float dropHeight = 2.0f;

    [Tooltip("Duration in seconds for the prop to drop down and fade in before cutting starts.")]
    public float entryDuration = 0.15f;

    [Tooltip("Duration in seconds for the prop to exit and fade out after cutting completes.")]
    public float exitDuration = 0.1f;

    [Header("Prop Audio Feedback")]
    [Tooltip("Audio clip played when prop drops down/enters the board.")]
    public AudioClip entrySound;

    [Tooltip("Audio clip played when prop begins actively sweeping/cutting across the line.")]
    public AudioClip sweepSound;

    [Tooltip("Audio clip played when prop exits the board.")]
    public AudioClip exitSound;

    [Range(0f, 1f)]
    [Tooltip("Volume multiplier for prop audio clips.")]
    public float soundVolume = 1.0f;

    public virtual void PlayEntrySound()
    {
        PlaySound(entrySound);
    }

    public virtual void PlaySweepSound()
    {
        PlaySound(sweepSound);
    }

    public virtual void PlayExitSound()
    {
        PlaySound(exitSound);
    }

    protected void PlaySound(AudioClip clip)
    {
        if (clip != null && ServiceLocator.TryGet<ISoundFXService>(out var soundService))
        {
            soundService.PlaySound(clip, soundVolume);
        }
    }
}
