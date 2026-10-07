using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Natural pet fetch clear animation synchronized with flying projectiles (Frisbee, bones, balls).
/// Features: Post-pass trigger -> In-place alert shake -> Reaction hop & landing -> Curved takeoff flight with early gameplay release ->
/// Real-time live prop facing with ground-aligned feet during follow -> Natural offscreen shrink to zero (no alpha fade or material swap).
/// Adheres strictly to dotween.md, visual_effects.md, and architecture.md.
/// </summary>
[CreateAssetMenu(fileName = "PetFetch_ClearAnimation", menuName = "Block Blast/Effects/Grid/Clear/Pet/Fetch")]
public class PetFetchClearAnimationSO : PetClearAnimationSOBase
{
    [Header("Post-pass Reaction")]
    [Min(0f)]
    [Tooltip("Delay after the Frisbee center passes the cell before the pet reacts.")]
    public float reactionDelayAfterPass = 0.12f;

    [Header("Alert Shake")]
    [Range(0.01f, 0.2f)] public float shakeDuration = 0.10f;
    public Vector3 shakeStrength = new Vector3(2f, 0f, 2f);
    public int shakeVibrato = 8;
    public Ease shakeEase = Ease.OutQuad;

    [Header("Reaction Hop")]
    [Min(0.01f)] public float reactionHopUpDuration = 0.08f;
    [Min(0.01f)] public float reactionHopDownDuration = 0.10f;
    [Min(0f)] public float reactionHopHeight = 0.12f;
    public Ease reactionHopUpEase = Ease.OutQuad;
    public Ease reactionHopDownEase = Ease.InQuad;
    [Range(0.8f, 1.2f)] public float landingScaleY = 0.94f;
    [Min(0f)] public float landingSettleDuration = 0.05f;

    [Header("Facing & Ground Alignment")]
    [Tooltip("Local axis pointing out of the pet face in the pivot rest pose.")]
    public Vector3 faceLocalAxis = Vector3.up;

    [Tooltip("Local axis pointing from the pet body toward its feet.")]
    public Vector3 feetLocalAxis = Vector3.back;

    [Range(0f, 180f)]
    public float maxFaceAlignmentAngle = 90f;

    [Min(0f)]
    public float faceTurnSpeed = 360f;

    [Range(0f, 1f)]
    [Tooltip("Blends feet alignment toward world down without changing face direction.")]
    public float feetGroundAlignment = 1f;

    [Header("Takeoff & Early Gameplay Release")]
    [FormerlySerializedAs("releaseFlightDuration")]
    [Min(0.01f)] public float takeoffDuration = 0.24f;
    [Min(0f)] public float gameplayReleaseDelay = 0.06f;
    [Tooltip("Forward distance along sweep direction during initial chase to reach the release threshold.")]
    public float followForwardDistance = 0.30f;
    [Tooltip("Y height offset above board where pet triggers immediate gameplay release.")]
    public float gameplayReleaseYOffset = 0.80f;
    public Vector3 leapScale = new Vector3(0.86f, 1.18f, 0.86f);
    [Tooltip("Vertical arc curvature peak during takeoff flight.")]
    [Min(0f)] public float takeoffArcHeight = 0.22f;

    [Header("Natural Follow Timing")]
    [Min(0.01f)] public float followMoveSpeed = 7.0f;
    [Min(0.01f)] public float minFollowDuration = 0.32f;
    [Min(0.01f)] public float maxFollowDuration = 0.85f;
    [Min(0f)] public float petExitOvershoot = 0.35f;
    [Tooltip("Horizontal side offset of the follow trajectory control point.")]
    [Min(0f)] public float followSideOffset = 0.12f;
    [Tooltip("Vertical lift offset of the follow trajectory control point.")]
    [Min(0f)] public float followLiftOffset = 0.10f;
    [Tooltip("Easing function for the follow path interpolation.")]
    public Ease followEase = Ease.InOutSine;

    [Header("Offscreen Shrink Exit")]
    [Tooltip("Additional Y height offset applied during exit flight.")]
    public float exitHeightOffset = 0.20f;
    [Tooltip("Duration in seconds for scaling to zero at offscreen destination.")]
    [Min(0.01f)] public float shrinkOutDuration = 0.18f;
    [Tooltip("Easing curve for scaling to zero.")]
    public Ease shrinkOutEase = Ease.InQuad;

    public override float PreExplosionDuration =>
        shakeDuration
        + reactionHopUpDuration
        + reactionHopDownDuration
        + landingSettleDuration
        + Mathf.Clamp(gameplayReleaseDelay, 0f, takeoffDuration);

    public override float SweepContactOffset => 0f;
    public override float SweepPostContactDelay => reactionDelayAfterPass;

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

        PetAnimationRig rig = ResolveRig(target);
        Vector3 rootPos = context.canonicalPos;
        Quaternion rootRot = context.canonicalRotation;
        PrepareRig(rig, rootPos, rootRot);

        Vector3 direction = ResolveDirection(context.sweepDirection);

