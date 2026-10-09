using DG.Tweening;
using UnityEngine;

/// <summary>
/// Motion-centric sweeper for "Smoked Chicken Leg Flight" choreography.
/// Orchestrates: Linear flight with forward +Z orientation fixed at spawn, subtle vertical bobbing and micro-shake,
/// continuous aroma smoke emission, and offscreen shrink exit.
/// Rotation is strictly locked (never animated) throughout entry, sweep, and exit.
/// Adheres strictly to dotween.md, visual_effects.md, and architecture.md.
/// </summary>
[CreateAssetMenu(
    fileName = "NewChickenLegFlightSweeper",
    menuName = "Block Blast/Effects/Clear Sweeper/Chicken Leg Flight Sweeper")]
public sealed class ChickenLegFlightSweeperSO : ClearSweeperBase
{
    [Header("Viewport Entry")]
    [Tooltip("If true, computes entry spawn position outside the camera's viewport frustum.")]
    public bool enterFromOutsideViewport = false;

    [Range(0f, 0.25f)]
    [Tooltip("Viewport margin padding for entry spawn beyond screen bounds.")]
    public float entryViewportPadding = 0.10f;

    [Min(0f)]
    [Tooltip("Additional world distance beyond the viewport boundary for entry spawn.")]
    public float entryWorldOvershoot = 0f;

    [Min(0.5f)]
    [Tooltip("Fallback distance in world units behind start cut if camera or viewport projection is unavailable.")]
    public float fallbackEntryDistance = 3f;

    [Header("Viewport Exit")]
    [Tooltip("If true, computes exit destination outside the camera's viewport frustum.")]
    public bool exitOutsideViewport = true;

    [Range(0f, 0.25f)]
    [Tooltip("Viewport margin padding beyond [0, 1] screen bounds.")]
    public float exitViewportPadding = 0.10f;

    [Min(0f)]
    [Tooltip("Additional world distance beyond the viewport boundary for exit destination.")]
    public float exitWorldOvershoot = 0f;

    [Min(0.5f)]
    [Tooltip("Fallback distance in world units past the line if camera or viewport projection is unavailable.")]
    public float fallbackExitDistance = 1.5f;

    [Header("Chicken Leg Position Juice")]
    [Range(0f, 0.12f)]
    [Tooltip("Vertical bobbing amplitude (floating food motion).")]
    public float bobAmplitude = 0.04f;

    [Min(0f)]
    [Tooltip("Number of full vertical bobbing cycles across the sweep.")]
    public float bobCycles = 1.75f;

    [Tooltip("Local-space positional shake only. Rotation is never animated.")]
    public Vector3 microShakeStrength = new Vector3(0.008f, 0.004f, 0.008f);

    [Min(0f)]
    [Tooltip("Frequency of deterministic positional micro-shake.")]
    public float microShakeFrequency = 11f;

    [Header("Post-exit Shrink")]
    [Tooltip("If true, scales the sweeper to zero after fully exiting outside the viewport.")]
    public bool shrinkAfterViewportExit = true;

    [Min(0.01f)]
    [Tooltip("Duration in seconds for scaling to zero after viewport exit.")]
    public float offscreenShrinkDuration = 0.16f;

    [Tooltip("Easing curve for scaling to zero after viewport exit.")]
    public Ease offscreenShrinkEase = Ease.InQuad;

    [Header("Optional Contact Burst Prefab")]
    [Tooltip("Optional pooled particle prefab spawned on each cell contact.")]
    public GameObject contactBurstPrefab;

    public override float PostExitVisualDuration =>
        shrinkAfterViewportExit ? offscreenShrinkDuration : 0f;

    public override Vector3 ResolveVisualExitPosition(ClearTimelineContext timeline)
    {
        return ComputeViewportExitPosition(
            timeline,
            forwardOffset,
            heightOffset,
            exitOutsideViewport,
            exitViewportPadding,
            fallbackExitDistance,
            exitWorldOvershoot);
    }

    private Tween CreateVisualFlightTween(
        GameObject owner,
        Transform visual,
        Vector3 restLocalPosition,
        float duration)
    {
        if (visual == null || duration <= 0.001f) return null;

        float safeCycles = Mathf.Max(0f, bobCycles);
        float safeFrequency = Mathf.Max(0f, microShakeFrequency);

        return DOTween.To(() => 0f, t =>
        {
            float bobPhase = t * Mathf.PI * 2f * safeCycles;
            float elapsed = t * duration;
            float shakePhase = elapsed * safeFrequency * Mathf.PI * 2f;
            float envelope = Mathf.Sin(t * Mathf.PI);

            Vector3 shake = new Vector3(
                Mathf.Sin(shakePhase),
                Mathf.Sin(shakePhase * 1.37f),
                Mathf.Cos(shakePhase * 0.83f));

            visual.localPosition = restLocalPosition
                + Vector3.up * (Mathf.Sin(bobPhase) * bobAmplitude * envelope)
                + Vector3.Scale(shake, microShakeStrength) * envelope;
        }, 1f, duration)
        .SetEase(Ease.Linear)
        .SetTarget(owner)
        .SetLink(owner, LinkBehaviour.KillOnDisable);
    }

