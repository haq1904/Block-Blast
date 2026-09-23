using System;
using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(fileName = "JellyWobble_ClearAnimation", menuName = "Block Blast/Effects/Grid/Clear/Jelly Wobble Animation")]
public class JellyWobbleClearAnimationSO : ClearAnimationSO
{
    [Header("Jelly Wobble")]
    [Tooltip("Total duration of jelly wobble oscillation in seconds.")]
    [Range(0.04f, 0.3f)]
    public float wobbleDuration = 0.12f;

    [Tooltip("Punch scale deformation vector across axes.")]
    public Vector3 punchScale = new Vector3(0.35f, -0.3f, 0.35f);

    [Tooltip("Vibrato count of elastic oscillations.")]
    [Range(5, 20)]
    public int vibrato = 10;

    [Tooltip("Elasticity of wobble.")]
    [Range(0.1f, 2.0f)]
    public float elasticity = 1.0f;

    [Header("Jelly Wobble Shader Feedback")]
    [Tooltip("Increases specular glossiness and wobble sheen while oscillating (requires useShaderFeedback = true).")]
    public bool enableWobbleShine = true;

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

        // 3D Jelly punch wobble
        seq.Append(target.DOPunchScale(punchScale, wobbleDuration, vibrato, elasticity));

        // Climax reached: trigger explosion callback
        seq.AppendCallback(() => onExplode?.Invoke());
    }
}