        Sequence seq = DOTween.Sequence();
        seq.SetTarget(target);
        seq.SetLink(target.gameObject, LinkBehaviour.KillOnDisable);

        if (context.delay > 0f) seq.AppendInterval(context.delay);

        // Phase 1: In-place alert shake
        if (shakeDuration > 0f)
        {
            seq.Append(rig.pivot
                .DOShakeRotation(shakeDuration, shakeStrength, shakeVibrato)
                .SetEase(shakeEase));
            seq.AppendCallback(() => rig.pivot.localRotation = rig.pivotRestRotation);
        }

        // Phase 2: Reaction hop and landing squash
        Vector3 hopPeak = rootPos + Vector3.up * reactionHopHeight;
        seq.Append(rig.root.DOMove(hopPeak, reactionHopUpDuration).SetEase(reactionHopUpEase));
        seq.Append(rig.root.DOMove(rootPos, reactionHopDownDuration).SetEase(reactionHopDownEase));

        if (landingSettleDuration > 0f)
        {
            Vector3 landingScale = Vector3.Scale(
                rig.pivotRestScale,
                new Vector3(1.03f, landingScaleY, 1.03f));

            seq.Append(rig.pivot.DOScale(
                landingScale,
                landingSettleDuration * 0.45f).SetEase(Ease.OutQuad));
            seq.Append(rig.pivot.DOScale(
                rig.pivotRestScale,
                landingSettleDuration * 0.55f).SetEase(Ease.OutSine));
        }

        seq.AppendCallback(() => rig.root.position = rootPos);

        // Phase 3: Curved takeoff flight & early gameplay release
        float releaseDelay = Mathf.Clamp(gameplayReleaseDelay, 0f, takeoffDuration);
        Vector3 releasePoint = rootPos + direction * followForwardDistance + Vector3.up * gameplayReleaseYOffset;
        Vector3 takeoffControl = Vector3.Lerp(rootPos, releasePoint, 0.5f) + Vector3.up * takeoffArcHeight;
        takeoffControl.y = Mathf.Min(takeoffControl.y, releasePoint.y);

        float takeoffStartTime = seq.Duration();

        seq.Append(DOTween.To(() => 0f, t =>
        {
            rig.root.position = CalculateQuadraticBezier(rootPos, takeoffControl, releasePoint, t);
            float delta = Time.deltaTime > 0f ? Time.deltaTime : 0.016f;
            rig.pivot.localRotation = StepGroundedFacingRotation(
                rig,
                context.followTarget,
                direction,
                maxFaceAlignmentAngle,
                delta);
        }, 1f, takeoffDuration).SetEase(Ease.OutQuad));

        seq.Join(rig.pivot.DOScale(
            Vector3.Scale(rig.pivotRestScale, leapScale),
            takeoffDuration).SetEase(Ease.OutQuad));

        seq.InsertCallback(takeoffStartTime + releaseDelay, () =>
        {
            SpawnVFX(target, rig.root.position, target.rotation);
            lifecycle.ReleaseGameplay();
        });

        // Phase 4: Natural follow (speed-based timing, clamped duration)
        Vector3 petExit = (context.visualExitPosition != Vector3.zero
            ? context.visualExitPosition
            : releasePoint + direction * 4f)
            + direction * petExitOvershoot
            + Vector3.up * exitHeightOffset;

        float followDistance = Vector3.Distance(releasePoint, petExit);
        float followDuration = Mathf.Clamp(
            followDistance / Mathf.Max(0.01f, followMoveSpeed),
            minFollowDuration,
            Mathf.Max(minFollowDuration, maxFollowDuration));

        float sideSign = (context.indexInLine % 2 == 0) ? 1f : -1f;
        Vector3 side = Vector3.Cross(Vector3.up, direction).normalized;
        Vector3 followControl = Vector3.Lerp(releasePoint, petExit, 0.5f)
                              + side * (followSideOffset * sideSign)
                              + Vector3.up * followLiftOffset;

        Tweener followTween = DOTween.To(() => 0f, t =>
        {
            rig.root.position = CalculateQuadraticBezier(releasePoint, followControl, petExit, t);
            float delta = Time.deltaTime > 0f ? Time.deltaTime : 0.016f;
            rig.pivot.localRotation = StepGroundedFacingRotation(
                rig,
                context.followTarget,
                direction,
                maxFaceAlignmentAngle,
                delta);
        }, 1f, followDuration).SetEase(followEase);

        seq.Append(followTween); // reaches petExit at full/rest scale
        seq.Join(rig.pivot.DOScale(rig.pivotRestScale, followDuration).SetEase(Ease.OutSine));

        // Phase 5: Offscreen shrink to zero
        seq.AppendCallback(() => rig.root.position = petExit);
        seq.Append(rig.pivot.DOScale(Vector3.zero, shrinkOutDuration).SetEase(shrinkOutEase));

