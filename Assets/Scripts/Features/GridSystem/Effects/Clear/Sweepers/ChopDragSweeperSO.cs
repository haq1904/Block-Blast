using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Motion-centric sweeper for "Chop & Serpentine Drag" choreography.
/// Orchestrates: Slide in -> Windup anticipation around X -> Chop down impact -> Serpentine drag sweep with debris trail -> Exit.
/// Highly decoupled: accommodates axes, pickaxes, shovels, monster claws, or heavy anchors.
/// Adheres strictly to dotween.md (Stateless SO), visual_effects.md, and architecture.md.
/// </summary>
[CreateAssetMenu(fileName = "NewChopDragSweeper", menuName = "Block Blast/Effects/Clear Sweeper/Chop & Drag Sweeper")]
public class ChopDragSweeperSO : ClearSweeperBase
{
    [Header("Sweeper Orientation Offsets")]
    [Tooltip("Base Euler offset applied to align the visual mesh forward along the cutting direction.")]
    public Vector3 baseRotationOffset = Vector3.zero;

    [Header("Windup & Strike Settings")]
    [Tooltip("Angle in degrees to tilt/raise the tool head around X during windup anticipation.")]
    public float windupAngleX = -20f;

    [Tooltip("Duration in seconds for the sweeper to wind up / raise head before striking.")]
    [Range(0.04f, 0.4f)]
    public float windupDuration = 0.1f;

    [Tooltip("Easing curve for windup anticipation.")]
    public Ease windupEase = Ease.OutQuad;

    [Header("Chop & Drag Impact Settings")]
    [Tooltip("Target tilt angle around X when the sweeper strikes down and drags across the line (e.g. 120 degrees).")]
    public float dragTiltAngleX = 120f;

    [Tooltip("Duration in seconds for the chop down impact onto the board.")]
    [Range(0.04f, 1f)]
    public float chopDuration = 0.226f;

    [Tooltip("Easing curve for chop impact.")]
    public Ease chopEase = Ease.InQuad;

    [Tooltip("Sound played at the moment the tool strikes down into the board surface.")]
    public AudioClip chopSound;

    [Tooltip("Hover height above the cutting plane during entry and windup.")]
    public float hoverHeight = 0.8f;

    [Header("Drag Cutting Shake")]
    [Tooltip("Shake amplitude for the sweeper visual while dragging across blocks.")]
    public float shakeStrength = 0.06f;

    [Tooltip("Shake vibrato frequency for cutting friction.")]
    public int shakeVibrato = 35;

    [Header("Serpentine Drag (Random Range Per Sweeper)")]
    [Tooltip("Lateral sway amplitude range (X=Min, Y=Max in Unity units). Max <= 0.45 so total swing is <= 1.0 unit.")]
    public Vector2 swayAmplitudeRange = new Vector2(0.1f, 0.2f);

    [Tooltip("Duration in seconds for one full sway cycle (X=Min, Y=Max). Independent of sweep duration.")]
    public Vector2 swayCycleDurationRange = new Vector2(1.0f, 2.0f);

    [Tooltip("Dynamic banking yaw angle range in degrees (X=Min, Y=Max).")]
    public Vector2 swayBankYawRange = new Vector2(10f, 20f);

    [Tooltip("Dynamic banking roll angle range in degrees (X=Min, Y=Max).")]
    public Vector2 swayBankRollRange = new Vector2(10f, 20f);

    [Header("Trail Debris (Odometry Spawning)")]
    [Tooltip("Low-poly trail debris prefabs (dirt furrows, stone chips, sparks) spawned along the drag trail.")]
    [FormerlySerializedAs("dirtFurrowPrefabs")]
    public GameObject[] trailDebrisPrefabs;

    [Tooltip("Distance step in meters between spawned debris pieces along the contact point trajectory.")]
    [Range(0.1f, 0.8f)]
    [FormerlySerializedAs("dirtSpawnInterval")]
    public float debrisSpawnInterval = 0.21f;

    [Tooltip("Duration in seconds for the debris to linger on the board before sinking/fading.")]
    [Range(0.05f, 1.5f)]
    [FormerlySerializedAs("dirtLingerDuration")]
    public float debrisLingerDuration = 0.15f;

    [Tooltip("Duration in seconds for the debris to pop up from scale 0 to 1.")]
    [Range(0.02f, 0.3f)]
    [FormerlySerializedAs("dirtPopDuration")]
    public float debrisPopDuration = 0.1255f;

    [Tooltip("Duration in seconds for the debris to sink/shrink into the floor before returning to pool.")]
    [Range(0.05f, 0.4f)]
    [FormerlySerializedAs("dirtSinkDuration")]
    public float debrisSinkDuration = 0.2f;

