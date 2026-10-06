using DG.Tweening;
using UnityEngine;

/// <summary>
/// Motion-centric sweeper for "Arc Flight & Bounded Bounce" projectile choreography (e.g. Tennis Ball).
/// Orchestrates: Curved entry arc -> Linear horizontal trajectory with rhythmic ground bounces -> Exit arc.
/// Guarantees that X/Z contact timing remains 100% linear and synchronized with cell destruction rhythms,
/// while Y visual bounces provide bouncy athletic juice without physics drift.
/// Adheres strictly to dotween.md (Stateless SO), visual_effects.md, and architecture.md.
/// </summary>
[CreateAssetMenu(fileName = "NewArcBounceSweeper", menuName = "Block Blast/Effects/Clear Sweeper/Arc & Bounce Sweeper")]
public class ArcBounceSweeperSO : ClearSweeperBase
{
    [Header("Arc & Bounce Dynamics")]
    [Tooltip("Maximum arc apex height during initial flight/entry.")]
    public float arcHeight = 0.55f;

    [Tooltip("Duration in seconds of the entry arc towards the cutting line.")]
    public float arcDuration = 0.22f;

    [Range(1, 3)]
    [Tooltip("Number of rhythmic ground bounces along the line during sweep (capped at 3).")]
    public int bounceCount = 2;

    [Tooltip("Apex height of bounces along the line in world units.")]
    public float bounceHeight = 0.18f;

    [Tooltip("Duration in seconds for an individual bounce cycle. If <= 0, automatically adapts to sweep duration.")]
    public float bounceDuration = 0.10f;

    [Tooltip("Easing curve for entry arc motion.")]
    public Ease arcEase = Ease.InOutSine;

    [Header("Ball Spin Settings")]
    [Tooltip("Rotations per minute for rolling/spinning motion.")]
    public float spinRpm = 720f;

    [Tooltip("Local axis around which the ball spins/rolls.")]
    public Vector3 spinAxis = Vector3.right;

    [Header("Viewport Exit Settings")]
    [Tooltip("If true, computes exit destination outside the camera's viewport frustum.")]
    public bool exitOutsideViewport = true;

    [Range(0f, 0.25f)]
    [Tooltip("Viewport margin padding beyond [0, 1] screen bounds (e.g. 0.1 = 10% outside screen).")]
    public float exitViewportPadding = 0.1f;

    [Min(0.5f)]
    [Tooltip("Fallback distance in world units past the line if camera or viewport projection is unavailable.")]
    public float fallbackExitDistance = 1.5f;

    public override float TotalPreSweepDuration => arcDuration > 0.001f ? arcDuration : entryDuration;

    public override Vector3 ResolveVisualExitPosition(ClearTimelineContext timeline)
    {
        return ComputeViewportExitPosition(
            timeline,
            forwardOffset,
            heightOffset,
            exitOutsideViewport,
            exitViewportPadding,
            fallbackExitDistance);
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
        float entryDur = TotalPreSweepDuration;

        Vector3 forwardVec = dir * forwardOffset;
        Vector3 cutStartPos = startCellPos - dir * 0.5f + forwardVec + Vector3.up * heightOffset;
        Vector3 spawnPos = cutStartPos - dir * spawnDistance + Vector3.up * dropHeight;
        Vector3 cutEndPos = endCellPos + dir * 0.5f + forwardVec + Vector3.up * heightOffset;
        Vector3 exitPos = timeline.visualExitPosition != Vector3.zero
            ? timeline.visualExitPosition
            : ResolveVisualExitPosition(timeline);

        Quaternion flightRotation = dir.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(dir, Vector3.up)
            : Quaternion.identity;

        sweeper.transform.position = spawnPos;
        sweeper.transform.rotation = flightRotation;

        Transform visual = sweeper.transform.Find("Visual") ?? sweeper.transform.Find("VisualBlade");
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

        // Phase 1: Entry arc towards cutting start & Fade in & Spin
        seq.AppendCallback(PlayEntrySound);
        seq.Append(sweeper.transform.DOMove(cutStartPos, entryDur).SetEase(arcEase));
        ApplyFadeIn(seq, sweeper, entryDur);

        if (arcHeight > 0.001f && visual != null && visual != sweeper.transform)
        {
            float halfEntry = entryDur * 0.5f;
            Sequence arcSeq = DOTween.Sequence();
            arcSeq.SetTarget(sweeper);
            arcSeq.SetLink(sweeper.gameObject, LinkBehaviour.KillOnDisable);
            arcSeq.Append(visual.DOLocalMoveY(restLocalPos.y + arcHeight, halfEntry).SetEase(Ease.OutSine));
            arcSeq.Append(visual.DOLocalMoveY(restLocalPos.y, halfEntry).SetEase(Ease.InSine));
            seq.Join(arcSeq);
        }

        if (spinRpm > 0f && visual != null)
        {
            float rotEntry = 360f * (spinRpm / 60f) * entryDur;
            seq.Join(visual.DOLocalRotate(spinAxis.normalized * rotEntry, entryDur, RotateMode.LocalAxisAdd).SetEase(Ease.Linear));
        }

        // Phase 2: Linear sweep across the line with rhythmic ground bounces and continuous spin
        seq.AppendCallback(() =>
        {
            SetSweeperParticles(sweeper, true, dir);
            PlaySweepSound();
        });

        // 100% linear root movement ensures cell contact timing is strictly preserved
        seq.Append(sweeper.transform.DOMove(cutEndPos, sweepDuration).SetEase(Ease.Linear));

        if (visual != null && visual != sweeper.transform && bounceCount > 0 && bounceHeight > 0.001f)
        {
            int bounces = Mathf.Clamp(bounceCount, 1, 3);
            float singleBounceDuration = sweepDuration / bounces;
            float halfBounce = singleBounceDuration * 0.5f;

            Sequence bounceSeq = DOTween.Sequence();
            bounceSeq.SetTarget(sweeper);
            bounceSeq.SetLink(sweeper.gameObject, LinkBehaviour.KillOnDisable);
            for (int b = 0; b < bounces; b++)
            {
                bounceSeq.Append(visual.DOLocalMoveY(restLocalPos.y + bounceHeight, halfBounce).SetEase(Ease.OutSine));
                bounceSeq.Append(visual.DOLocalMoveY(restLocalPos.y, halfBounce).SetEase(Ease.InSine));
            }
            seq.Join(bounceSeq);
        }

        if (spinRpm > 0f && visual != null)
        {
            float rotSweep = 360f * (spinRpm / 60f) * sweepDuration;
            seq.Join(visual.DOLocalRotate(spinAxis.normalized * rotSweep, sweepDuration, RotateMode.LocalAxisAdd).SetEase(Ease.Linear));
        }

        // Phase 3: Exit arc & Fade out & Spin
        seq.AppendCallback(() =>
        {
            SetSweeperParticles(sweeper, false);
            PlayExitSound();
        });
        seq.Append(sweeper.transform.DOMove(exitPos, exitDuration).SetEase(Ease.InQuad));
        ApplyFadeOut(seq, sweeper, exitDuration);

        if (spinRpm > 0f && visual != null)
        {
            float rotExit = 360f * (spinRpm / 60f) * exitDuration;
            seq.Join(visual.DOLocalRotate(spinAxis.normalized * rotExit, exitDuration, RotateMode.LocalAxisAdd).SetEase(Ease.Linear));
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
}

