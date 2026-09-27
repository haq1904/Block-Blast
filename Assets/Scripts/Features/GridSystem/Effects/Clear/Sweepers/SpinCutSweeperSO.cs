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
        Vector3 spawnPos = cutStartPos - dir * spawnDistance + Vector3.up * dropHeight;
        Vector3 cutEndPos = endCellPos + dir * 0.5f + forwardVec + Vector3.up * heightOffset;
        Vector3 exitPos = cutEndPos + dir * 1.0f + Vector3.up * heightOffset;

        Quaternion sweeperRot = Mathf.Abs(dir.z) > Mathf.Abs(dir.x)
            ? Quaternion.Euler(90f, 90f, 0f)
            : Quaternion.Euler(90f, 0f, 0f);

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

        visual.localPosition = Vector3.zero;
        visual.localRotation = Quaternion.identity;
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

        seq.OnComplete(() =>
        {
            ResetSweeperVisualState(sweeper);
            if (visual != null && visual != sweeper.transform)
            {
                visual.localPosition = Vector3.zero;
                visual.localRotation = Quaternion.identity;
            }
            poolService?.ReturnObjectToPool(sweeper, PoolType.GameObject);
        });

        return seq;
    }
}