    [Tooltip("Local offset from the visual origin to the cutting contact point touching the board.")]
    [FormerlySerializedAs("bladeTipOffset")]
    public Vector3 contactPointOffset = new Vector3(0f, 2.4f, 0.7f);

    public override float TotalPreSweepDuration => entryDuration + windupDuration + chopDuration;

    public override Sequence AnimateSweeperSequence(
        GameObject sweeper,
        ClearTimelineContext timeline,
        IPoolService poolService)
    {
        if (sweeper == null || timeline == null) return null;

        Vector3 startCellPos = timeline.startPos;
        Vector3 endCellPos = timeline.endPos;
        Vector3 dir = timeline.direction;
        float sweepDuration = timeline.totalSweepDuration;
        float leadOffset = timeline.leadOffset;

        Vector3 forwardVec = dir * forwardOffset;
        Vector3 chopPos = startCellPos - dir * 0.5f + forwardVec + Vector3.up * heightOffset;
        Vector3 hoverStartPos = chopPos + Vector3.up * hoverHeight;
        Vector3 spawnPos = hoverStartPos - dir * spawnDistance + Vector3.up * dropHeight;
        Vector3 cutEndPos = endCellPos + dir * 0.5f + forwardVec + Vector3.up * heightOffset;
        Vector3 exitPos = cutEndPos + dir * 1.0f + Vector3.up * hoverHeight;

        // Face opposite to direction of movement (-dir) so the cutting edge faces backwards while dragging
        Quaternion baseRot = (dir.sqrMagnitude > 0.001f ? Quaternion.LookRotation(-dir, Vector3.up) : Quaternion.identity)
                             * Quaternion.Euler(baseRotationOffset);
        Quaternion windupRot = baseRot * Quaternion.Euler(windupAngleX, 0f, 0f);
        Quaternion dragRot = baseRot * Quaternion.Euler(dragTiltAngleX, 0f, 0f);

        sweeper.transform.position = spawnPos;
        sweeper.transform.rotation = baseRot;

        Transform visual = sweeper.transform.Find("VisualAxe") ?? sweeper.transform.Find("Visual");
        if (visual == null && sweeper.transform.childCount > 0)
        {
            visual = sweeper.transform.GetChild(0);
        }
        if (visual == null)
        {
            visual = sweeper.transform;
        }

        visual.localPosition = Vector3.zero;
        visual.localRotation = Quaternion.identity;
        PrepareSweeperForEntry(sweeper);

        Sequence seq = DOTween.Sequence();
        seq.SetTarget(sweeper);
        seq.SetLink(sweeper, LinkBehaviour.KillOnDisable);

        if (leadOffset > 0f)
        {
            seq.AppendInterval(leadOffset);
        }

        // Phase 1: Slide in from sky to hover position & fade in
        seq.AppendCallback(PlayEntrySound);
        seq.Append(sweeper.transform.DOMove(hoverStartPos, entryDuration).SetEase(Ease.OutQuad));
        ApplyFadeIn(seq, sweeper, entryDuration);

        // Phase 2: Windup - tilt tool head up around X + slight anticipation lift
        seq.Append(sweeper.transform.DORotateQuaternion(windupRot, windupDuration).SetEase(windupEase));
        seq.Join(sweeper.transform.DOMove(hoverStartPos + Vector3.up * 0.25f, windupDuration).SetEase(windupEase));

        // Phase 3: Chop down onto first cell directly into drag posture (dragTiltAngleX)
        seq.Append(sweeper.transform.DORotateQuaternion(dragRot, chopDuration).SetEase(chopEase));
        seq.Join(sweeper.transform.DOMove(chopPos, chopDuration).SetEase(chopEase));
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
            SetSweeperParticles(sweeper, true, dir);
            PlaySweepSound();
        });
        seq.Append(sweeper.transform.DOMove(cutEndPos, sweepDuration).SetEase(Ease.Linear));

        // Sample random serpentine drag parameters for this specific sweeper instance
        float sampledAmplitude = Random.Range(swayAmplitudeRange.x, swayAmplitudeRange.y);
        float sampledCycleDuration = Random.Range(swayCycleDurationRange.x, swayCycleDurationRange.y);
        float sampledBankYaw = Random.Range(swayBankYawRange.x, swayBankYawRange.y);
        float sampledBankRoll = Random.Range(swayBankRollRange.x, swayBankRollRange.y);
        float initialSwayDirection = Random.value < 0.5f ? -1f : 1f;

        Vector3 lastSpawnPos = Vector3.zero;
        bool hasLastSpawnPos = false;
        float groundY = startCellPos.y;
        bool hasDebris = trailDebrisPrefabs != null && trailDebrisPrefabs.Length > 0 && poolService != null;

        bool hasSway = sampledAmplitude > 0.001f && sampledCycleDuration > 0.01f;
        bool hasShake = shakeStrength > 0.0001f && shakeVibrato > 0;

        if (visual != null && (hasSway || hasShake || hasDebris))
        {
            // Continuous sinusoidal serpentine sway, micro-shake and contact point odometry
            seq.Join(DOTween.To(() => 0f, elapsed =>
            {
                if (visual == null) return;

                float progress = sweepDuration > 0.001f ? Mathf.Clamp01(elapsed / sweepDuration) : 0f;
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

                if (visual != sweeper.transform)
                {
                    visual.localPosition = new Vector3(currentSwayX, 0f, 0f);
                    visual.localRotation = Quaternion.Euler(0f, bankFactor * sampledBankYaw + shakeYaw, bankFactor * sampledBankRoll + shakeRoll);
                }

                // Contact-Point Odometry: Spawn trail debris along actual 3D contact path
                if (hasDebris)
                {
                    Vector3 currentTipPos = visual.TransformPoint(contactPointOffset);
                    currentTipPos.y = groundY;

                    if (!hasLastSpawnPos)
                    {
                        lastSpawnPos = currentTipPos;
                        hasLastSpawnPos = true;
                        SpawnDebrisMound(currentTipPos, dir, poolService);
                    }
                    else
                    {
                        Vector3 delta = currentTipPos - lastSpawnPos;
                        delta.y = 0f;
                        if (delta.sqrMagnitude >= debrisSpawnInterval * debrisSpawnInterval)
                        {
                            Vector3 moveDir = delta.sqrMagnitude > 0.0001f ? delta.normalized : dir;
                            SpawnDebrisMound(currentTipPos, moveDir, poolService);
                            lastSpawnPos = currentTipPos;
                        }
                    }
                }
            }, sweepDuration, sweepDuration).SetEase(Ease.Linear));
        }

        // Phase 5: Lift exit & fade out
        seq.AppendCallback(() =>
        {
            SetSweeperParticles(sweeper, false);
            PlayExitSound();
        });
        seq.Append(sweeper.transform.DOMove(exitPos, exitDuration).SetEase(Ease.InQuad));
        seq.Join(sweeper.transform.DORotateQuaternion(baseRot, exitDuration).SetEase(Ease.InQuad));
        ApplyFadeOut(seq, sweeper, exitDuration);

        seq.OnComplete(() =>
        {
            ResetSweeperVisualState(sweeper);
            if (visual != null && visual != sweeper.transform)
            {
                visual.localPosition = Vector3.zero;
                visual.localRotation = Quaternion.identity;
            }
            poolService?.ReturnObjectToPool(sweeper, PoolType.GameObject);
        });

        return seq;
    }

    /// <summary>
    /// Spawns an organic debris mound from pool with juicy pop-up and sink sequence.
    /// Strictly adheres to dotween.md and visual_effects.md.
    /// </summary>
    private void SpawnDebrisMound(Vector3 position, Vector3 moveDir, IPoolService poolService)
    {
        if (trailDebrisPrefabs == null || trailDebrisPrefabs.Length == 0 || poolService == null) return;

        int randomIndex = Random.Range(0, trailDebrisPrefabs.Length);
        GameObject prefab = trailDebrisPrefabs[randomIndex];
        if (prefab == null) return;

        // Random yaw jitter +- 15 degrees for organic variation
        float randomYaw = Random.Range(-15f, 15f);
        Quaternion baseRot = moveDir.sqrMagnitude > 0.001f ? Quaternion.LookRotation(moveDir, Vector3.up) : Quaternion.identity;
        Quaternion spawnRot = baseRot * Quaternion.Euler(0f, randomYaw, 0f);

        GameObject debrisObj = poolService.SpawnObject(prefab, position, spawnRot, PoolType.GameObject);
        if (debrisObj == null) return;

        debrisObj.transform.localScale = Vector3.zero;
        debrisObj.transform.DOKill();

        Sequence debrisSeq = DOTween.Sequence();
        debrisSeq.SetTarget(debrisObj);
        debrisSeq.SetLink(debrisObj, LinkBehaviour.KillOnDisable);

        // Phase 1: Pop up with juicy bounce
        debrisSeq.Append(debrisObj.transform.DOScale(Vector3.one, debrisPopDuration).SetEase(Ease.OutBack));
        // Phase 2: Linger on the board surface
        debrisSeq.AppendInterval(debrisLingerDuration);
        // Phase 3: Sink / shrink into the ground
        debrisSeq.Append(debrisObj.transform.DOScale(Vector3.zero, debrisSinkDuration).SetEase(Ease.InQuad));

        debrisSeq.OnComplete(() =>
        {
            debrisObj.transform.localScale = Vector3.one;
            debrisObj.transform.rotation = Quaternion.identity;
            poolService.ReturnObjectToPool(debrisObj, PoolType.GameObject);
        });
    }
}
