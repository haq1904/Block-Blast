using System;
using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(fileName = "PetFetch_ClearAnimation", menuName = "Block Blast/Effects/Grid/Clear/Pet/Fetch")]
public class PetFetchClearAnimationSO : ClearAnimationSO
{
    [Header("Notice")]
    [Range(0.01f, 0.15f)] public float noticeDuration = 0.04f;
    [Range(0f, 15f)] public float noticeLeanAngle = 5f;
    public Vector3 noticeScale = new Vector3(0.98f, 1.03f, 0.98f);

    [Header("Crouch")]
    [Range(0.02f, 0.2f)] public float crouchDuration = 0.07f;
    public Vector3 crouchScale = new Vector3(1.08f, 0.78f, 1.08f);
    [Range(0f, 0.15f)] public float crouchDrop = 0.03f;

    [Header("Frisbee Contact / Phase A Chase")]
    [Tooltip("Forward distance along sweep direction during initial chase to reach the release threshold.")]
    public float followForwardDistance = 0.30f;
    [Tooltip("Tilt angle during chase leap.")]
    [Range(0f, 30f)] public float leapTiltAngle = 12f;
    public Vector3 leapScale = new Vector3(0.86f, 1.18f, 0.86f);

    [Header("Gameplay Release")]
    [Tooltip("Y height offset above board where pet triggers immediate gameplay release.")]
    public float gameplayReleaseYOffset = 0.80f;
    [Tooltip("Flight duration in seconds from contact until reaching the release threshold.")]
    public float releaseFlightDuration = 0.15f;

    [Header("Visible Follow")]
    [Tooltip("Duration in seconds that the pet visibly follows the Frisbee after gameplay release and before fading.")]
    [Min(0f)] public float followFlightDuration = 0.34f;

    [Header("Fade Exit")]
    [Tooltip("Duration in seconds of the final movement and fade outside the camera.")]
    [Min(0.01f)] public float fadeOutDuration = 0.16f;
    [Tooltip("Lag distance behind the Frisbee along sweep direction during exit flight.")]
    public float followBehindDistance = 0.32f;
    [Tooltip("Additional Y height offset applied during exit flight.")]
    public float exitHeightOffset = 0.20f;
    [Tooltip("Scale multiplier applied during exit flight.")]
    public float exitScale = 0.75f;

    [Header("Fade Materials")]
    [Tooltip("Canonical opaque material used on Pet blocks (e.g. PetMat).")]
    public Material opaquePetMaterial;
    [Tooltip("Transparent fade material (e.g. ShadowPetMat).")]
    public Material fadePetMaterial;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static MaterialPropertyBlock mpbCache;
    private static MaterialPropertyBlock MPB => mpbCache ??= new MaterialPropertyBlock();

    public override float PreExplosionDuration => noticeDuration + crouchDuration + releaseFlightDuration;
    public override float SweepContactOffset => noticeDuration + crouchDuration;

    public override void Play(Transform target, ClearCellContext context, Action onExplode)
    {
        PlayWithLifecycle(target, context, new ClearAnimationLifecycle(onExplode, null));
    }

