using DG.Tweening;
using UnityEngine;

/// <summary>
/// Specialized ScriptableObject for the Axe line sweeper prop.
/// Orchestrates a 5-phase choreography: Slide in -> Windup anticipation around Z -> Chop down impact -> Drag sweep -> Exit.
/// Adheres strictly to dotween.md (Stateless SO) and visual_effects.md.
/// </summary>
[CreateAssetMenu(fileName = "NewAxeClearProp", menuName = "Block Blast/Effects/Clear Prop/Axe Prop")]
public class AxeClearPropSO : ClearPropBase
{
    [Header("Axe Orientation Offsets")]
    [Tooltip("Base Euler offset applied to align the axe mesh forward along the cutting direction.")]
    public Vector3 baseRotationOffset = Vector3.zero;

    [Header("Axe Windup & Strike Settings")]
    [Tooltip("Angle in degrees to tilt/raise the axe blade around X during windup anticipation.")]
    public float windupAngleX = -55f;

    [Tooltip("Duration in seconds for the axe to wind up / raise blade before striking.")]
    [Range(0.04f, 0.4f)]
    public float windupDuration = 0.12f;

    [Tooltip("Easing curve for windup anticipation.")]
    public Ease windupEase = Ease.OutQuad;

    [Header("Chop & Drag Impact Settings")]
    [Tooltip("Target tilt angle around X when the axe chops down and drags across the line (e.g. 120 degrees).")]
    public float dragTiltAngleX = 120f;

    [Tooltip("Duration in seconds for the chop down impact onto the board.")]
    [Range(0.04f, 1f)]
    public float chopDuration = 0.08f;

    [Tooltip("Easing curve for chop impact.")]
    public Ease chopEase = Ease.InBack;

    [Tooltip("Sound played at the moment the axe blade strikes down into the wood.")]
    public AudioClip chopSound;

    [Tooltip("Hover height above the cutting plane during entry and windup.")]
    public float hoverHeight = 0.8f;

    [Header("Drag Cutting Shake")]
    [Tooltip("Shake amplitude for the axe visual while dragging across wood crates.")]
    public float shakeStrength = 0.04f;

    [Tooltip("Shake vibrato frequency for cutting friction.")]
    public int shakeVibrato = 35;

    [Header("Serpentine Drag (Random Range Per Axe)")]
    [Tooltip("Lateral sway amplitude range (X=Min, Y=Max in Unity units). Max <= 0.45 so total swing is <= 1.0 unit.")]
    public Vector2 swayAmplitudeRange = new Vector2(0.35f, 0.45f);

    [Tooltip("Duration in seconds for one full sway cycle (X=Min, Y=Max). Independent of sweep duration. Higher = slower, gentler sway.")]
    public Vector2 swayCycleDurationRange = new Vector2(1.0f, 1.5f);

    [Tooltip("Dynamic banking yaw angle range in degrees (X=Min, Y=Max).")]
    public Vector2 swayBankYawRange = new Vector2(25f, 75f);

    [Tooltip("Dynamic banking roll angle range in degrees (X=Min, Y=Max).")]
    public Vector2 swayBankRollRange = new Vector2(25f, 75f);

    public override float TotalPreSweepDuration => entryDuration + windupDuration + chopDuration;

