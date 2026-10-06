using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(fileName = "RollingYarnBallSweeper", menuName = "Block Blast/Effects/Clear Sweeper/Rolling Yarn Ball")]
public class RollingYarnBallSweeperSO : ClearSweeperBase
{
    [Header("Rolling Motion")]
    [Range(0.1f, 1f)] public float ballRadius = 0.38f;
    [Range(0f, 0.25f)] public float swayAmplitude = 0.11f;
    [Range(0.5f, 5f)] public float swayCycles = 2f;
    [Range(0f, 0.35f)] public float entryBounceHeight = 0.14f;

    public override Sequence AnimateSweeperSequence(GameObject sweeper, ClearTimelineContext timeline, IPoolService poolService)
    {
        if (sweeper == null || timeline == null) return null;

        Vector3 direction = ResolveDirection(timeline.direction);
        Vector3 lateral = Vector3.Cross(Vector3.up, direction).normalized;
        Vector3 sweepStart = timeline.startPos - direction * 0.5f + direction * forwardOffset + Vector3.up * heightOffset;
        Vector3 spawn = sweepStart - direction * spawnDistance + Vector3.up * (dropHeight + entryBounceHeight);
        Vector3 sweepEnd = timeline.endPos + direction * 0.5f + direction * forwardOffset + Vector3.up * heightOffset;
        Vector3 exit = sweepEnd + direction * 1f + Vector3.up * entryBounceHeight;

        sweeper.transform.DOKill();
        sweeper.transform.position = spawn;
        sweeper.transform.rotation = Quaternion.identity;
        Transform visual = sweeper.transform.Find("Visual") ?? (sweeper.transform.childCount > 0 ? sweeper.transform.GetChild(0) : sweeper.transform);
        Quaternion visualRest = visual.localRotation;
        Vector3 visualPositionRest = visual.localPosition;

        PrepareSweeperForEntry(sweeper);

        Sequence seq = DOTween.Sequence();
        seq.SetTarget(sweeper);
        seq.SetLink(sweeper, LinkBehaviour.KillOnDisable);
        if (timeline.leadOffset > 0f) seq.AppendInterval(timeline.leadOffset);

        seq.AppendCallback(PlayEntrySound);
        seq.Append(sweeper.transform.DOMove(sweepStart, entryDuration).SetEase(Ease.OutBounce));
        ApplyFadeIn(seq, sweeper, entryDuration);

        seq.AppendCallback(() =>
        {
            SetSweeperParticles(sweeper, true, direction);
            PlaySweepSound();
        });

        float distance = Vector3.Distance(sweepStart, sweepEnd);
        float rollDegrees = distance / Mathf.Max(0.05f, ballRadius) * Mathf.Rad2Deg;
        Vector3 rollAxis = Vector3.Cross(direction, Vector3.up).normalized;
        float swaySign = Random.value < 0.5f ? -1f : 1f;

        seq.Append(DOTween.To(() => 0f, progress =>
        {
            if (sweeper == null || visual == null) return;
            float envelope = Mathf.Sin(progress * Mathf.PI);
            float sway = Mathf.Sin(progress * Mathf.PI * 2f * swayCycles) * swayAmplitude * swaySign * envelope;
            sweeper.transform.position = Vector3.LerpUnclamped(sweepStart, sweepEnd, progress) + lateral * sway;
            visual.localRotation = visualRest * Quaternion.AngleAxis(rollDegrees * progress, rollAxis);
        }, 1f, timeline.totalSweepDuration).SetEase(Ease.Linear));

        seq.AppendCallback(() =>
        {
            SetSweeperParticles(sweeper, false);
            PlayExitSound();
        });
        seq.Append(sweeper.transform.DOMove(exit, exitDuration).SetEase(Ease.InQuad));
        ApplyFadeOut(seq, sweeper, exitDuration);

        seq.OnComplete(() =>
        {
            if (visual != null)
            {
                visual.localRotation = visualRest;
                visual.localPosition = visualPositionRest;
            }
            ResetSweeperVisualState(sweeper);
            poolService?.ReturnObjectToPool(sweeper, PoolType.GameObject);
        });
        return seq;
    }

    private static Vector3 ResolveDirection(Vector3 direction)
    {
        direction.y = 0f;
        return direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.forward;
    }
}
