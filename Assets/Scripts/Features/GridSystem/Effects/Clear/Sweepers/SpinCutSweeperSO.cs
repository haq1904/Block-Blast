using DG.Tweening;
using UnityEngine;

/// <summary>
/// Motion-centric sweeper for "Linear Sweep & High-Speed Spin" choreography.
/// Orchestrates: Slide in with spin -> Linear cutting sweep with continuous spin and friction micro-shake -> Fly out exit.
/// Highly decoupled: accommodates saw blades, ninja shurikens, spiked wheels, chain discs, or spinner tops.
/// Adheres strictly to dotween.md (Stateless SO), visual_effects.md, and architecture.md.
/// </summary>
[CreateAssetMenu(fileName = "NewSpinCutSweeper", menuName = "Block Blast/Effects/Clear Sweeper/Spin & Cut Sweeper")]
public class SpinCutSweeperSO : ClearSweeperBase
{
    public enum FlightOrientation
    {
        SweepAligned = 0,
        Horizontal = 1
    }

    [Header("Flight Orientation")]
    [Tooltip("Controls how the sweeper root is oriented during flight.")]
    public FlightOrientation flightOrientation = FlightOrientation.SweepAligned;

    [Header("Spin Rotation Settings")]
    [Tooltip("Rotations per minute for the rotating visual component.")]
    public float rpm = 1200f;

    [Tooltip("Local axis around which the visual spins.")]
    public Vector3 rotationAxis = Vector3.up;

    [Header("Cutting Shake")]
    [Tooltip("Shake amplitude for the visual component during cutting friction.")]
    public float shakeStrength = 0.035f;

    [Tooltip("Shake vibrato frequency for high-speed mechanical jitter.")]
    public int shakeVibrato = 40;

    [Header("Viewport Entry Settings")]
    [Tooltip("If true, computes entry spawn position outside the camera's viewport frustum.")]
    public bool enterFromOutsideViewport = false;

    [Range(0f, 0.25f)]
    [Tooltip("Viewport margin padding for entry spawn beyond screen bounds.")]
    public float entryViewportPadding = 0.1f;

    [Min(0f)]
    [Tooltip("Additional world distance beyond the viewport boundary for entry spawn.")]
    public float entryWorldOvershoot = 0f;

    [Min(0.5f)]
    [Tooltip("Fallback distance in world units behind start cut if camera or viewport projection is unavailable.")]
    public float fallbackEntryDistance = 3.0f;

    [Header("Viewport Exit Settings")]
    [Tooltip("If true, computes exit destination outside the camera's viewport frustum.")]
    public bool exitOutsideViewport = false;

    [Range(0f, 0.25f)]
    [Tooltip("Viewport margin padding beyond [0, 1] screen bounds (e.g. 0.1 = 10% outside screen).")]
    public float exitViewportPadding = 0.1f;

    [Min(0f)]
    [Tooltip("Additional world distance beyond the viewport boundary for exit destination.")]
    public float exitWorldOvershoot = 0f;

    [Min(0.5f)]
    [Tooltip("Fallback distance in world units past the line if camera or viewport projection is unavailable.")]
    public float fallbackExitDistance = 1.5f;

    [Header("Post-exit Shrink")]
    [Tooltip("If true, scales the sweeper to zero after fully exiting outside the viewport.")]
    public bool shrinkAfterViewportExit = false;

    [Min(0.01f)]
    [Tooltip("Duration in seconds for scaling to zero after viewport exit.")]
    public float offscreenShrinkDuration = 0.16f;

    [Tooltip("Easing curve for scaling to zero after viewport exit.")]
    public Ease offscreenShrinkEase = Ease.InQuad;

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

