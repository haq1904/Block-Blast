using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(fileName = "StiffMicroShake_PlacementEffect", menuName = "Block Blast/Effects/Placement/Stiff Micro Shake")]
public class StiffMicroShakePlacementEffectSO : PlacementEffectSO
{
    [Tooltip("Magnitude of the positional micro-shake vibration.")]
    public float shakeStrength = 0.04f;

    [Tooltip("Duration of the micro-shake.")]
    public float duration = 0.1f;

    [Tooltip("Frequency of vibrations.")]
    public int vibrato = 25;

    public override void Apply(Transform target)
    {
        if (target == null) return;
        target.DOKill();
        target.localScale = Vector3.one;
        target.localRotation = Quaternion.identity;

        target.DOShakePosition(duration, shakeStrength, vibrato, 90f, false, true)
            .SetTarget(target)
            .SetLink(target.gameObject, LinkBehaviour.KillOnDisable);
    }
}