        seq.OnComplete(() =>
        {
            ResetRig(rig, rootPos, rootRot);
            lifecycle.CompleteVisual();
        });
    }

    public override void Cancel(Transform target, Vector3 canonicalPos, Quaternion canonicalRot)
    {
        if (target == null) return;
        base.Cancel(target, canonicalPos, canonicalRot);
    }

    private static Vector3 CalculateQuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        float u = 1f - t;
        return (u * u * p0) + (2f * u * t * p1) + (t * t * p2);
    }

    private static Vector3 ResolveTargetFaceInRoot(
        PetAnimationRig rig,
        Transform followTarget,
        Vector3 fallbackDir)
    {
        Vector3 desiredWorld = followTarget != null && followTarget.gameObject.activeInHierarchy
            ? followTarget.position - rig.pivot.position
            : fallbackDir;

        if (desiredWorld.sqrMagnitude < 0.001f)
        {
            desiredWorld = fallbackDir.sqrMagnitude > 0.001f ? fallbackDir : Vector3.forward;
        }

        Vector3 desiredInRoot = rig.root.InverseTransformDirection(desiredWorld.normalized);
        return desiredInRoot.sqrMagnitude > 0.001f ? desiredInRoot.normalized : Vector3.forward;
    }

    private Vector3 ClampFaceDirection(
        PetAnimationRig rig,
        Vector3 targetFaceInRoot,
        float maxAngle)
    {
        Vector3 faceAxis = faceLocalAxis.sqrMagnitude > 0.001f
            ? faceLocalAxis.normalized
            : Vector3.up;

        Vector3 restFaceInRoot = (rig.pivotRestRotation * faceAxis).normalized;

        Quaternion targetSwing = Quaternion.FromToRotation(
            restFaceInRoot,
            targetFaceInRoot) * rig.pivotRestRotation;

        Quaternion clampedSwing = Quaternion.RotateTowards(
            rig.pivotRestRotation,
            targetSwing,
            Mathf.Clamp(maxAngle, 0f, 180f));

        return (clampedSwing * faceAxis).normalized;
    }

    private Quaternion BuildGroundedFacingRotation(
        PetAnimationRig rig,
        Vector3 faceInRoot)
    {
        Vector3 faceAxis = faceLocalAxis.sqrMagnitude > 0.001f
            ? faceLocalAxis.normalized
            : Vector3.up;

        Vector3 configuredFeetAxis = feetLocalAxis.sqrMagnitude > 0.001f
            ? feetLocalAxis.normalized
            : Vector3.back;

        Vector3 feetAxis = Vector3.ProjectOnPlane(configuredFeetAxis, faceAxis);
        if (feetAxis.sqrMagnitude < 0.001f)
        {
            feetAxis = Vector3.back;
        }
        feetAxis.Normalize();

        Vector3 restFaceInRoot = (rig.pivotRestRotation * faceAxis).normalized;

        Quaternion faceRotation = Quaternion.FromToRotation(
            restFaceInRoot,
            faceInRoot.normalized) * rig.pivotRestRotation;

        Vector3 actualFaceInRoot = (faceRotation * faceAxis).normalized;

        Vector3 groundDownInRoot = rig.root.InverseTransformDirection(Vector3.down).normalized;

        Vector3 desiredFeetInRoot = Vector3.ProjectOnPlane(groundDownInRoot, actualFaceInRoot);
        Vector3 currentFeetInRoot = Vector3.ProjectOnPlane(faceRotation * feetAxis, actualFaceInRoot);

        if (desiredFeetInRoot.sqrMagnitude < 0.001f || currentFeetInRoot.sqrMagnitude < 0.001f)
        {
            return faceRotation;
        }

        desiredFeetInRoot.Normalize();
        currentFeetInRoot.Normalize();

        float twistAngle = Vector3.SignedAngle(
            currentFeetInRoot,
            desiredFeetInRoot,
            actualFaceInRoot);

        float weightedTwist = twistAngle * Mathf.Clamp01(feetGroundAlignment);

        return Quaternion.AngleAxis(weightedTwist, actualFaceInRoot) * faceRotation;
    }

    private Quaternion StepGroundedFacingRotation(
        PetAnimationRig rig,
        Transform followTarget,
        Vector3 fallbackDir,
        float maxAngle,
        float deltaTime)
    {
        Vector3 faceAxis = faceLocalAxis.sqrMagnitude > 0.001f
            ? faceLocalAxis.normalized
            : Vector3.up;

        Vector3 targetFace = ResolveTargetFaceInRoot(
            rig,
            followTarget,
            fallbackDir);

        Vector3 clampedTargetFace = ClampFaceDirection(
            rig,
            targetFace,
            maxAngle);

        Vector3 currentFace = (rig.pivot.localRotation * faceAxis).normalized;

        float maxRadians = Mathf.Deg2Rad
            * Mathf.Max(0f, faceTurnSpeed)
            * Mathf.Max(0f, deltaTime);

        Vector3 nextFace = Vector3.RotateTowards(
            currentFace,
            clampedTargetFace,
            maxRadians,
            0f).normalized;

        return BuildGroundedFacingRotation(rig, nextFace);
    }
}
