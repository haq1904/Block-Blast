using DG.Tweening;
using UnityEngine;

/// <summary>
/// Controls continuous high-speed rotation and material property block transparency for the saw blade prop.
/// Adheres strictly to dotween.md: kills tweens on disable, avoids memory leaks, and restores canonical transform.
/// </summary>
public class SawBladeSpinner : MonoBehaviour
{
    [Header("Visual Mesh Reference")]
    [Tooltip("Child transform containing the blade mesh to spin and shake. If null, falls back to this transform.")]
    [SerializeField] private Transform bladeVisual;

    [Header("Rotation Settings")]
    [Tooltip("Rotations per minute for the saw blade disc.")]
    [SerializeField] private float rpm = 1200f;

    [Tooltip("Local axis around which the blade spins.")]
    [SerializeField] private Vector3 rotationAxis = Vector3.up;

    [Header("Cutting Shake Settings")]
    [Tooltip("Shake strength amplitude for the blade during wood cutting.")]
    [SerializeField] private float shakeStrength = 0.035f;

    [Tooltip("Shake vibrato frequency for high-speed mechanical jitter.")]
    [SerializeField] private int shakeVibrato = 40;

    [Header("Sparks VFX")]
    [Tooltip("Particle system emitting cutting sparks at the bottom edge of the blade.")]
    [SerializeField] private ParticleSystem sparksParticle;

    [Tooltip("Pivot transform of the spark emitter to align direction opposite to movement.")]
    [SerializeField] private Transform sparksPivot;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private Tween spinTween;
    private Tween shakeTween;
    private Quaternion canonicalLocalRotation;
    private Quaternion canonicalVisualRotation;
    private Vector3 canonicalVisualLocalPos;
    private Renderer[] cachedRenderers;
    private MaterialPropertyBlock propBlock;

    private void Awake()
    {
        canonicalLocalRotation = transform.localRotation;
        if (bladeVisual != null)
        {
            canonicalVisualRotation = bladeVisual.localRotation;
            canonicalVisualLocalPos = bladeVisual.localPosition;
        }
        cachedRenderers = GetComponentsInChildren<Renderer>(true);
        propBlock = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        StartSpinning();
    }

    private void OnDisable()
    {
        spinTween?.Kill();
        spinTween = null;

        StopCuttingFeedback();

        transform.localRotation = canonicalLocalRotation;
        if (bladeVisual != null)
        {
            bladeVisual.localRotation = canonicalVisualRotation;
            bladeVisual.localPosition = canonicalVisualLocalPos;
        }

        if (sparksParticle != null)
        {
            sparksParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        SetAlpha(1f);
    }

    /// <summary>
    /// Starts the continuous high-speed spinning tween.
    /// </summary>
    public void StartSpinning()
    {
        spinTween?.Kill();

        if (rpm <= 0f) return;

        float duration = 60f / rpm;
        Transform target = bladeVisual != null ? bladeVisual : transform;

        spinTween = target.DOLocalRotate(rotationAxis.normalized * 360f, duration, RotateMode.LocalAxisAdd)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Incremental)
            .SetTarget(gameObject)
            .SetLink(gameObject, LinkBehaviour.KillOnDisable);
    }

    /// <summary>
    /// Activates spark emission and mechanical micro-shake when the blade begins slicing wood blocks.
    /// </summary>
    /// <param name="sweepDirection">Direction of the line sweep, used to spray sparks backwards.</param>
    public void StartCuttingFeedback(Vector3 sweepDirection)
    {
        // 1. Position and orient spark emitter
        if (sparksPivot != null)
        {
            sparksPivot.position = transform.position + Vector3.down * 0.5f;

            if (sweepDirection.sqrMagnitude > 0.001f)
            {
                Vector3 sprayDir = (-sweepDirection + Vector3.up * 0.35f).normalized;
                sparksPivot.rotation = Quaternion.LookRotation(sprayDir, Vector3.up);
            }
        }

        if (sparksParticle != null)
        {
            sparksParticle.Play();
        }

        // 2. High-frequency micro-jitter on blade visual
        Transform target = bladeVisual != null ? bladeVisual : transform;
        shakeTween?.Kill();
        if (bladeVisual != null)
        {
            bladeVisual.localPosition = canonicalVisualLocalPos;
        }

        shakeTween = target.DOShakePosition(0.15f, shakeStrength, shakeVibrato, 90f, false, false)
            .SetLoops(-1)
            .SetTarget(gameObject)
            .SetLink(gameObject, LinkBehaviour.KillOnDisable);
    }

    /// <summary>
    /// Stops spark emission and terminates the mechanical micro-shake.
    /// </summary>
    public void StopCuttingFeedback()
    {
        if (sparksParticle != null)
        {
            sparksParticle.Stop();
        }

        shakeTween?.Kill();
        shakeTween = null;

        if (bladeVisual != null)
        {
            bladeVisual.localPosition = canonicalVisualLocalPos;
        }
    }

    /// <summary>
    /// Adjusts the transparency of the saw blade via MaterialPropertyBlock without instantiating materials.
    /// </summary>
    /// <param name="alpha">Alpha transparency between 0 (fully transparent) and 1 (fully opaque).</param>
    public void SetAlpha(float alpha)
    {
        if (cachedRenderers == null || cachedRenderers.Length == 0)
        {
            cachedRenderers = GetComponentsInChildren<Renderer>(true);
        }

        if (propBlock == null)
        {
            propBlock = new MaterialPropertyBlock();
        }

        Color c = Color.white;
        c.a = Mathf.Clamp01(alpha);

        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            if (cachedRenderers[i] != null)
            {
                cachedRenderers[i].GetPropertyBlock(propBlock);
                propBlock.SetColor(BaseColorId, c);
                cachedRenderers[i].SetPropertyBlock(propBlock);
            }
        }
    }
}