    public override Sequence AnimatePropSequence(
        GameObject prop,
        Vector3 startCellPos,
        Vector3 endCellPos,
        Vector3 dir,
        float sweepDuration,
        float leadOffset,
        IPoolService poolService)
    {
        if (prop == null) return null;

        Vector3 forwardVec = dir * forwardOffset;
        Vector3 chopPos = startCellPos - dir * 0.5f + forwardVec + Vector3.up * heightOffset;
        Vector3 hoverStartPos = chopPos + Vector3.up * hoverHeight;
        Vector3 spawnPos = hoverStartPos - dir * spawnDistance + Vector3.up * dropHeight;
        Vector3 cutEndPos = endCellPos + dir * 0.5f + forwardVec + Vector3.up * heightOffset;
        Vector3 exitPos = cutEndPos + dir * 1.0f + Vector3.up * hoverHeight;

        // Face opposite to direction of movement (-dir) so the blade faces backwards while dragging
        Quaternion baseRot = (dir.sqrMagnitude > 0.001f ? Quaternion.LookRotation(-dir, Vector3.up) : Quaternion.identity)
                             * Quaternion.Euler(baseRotationOffset);
        Quaternion windupRot = baseRot * Quaternion.Euler(windupAngleX, 0f, 0f);
        Quaternion dragRot = baseRot * Quaternion.Euler(dragTiltAngleX, 0f, 0f);

        prop.transform.position = spawnPos;
        prop.transform.rotation = baseRot;

        Transform visual = prop.transform.Find("VisualAxe") ?? prop.transform.Find("Visual");
        if (visual == null && prop.transform.childCount > 0)
        {
            visual = prop.transform.GetChild(0);
        }
        if (visual == null)
        {
            visual = prop.transform;
        }

        visual.localPosition = Vector3.zero;
        visual.localRotation = Quaternion.identity;
        PreparePropForEntry(prop);

        Sequence seq = DOTween.Sequence();
        seq.SetTarget(prop);
        seq.SetLink(prop, LinkBehaviour.KillOnDisable);

        if (leadOffset > 0f)
        {
            seq.AppendInterval(leadOffset);
        }

        // Phase 1: Slide in from sky to hover position & fade in
        seq.AppendCallback(PlayEntrySound);
        seq.Append(prop.transform.DOMove(hoverStartPos, entryDuration).SetEase(Ease.OutQuad));
        ApplyFadeIn(seq, prop, entryDuration);

        // Phase 2: Windup - tilt blade up around X + slight anticipation lift
        seq.Append(prop.transform.DORotateQuaternion(windupRot, windupDuration).SetEase(windupEase));
        seq.Join(prop.transform.DOMove(hoverStartPos + Vector3.up * 0.25f, windupDuration).SetEase(windupEase));

        // Phase 3: Chop down onto first cell directly into drag posture (dragTiltAngleX)
        seq.Append(prop.transform.DORotateQuaternion(dragRot, chopDuration).SetEase(chopEase));
        seq.Join(prop.transform.DOMove(chopPos, chopDuration).SetEase(chopEase));
        seq.AppendCallback(() =>
        {
            if (chopSound != null)
            {
                PlaySound(chopSound);
            }
        });

        // Phase 4: Drag sweep across line with particle feedback, serpentine sway & friction micro-shake
        seq.AppendCallback(() =>
        {
            SetPropParticles(prop, true, dir);
            PlaySweepSound();
        });
        seq.Append(prop.transform.DOMove(cutEndPos, sweepDuration).SetEase(Ease.Linear));

        // Sample random serpentine drag parameters for this specific axe instance
        float sampledAmplitude = UnityEngine.Random.Range(swayAmplitudeRange.x, swayAmplitudeRange.y);
        float sampledCycleDuration = UnityEngine.Random.Range(swayCycleDurationRange.x, swayCycleDurationRange.y);
        float sampledBankYaw = UnityEngine.Random.Range(swayBankYawRange.x, swayBankYawRange.y);
        float sampledBankRoll = UnityEngine.Random.Range(swayBankRollRange.x, swayBankRollRange.y);
        float initialSwayDirection = UnityEngine.Random.value < 0.5f ? -1f : 1f;

        if (visual != null && visual != prop.transform)
        {
            bool hasSway = sampledAmplitude > 0.001f && sampledCycleDuration > 0.01f;
            bool hasShake = shakeStrength > 0.0001f && shakeVibrato > 0;

            if (hasSway || hasShake)
            {
                // Continuous sinusoidal serpentine sway & micro-shake independent of sweepDuration
                seq.Join(DOTween.To(() => 0f, elapsed =>
                {
                    if (visual == null) return;

                    float progress = sweepDuration > 0.001f ? Mathf.Clamp01(elapsed / sweepDuration) : 0f;
                    // Sin envelope: 0 at start, 1 in middle, 0 at end -> smooth return to center
                    float envelope = Mathf.Sin(progress * Mathf.PI);

                    float currentSwayX = 0f;
                    float bankFactor = 0f;

                    if (hasSway)
                    {
                        float angle = (elapsed / sampledCycleDuration) * Mathf.PI * 2f;
                        currentSwayX = Mathf.Sin(angle) * (sampledAmplitude * initialSwayDirection) * envelope;
                        bankFactor = Mathf.Cos(angle) * initialSwayDirection * envelope;
                    }

                    float shakeYaw = 0f;
                    float shakeRoll = 0f;
                    if (hasShake)
                    {
                        float noiseY = (Mathf.PerlinNoise(elapsed * shakeVibrato, 0.2f) - 0.5f) * 2f;
                        float noiseZ = (Mathf.PerlinNoise(0.8f, elapsed * shakeVibrato) - 0.5f) * 2f;
                        shakeYaw = noiseY * shakeStrength * 100f;
                        shakeRoll = noiseZ * shakeStrength * 100f;
                    }

                    visual.localPosition = new Vector3(currentSwayX, 0f, 0f);
                    visual.localRotation = Quaternion.Euler(0f, bankFactor * sampledBankYaw + shakeYaw, bankFactor * sampledBankRoll + shakeRoll);
                }, sweepDuration, sweepDuration).SetEase(Ease.Linear));
            }
        }

        // Phase 5: Lift exit & fade out
        seq.AppendCallback(() =>
        {
            SetPropParticles(prop, false);
            PlayExitSound();
        });
        seq.Append(prop.transform.DOMove(exitPos, exitDuration).SetEase(Ease.InQuad));
        seq.Join(prop.transform.DORotateQuaternion(baseRot, exitDuration).SetEase(Ease.InQuad));
        ApplyFadeOut(seq, prop, exitDuration);

        seq.OnComplete(() =>
        {
            ResetPropVisualState(prop);
            if (visual != null && visual != prop.transform)
            {
                visual.localPosition = Vector3.zero;
                visual.localRotation = Quaternion.identity;
            }
            poolService?.ReturnObjectToPool(prop);
        });

        return seq;
    }
}