    public override Sequence AnimateSweeperSequence(
        GameObject sweeper,
        ClearTimelineContext timeline,
        IPoolService poolService)
    {
        if (sweeper == null || timeline == null) return null;

        Transform root = sweeper.transform;
        DOTween.Kill(sweeper);
        root.DOKill();

        Vector3 startCellPos = timeline.startPos;
        Vector3 endCellPos = timeline.endPos;
        Vector3 dir = timeline.direction;
        float sweepDuration = timeline.totalSweepDuration;
        float leadOffset = timeline.leadOffset;

        Vector3 forwardVec = dir * forwardOffset;
        Vector3 cutStartPos = startCellPos - dir * 0.5f + forwardVec + Vector3.up * heightOffset;
        Vector3 spawnPos = enterFromOutsideViewport
            ? ComputeViewportBoundaryPosition(
                timeline.viewCamera,
                cutStartPos,
                -dir,
                true,
                entryViewportPadding,
                fallbackEntryDistance,
                entryWorldOvershoot) + Vector3.up * dropHeight
            : cutStartPos - dir * spawnDistance + Vector3.up * dropHeight;
        Vector3 cutEndPos = endCellPos + dir * 0.5f + forwardVec + Vector3.up * heightOffset;
        Vector3 exitPos = timeline.visualExitPosition != Vector3.zero
            ? timeline.visualExitPosition
            : ResolveVisualExitPosition(timeline);

        Quaternion spawnRotation = dir.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(dir.normalized, Vector3.up)
            : Quaternion.identity;
        root.SetPositionAndRotation(spawnPos, spawnRotation);

        Transform visual = root.Find("Visual");
        if (visual == null && root.childCount > 0)
        {
            visual = root.GetChild(0);
        }
        if (visual == null)
        {
            visual = root;
        }

        Vector3 restLocalPos = visual.localPosition;
        Quaternion restLocalRot = visual.localRotation;
        if (visual != null && visual != root)
        {
            visual.DOKill();
            visual.localPosition = restLocalPos;
            visual.localRotation = restLocalRot;
        }

        ResetSweeperVisualState(sweeper);
        ClearSweeperParticles(sweeper);
        PrepareSweeperForEntry(sweeper);

        Sequence seq = DOTween.Sequence()
            .SetTarget(sweeper)
            .SetLink(sweeper, LinkBehaviour.KillOnDisable);

        if (leadOffset > 0f)
        {
            seq.AppendInterval(leadOffset);
        }

        // Phase 1: Slide in from outside viewport/sky to cut start
        seq.AppendCallback(() =>
        {
            SetSweeperParticles(sweeper, true);
            PlayEntrySound();
        });
        seq.Append(root.DOMove(cutStartPos, entryDuration).SetEase(Ease.OutQuad));
        ApplyFadeIn(seq, sweeper, entryDuration);

        // Phase 2: Linear sweep across the line with deterministic bobbing & micro-shake
        seq.AppendCallback(PlaySweepSound);
        seq.Append(root.DOMove(cutEndPos, sweepDuration).SetEase(Ease.Linear));

        if (visual != null && visual != root)
        {
            Tween positionJuice = CreateVisualFlightTween(
                sweeper,
                visual,
                restLocalPos,
                sweepDuration);
            if (positionJuice != null)
            {
                seq.Join(positionJuice);
            }
        }

        if (contactBurstPrefab != null && timeline.waypoints != null && poolService != null)
        {
            for (int w = 0; w < timeline.waypoints.Count; w++)
            {
                var wp = timeline.waypoints[w];
                float contactTime = leadOffset + timeline.firstBlockDelay + wp.hitTime;
                Vector3 burstPos = wp.worldPos;
                seq.InsertCallback(contactTime, () =>
                {
                    poolService.SpawnObject(
                        contactBurstPrefab,
                        burstPos,
                        Quaternion.identity,
                        PoolType.ParticleSystem);
                });
            }
        }

        // Phase 3: Fly out past viewport boundary, then stop emitting and shrink
        seq.AppendCallback(PlayExitSound);
        seq.Append(root.DOMove(exitPos, exitDuration).SetEase(Ease.InQuad));
        seq.AppendCallback(() => SetSweeperParticles(sweeper, false));

        if (shrinkAfterViewportExit && offscreenShrinkDuration > 0f)
        {
            seq.AppendCallback(() => root.position = exitPos);
            seq.Append(root.DOScale(Vector3.zero, offscreenShrinkDuration)
                .SetEase(offscreenShrinkEase));
        }

        seq.OnComplete(() =>
        {
            ClearSweeperParticles(sweeper);
            ResetSweeperVisualState(sweeper);
            if (visual != null && visual != root)
            {
                visual.localPosition = restLocalPos;
                visual.localRotation = restLocalRot;
            }
            poolService?.ReturnObjectToPool(sweeper, PoolType.GameObject);
        });

        return seq;
    }
}
