using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(fileName = "PetCarrierPickupSweeper", menuName = "Block Blast/Effects/Clear Sweeper/Pet Carrier Pickup")]
public class PetCarrierPickupSweeperSO : ClearSweeperBase
{
    [Header("Carrier Motion")]
    [Range(0f, 0.15f)] public float bobHeight = 0.035f;
    [Range(1f, 8f)] public float bobCycles = 3f;
    [Range(0f, 90f)] public float doorOpenAngle = 55f;
    [Range(0f, 1f)] public float wheelRotationsPerUnit = 0.7f;

    public override Sequence AnimateSweeperSequence(GameObject sweeper, ClearTimelineContext timeline, IPoolService poolService)
    {
        if (sweeper == null || timeline == null) return null;

        Vector3 direction = ResolveDirection(timeline.direction);
        Vector3 start = timeline.startPos - direction * spawnDistance + direction * forwardOffset + Vector3.up * heightOffset;
        Vector3 sweepStart = timeline.startPos - direction * 0.5f + direction * forwardOffset + Vector3.up * heightOffset;
        Vector3 sweepEnd = timeline.endPos + direction * 0.5f + direction * forwardOffset + Vector3.up * heightOffset;
        Vector3 exit = sweepEnd + direction * 1f + Vector3.up * 0.15f;
        Quaternion facing = Quaternion.LookRotation(direction, Vector3.up);

        sweeper.transform.DOKill();
        sweeper.transform.position = start + Vector3.up * dropHeight;
        sweeper.transform.rotation = facing;

        Transform visual = FindChildOrRoot(sweeper.transform, "Visual");
        Transform door = FindChildOrNull(sweeper.transform, "Door");
        Transform wheelLeft = FindChildOrNull(sweeper.transform, "WheelLeft");
        Transform wheelRight = FindChildOrNull(sweeper.transform, "WheelRight");
        Quaternion doorRest = door != null ? door.localRotation : Quaternion.identity;
        Quaternion leftRest = wheelLeft != null ? wheelLeft.localRotation : Quaternion.identity;
        Quaternion rightRest = wheelRight != null ? wheelRight.localRotation : Quaternion.identity;
        Vector3 visualRest = visual != null && visual != sweeper.transform ? visual.localPosition : Vector3.zero;

        PrepareSweeperForEntry(sweeper);

        Sequence seq = DOTween.Sequence();
        seq.SetTarget(sweeper);
        seq.SetLink(sweeper, LinkBehaviour.KillOnDisable);
        if (timeline.leadOffset > 0f) seq.AppendInterval(timeline.leadOffset);

        seq.AppendCallback(PlayEntrySound);
        seq.Append(sweeper.transform.DOMove(sweepStart, entryDuration).SetEase(Ease.OutQuad));
        ApplyFadeIn(seq, sweeper, entryDuration);
        if (door != null)
        {
            seq.Join(door.DOLocalRotateQuaternion(doorRest * Quaternion.Euler(0f, doorOpenAngle, 0f), entryDuration).SetEase(Ease.OutBack));
        }

        seq.AppendCallback(() =>
        {
            SetSweeperParticles(sweeper, true, direction);
            PlaySweepSound();
        });
        seq.Append(sweeper.transform.DOMove(sweepEnd, timeline.totalSweepDuration).SetEase(Ease.Linear));

        float travelDistance = Vector3.Distance(sweepStart, sweepEnd);
        float wheelAngle = travelDistance * wheelRotationsPerUnit * 360f;
        if (wheelLeft != null) seq.Join(wheelLeft.DOLocalRotate(new Vector3(wheelAngle, 0f, 0f), timeline.totalSweepDuration, RotateMode.LocalAxisAdd).SetEase(Ease.Linear));
        if (wheelRight != null) seq.Join(wheelRight.DOLocalRotate(new Vector3(wheelAngle, 0f, 0f), timeline.totalSweepDuration, RotateMode.LocalAxisAdd).SetEase(Ease.Linear));

        if (visual != null && visual != sweeper.transform && bobHeight > 0f && bobCycles > 0f)
        {
            seq.Join(DOTween.To(() => 0f, progress =>
            {
                if (visual == null) return;
                float envelope = Mathf.Sin(progress * Mathf.PI);
                float bob = Mathf.Abs(Mathf.Sin(progress * Mathf.PI * 2f * bobCycles)) * bobHeight * envelope;
                visual.localPosition = visualRest + Vector3.up * bob;
            }, 1f, timeline.totalSweepDuration).SetEase(Ease.Linear));
        }

        seq.AppendCallback(() =>
        {
            SetSweeperParticles(sweeper, false);
            PlayExitSound();
        });
        seq.Append(sweeper.transform.DOMove(exit, exitDuration).SetEase(Ease.InQuad));
        ApplyFadeOut(seq, sweeper, exitDuration);
        if (door != null) seq.Join(door.DOLocalRotateQuaternion(doorRest, exitDuration).SetEase(Ease.InQuad));

        seq.OnComplete(() =>
        {
            if (visual != null && visual != sweeper.transform) visual.localPosition = visualRest;
            if (door != null) door.localRotation = doorRest;
            if (wheelLeft != null) wheelLeft.localRotation = leftRest;
            if (wheelRight != null) wheelRight.localRotation = rightRest;
            ResetSweeperVisualState(sweeper);
            poolService?.ReturnObjectToPool(sweeper, PoolType.GameObject);
        });
        return seq;
    }

    private static Transform FindChildOrRoot(Transform root, string childName)
    {
        Transform child = FindChildOrNull(root, childName);
        return child != null ? child : root;
    }

    private static Transform FindChildOrNull(Transform root, string childName)
    {
        if (root == null) return null;
        Transform direct = root.Find(childName);
        if (direct != null) return direct;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildOrNull(root.GetChild(i), childName);
            if (found != null) return found;
        }
        return null;
    }

    private static Vector3 ResolveDirection(Vector3 direction)
    {
        direction.y = 0f;
        return direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.forward;
    }
}