    public override Sequence AnimateSweeperSequence(
        GameObject sweeper,
        ClearTimelineContext timeline,
        IPoolService poolService)
    {
        if (sweeper == null || timeline == null) return null;

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

        Quaternion sweeperRot = ResolveFlightRotation(dir);

        sweeper.transform.position = spawnPos;
        sweeper.transform.rotation = sweeperRot;

        Transform visual = sweeper.transform.Find("VisualBlade") ?? sweeper.transform.Find("Blade") ?? sweeper.transform.Find("Visual");
        if (visual == null && sweeper.transform.childCount > 0)
        {
            visual = sweeper.transform.GetChild(0);
        }
        if (visual == null)
        {
            visual = sweeper.transform;
        }

        Vector3 restLocalPos = visual.localPosition;
        Quaternion restLocalRot = visual.localRotation;
        PrepareSweeperForEntry(sweeper);

        Sequence seq = DOTween.Sequence();
        seq.SetTarget(sweeper);
        seq.SetLink(sweeper, LinkBehaviour.KillOnDisable);

        if (leadOffset > 0f)
        {
            seq.AppendInterval(leadOffset);
        }

        // Phase 1: Slide in from sky to cut start & Fade in & Spin
        seq.AppendCallback(PlayEntrySound);
        seq.Append(sweeper.transform.DOMove(cutStartPos, entryDuration).SetEase(Ease.OutQuad));
        ApplyFadeIn(seq, sweeper, entryDuration);
        if (rpm > 0f && visual != null)
        {
            float rot1 = 360f * (rpm / 60f) * entryDuration;
            seq.Join(visual.DOLocalRotate(rotationAxis.normalized * rot1, entryDuration, RotateMode.LocalAxisAdd).SetEase(Ease.Linear));
        }

        // Phase 2: Linear sweep across the line with continuous spin, sparks and mechanical micro-shake
        seq.AppendCallback(() =>
        {
            SetSweeperParticles(sweeper, true, dir);
            PlaySweepSound();
        });
        seq.Append(sweeper.transform.DOMove(cutEndPos, sweepDuration).SetEase(Ease.Linear));
        if (rpm > 0f && visual != null)
        {
            float rot2 = 360f * (rpm / 60f) * sweepDuration;
            seq.Join(visual.DOLocalRotate(rotationAxis.normalized * rot2, sweepDuration, RotateMode.LocalAxisAdd).SetEase(Ease.Linear));
        }
        if (visual != null && visual != sweeper.transform)
        {
            seq.Join(visual.DOShakePosition(sweepDuration, shakeStrength, shakeVibrato, 90f, false, false));
        }

        // Phase 3: Exit & Fade out & Spin
        seq.AppendCallback(() =>
        {
            SetSweeperParticles(sweeper, false);
            PlayExitSound();
        });
        seq.Append(sweeper.transform.DOMove(exitPos, exitDuration).SetEase(Ease.InQuad));
        ApplyFadeOut(seq, sweeper, exitDuration);
        if (rpm > 0f && visual != null)
        {
            float rot3 = 360f * (rpm / 60f) * exitDuration;
            seq.Join(visual.DOLocalRotate(rotationAxis.normalized * rot3, exitDuration, RotateMode.LocalAxisAdd).SetEase(Ease.Linear));
        }

        if (shrinkAfterViewportExit && offscreenShrinkDuration > 0f)
        {
            seq.AppendCallback(() => sweeper.transform.position = exitPos);
            seq.Append(sweeper.transform
                .DOScale(Vector3.zero, offscreenShrinkDuration)
                .SetEase(offscreenShrinkEase));
        }

        seq.OnComplete(() =>
        {
            ResetSweeperVisualState(sweeper);
            if (visual != null && visual != sweeper.transform)
            {
                visual.localPosition = restLocalPos;
                visual.localRotation = restLocalRot;
            }
            poolService?.ReturnObjectToPool(sweeper, PoolType.GameObject);
        });

        return seq;
    }

    private Quaternion ResolveFlightRotation(Vector3 dir)
    {
        if (flightOrientation == FlightOrientation.Horizontal)
        {
            return Quaternion.identity;
        }

        return Mathf.Abs(dir.z) > Mathf.Abs(dir.x)
            ? Quaternion.Euler(90f, 90f, 0f)
            : Quaternion.Euler(90f, 0f, 0f);
    }
}

