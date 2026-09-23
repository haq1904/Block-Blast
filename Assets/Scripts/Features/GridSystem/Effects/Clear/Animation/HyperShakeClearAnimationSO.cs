using System;
using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(fileName = "HyperShake_ClearAnimation", menuName = "Block Blast/Effects/Grid/Clear/Hyper Shake Animation")]
public class HyperShakeClearAnimationSO : ClearAnimationSO
{
    [Header("Hyper Shake")]
    [Tooltip("Total duration of intense violent shake in seconds.")]
    [Range(0.04f, 1f)]
    public float shakeDuration = 0.09f;

    [Tooltip("Shake position displacement amplitude.")]
    [Range(0.01f, 0.3f)]
    public float shakeStrength = 0.08f;

    [Tooltip("Frequency vibrato of shakes.")]
    [Range(10, 50)]
    public int vibrato = 30;

    [Tooltip("Scale swell during shake.")]
    [Range(1.0f, 1.5f)]
    public float anticipationScale = 1.15f;

    [Header("Hyper Shake Shader Feedback")]
    [Tooltip("Pulses high-intensity HDR emission during high-frequency vibration (requires useShaderFeedback = true).")]
    public bool pulseEmission = true;

    [ColorUsage(true, true)]
    [Tooltip("HDR color used for overloading electrical emission.")]
    public Color emissionColor = new Color(1f, 0.85f, 0.3f, 1f);

    [Range(1f, 8f)]
    [Tooltip("Maximum emission intensity multiplier at peak oscillation.")]
    public float maxEmissionIntensity = 4.0f;

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

        // Violent high-frequency position shake and scale swell
        seq.Append(target.DOScale(anticipationScale, shakeDuration).SetEase(Ease.OutQuad));
        seq.Join(target.DOShakePosition(shakeDuration, shakeStrength, vibrato, 90, false, true));

        // Climax reached: trigger explosion callback
        seq.AppendCallback(() => onExplode?.Invoke());
    }
}