    public override void PlayWithLifecycle(
        Transform target,
        ClearCellContext context,
        ClearAnimationLifecycle lifecycle)
    {
        if (target == null) return;

        target.DOKill();
        RestoreCanonicalMaterials(target, opaquePetMaterial);

        Vector3 rootPos = context.canonicalPos;
        Quaternion rootRot = context.canonicalRotation;
        Vector3 direction = ResolveDirection(context.sweepDirection);
        Vector3 leanAxis = Vector3.Cross(Vector3.up, direction).normalized;
        Quaternion noticeRot = Quaternion.AngleAxis(noticeLeanAngle, leanAxis) * rootRot;
        Quaternion leapRot = Quaternion.AngleAxis(leapTiltAngle, leanAxis) * rootRot;

        target.position = rootPos;
        target.rotation = rootRot;
        target.localScale = Vector3.one;

        Sequence seq = DOTween.Sequence();
        seq.SetTarget(target);
        seq.SetLink(target.gameObject, LinkBehaviour.KillOnDisable);

        if (context.delay > 0f) seq.AppendInterval(context.delay);

        // Anticipation: Notice & Crouch
        seq.Append(target.DORotateQuaternion(noticeRot, noticeDuration).SetEase(Ease.OutSine));
        seq.Join(target.DOScale(noticeScale, noticeDuration).SetEase(Ease.OutSine));

        seq.Append(target.DOMove(rootPos - Vector3.up * crouchDrop, crouchDuration).SetEase(Ease.InQuad));
        seq.Join(target.DOScale(crouchScale, crouchDuration).SetEase(Ease.InQuad));

        // Phase A: Chase Frisbee to release threshold
        Vector3 releasePoint = rootPos + direction * followForwardDistance + Vector3.up * gameplayReleaseYOffset;
        seq.Append(target.DOMove(releasePoint, releaseFlightDuration).SetEase(Ease.OutQuad));
        seq.Join(target.DOScale(leapScale, releaseFlightDuration).SetEase(Ease.OutQuad));
        seq.Join(target.DORotateQuaternion(leapRot, releaseFlightDuration).SetEase(Ease.OutSine));

        // Climax / Threshold: spawn step VFX and release cell logically for immediate block placement
        seq.AppendCallback(() =>
        {
            SpawnVFX(target, releasePoint, target.rotation);
            lifecycle.ReleaseGameplay();
        });

        // Tail path calculation with continuous linear velocity
        Vector3 exitTarget = context.visualExitPosition != Vector3.zero
            ? context.visualExitPosition
            : (releasePoint + direction * 2.0f);
        Vector3 petExit = exitTarget - direction * followBehindDistance + Vector3.up * exitHeightOffset;

        float clampedFadeOut = Mathf.Max(0.01f, fadeOutDuration);
        float totalTailDuration = followFlightDuration + clampedFadeOut;
        float followRatio = totalTailDuration > 0.0001f ? (followFlightDuration / totalTailDuration) : 0f;
        Vector3 followPoint = Vector3.Lerp(releasePoint, petExit, followRatio);

        // Phase B: Visible Follow
        if (followFlightDuration > 0f)
        {
            seq.Append(target.DOMove(followPoint, followFlightDuration).SetEase(Ease.Linear));
        }

        // Phase C: Fade Exit
        seq.AppendCallback(() =>
        {
            ApplyFadeMaterial(target, opaquePetMaterial, fadePetMaterial);
        });
        seq.Append(target.DOMove(petExit, clampedFadeOut).SetEase(Ease.Linear));
        seq.Join(target.DOScale(Vector3.one * exitScale, clampedFadeOut).SetEase(Ease.Linear));
        seq.Join(DOTween.To(() => 1f, a => SetTargetAlpha(target, a), 0f, clampedFadeOut).SetEase(Ease.Linear));

        // Cleanup and visual complete
        seq.OnComplete(() =>
        {
            RestoreCanonicalMaterials(target, opaquePetMaterial);
            lifecycle.CompleteVisual();
        });
    }

    public override void Cancel(Transform target, Vector3 canonicalPos, Quaternion canonicalRot)
    {
        if (target == null) return;
        target.DOKill();
        RestoreCanonicalMaterials(target, opaquePetMaterial);
        base.Cancel(target, canonicalPos, canonicalRot);
    }

    private static Vector3 ResolveDirection(Vector3 direction)
    {
        direction.y = 0f;
        return direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.forward;
    }

    private static void ApplyFadeMaterial(Transform target, Material opaqueMat, Material fadeMat)
    {
        if (target == null || fadeMat == null) return;
        var renderers = target.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            var rend = renderers[i];
            if (rend == null) continue;
            var mats = rend.sharedMaterials;
            bool changed = false;
            for (int m = 0; m < mats.Length; m++)
            {
                if (opaqueMat == null || mats[m] == opaqueMat)
                {
                    mats[m] = fadeMat;
                    changed = true;
                }
            }
            if (changed)
            {
                rend.sharedMaterials = mats;
                MPB.Clear();
                rend.GetPropertyBlock(MPB);
                MPB.SetColor(BaseColorId, Color.white);
                rend.SetPropertyBlock(MPB);
            }
        }
    }

    private static void SetTargetAlpha(Transform target, float alpha)
    {
        if (target == null) return;
        var renderers = target.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            var rend = renderers[i];
            if (rend == null) continue;
            MPB.Clear();
            rend.GetPropertyBlock(MPB);
            MPB.SetColor(BaseColorId, new Color(1f, 1f, 1f, alpha));
            rend.SetPropertyBlock(MPB);
        }
    }

    private static void RestoreCanonicalMaterials(Transform target, Material opaqueMat)
    {
        if (target == null) return;
        var renderers = target.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            var rend = renderers[i];
            if (rend == null) continue;
            rend.SetPropertyBlock(null);
            if (opaqueMat != null)
            {
                var mats = rend.sharedMaterials;
                bool changed = false;
                for (int m = 0; m < mats.Length; m++)
                {
                    if (mats[m] != opaqueMat)
                    {
                        mats[m] = opaqueMat;
                        changed = true;
                    }
                }
                if (changed)
                {
                    rend.sharedMaterials = mats;
                }
            }
        }
    }
}
