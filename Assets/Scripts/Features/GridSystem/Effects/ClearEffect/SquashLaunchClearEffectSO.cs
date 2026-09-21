using System;
using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(fileName = "NewSquashLaunchClearEffect", menuName = "Block Blast/Effects/Grid/Clear/Squash Launch")]
public class SquashLaunchClearEffectSO : ClearAnimationEffectSO
{
    [Header("Anticipation Squash")]
    [Tooltip("Scale vector when squashing flat against the board.")]
    public Vector3 squashScale = new Vector3(1.25f, 0.55f, 1.25f);

    [Tooltip("Duration of squash phase in seconds.")]
    [Range(0.02f, 0.2f)]
    public float squashDuration = 0.05f;

    [Header("Explosive Launch")]
    [Tooltip("Upward height the block launches before bursting.")]
    [Range(0.1f, 1.5f)]
    public float launchHeight = 0.45f;

    [Tooltip("Elongated scale vector while flying upward.")]
    public Vector3 launchScale = new Vector3(0.75f, 1.4f, 0.75f);

    [Tooltip("Maximum random 3D tilt angle during launch.")]
    [Range(0f, 45f)]
    public float randomTiltAngle = 15f;

    [Tooltip("Duration of launch flight in seconds.")]
    [Range(0.04f, 0.3f)]
    public float launchDuration = 0.08f;

    public override void Play(Transform target, ClearCellContext context, Action onExplode)
    {
        if (target == null) return;

        target.DOKill();

        Sequence seq = DOTween.Sequence();
        seq.SetTarget(target);
        seq.SetLink(target.gameObject, LinkBehaviour.KillOnDisable);

        if (context.delay > 0f)
        {
            seq.AppendInterval(context.delay);
        }

        // 1. Squash anticipation
        seq.Append(target.DOScale(squashScale, squashDuration).SetEase(Ease.InQuad));

        // 2. Upward launch with elongation and 3D tilt
        Vector3 peakPos = context.canonicalPos + Vector3.up * launchHeight;
        Vector3 randomEuler = new Vector3(
            UnityEngine.Random.Range(-randomTiltAngle, randomTiltAngle),
            UnityEngine.Random.Range(-randomTiltAngle, randomTiltAngle),
            UnityEngine.Random.Range(-randomTiltAngle, randomTiltAngle));

        seq.Append(target.DOMove(peakPos, launchDuration).SetEase(Ease.OutQuad));
        seq.Join(target.DOScale(launchScale, launchDuration).SetEase(Ease.OutQuad));
        seq.Join(target.DORotate(randomEuler, launchDuration, RotateMode.Fast));

        // 3. Peak reached: trigger explosion callback
        seq.AppendCallback(() => onExplode?.Invoke());
    }
}
