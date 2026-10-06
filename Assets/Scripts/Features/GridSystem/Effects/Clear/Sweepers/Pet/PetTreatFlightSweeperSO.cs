using DG.Tweening;
using UnityEngine;

/// <summary>
/// Motion-centric sweeper for "Magic Pet Treat Flight & Wobble" projectile choreography.
/// Orchestrates: Smooth gliding flight with floating Y-bobbing, slight orbit tilt, and magical slow yaw.
/// Guarantees that X/Z contact timing remains 100% linear and synchronized with cell destruction rhythms,
/// while subtle floating bobbing provides whimsical treat floating juice.
/// Adheres strictly to dotween.md (Stateless SO), visual_effects.md, and architecture.md.
/// </summary>
[CreateAssetMenu(fileName = "NewPetTreatFlightSweeper", menuName = "Block Blast/Effects/Clear Sweeper/Pet Treat Flight Sweeper")]
public class PetTreatFlightSweeperSO : ClearSweeperBase
{
    [Header("Treat Flight Dynamics")]
    [Range(0.02f, 0.25f)]
    [Tooltip("Vertical bobbing amplitude (floating food motion).")]
    public float bobAmplitude = 0.10f;

    [Min(0.5f)]
    [Tooltip("Number of full vertical bobbing cycles across the sweep.")]
    public float bobCycles = 1.5f;

    [Range(0f, 30f)]
    [Tooltip("Maximum wobble/orbit tilt angle in degrees.")]
    public float orbitAngle = 8f;

    [Tooltip("Oscillation frequency for orbit/wobble.")]
    public float orbitFrequency = 1.2f;

    [Tooltip("Slow yaw rotation per minute around local Y axis.")]
    public float slowYawRpm = 60f;

    [Header("Optional Treat VFX Prefabs")]
    [Tooltip("Optional pooled particle prefab for crumb/magic trail.")]
    public GameObject trailPrefab;

    [Tooltip("Optional pooled particle prefab spawned on each cell contact.")]
    public GameObject contactBurstPrefab;

    [Header("Viewport Exit Settings")]
    [Tooltip("If true, computes exit destination outside the camera's viewport frustum.")]
    public bool exitOutsideViewport = true;

    [Range(0f, 0.25f)]
    [Tooltip("Viewport margin padding beyond [0, 1] screen bounds (e.g. 0.1 = 10% outside screen).")]
    public float exitViewportPadding = 0.1f;

    [Min(0.5f)]
    [Tooltip("Fallback distance in world units past the line if camera or viewport projection is unavailable.")]
    public float fallbackExitDistance = 1.5f;

    public override Vector3 ResolveVisualExitPosition(ClearTimelineContext timeline)
    {
        return ComputeViewportExitPosition(
            timeline,
            forwardOffset,
            heightOffset,
            exitOutsideViewport,
            exitViewportPadding,
            fallbackExitDistance);
    }

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
        Vector3 cutStartPos = startCellPos - dir * 0.5f + forwardVec + Vector3.up * heightOffset;
        Vector3 spawnPos = cutStartPos - dir * spawnDistance + Vector3.up * dropHeight;
        Vector3 cutEndPos = endCellPos + dir * 0.5f + forwardVec + Vector3.up * heightOffset;
        Vector3 exitPos = timeline.visualExitPosition != Vector3.zero
            ? timeline.visualExitPosition
            : ResolveVisualExitPosition(timeline);

        Quaternion flightRotation = dir.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(dir, Vector3.up)
            : Quaternion.identity;

        sweeper.transform.position = spawnPos;
        sweeper.transform.rotation = flightRotation;

        Transform visual = sweeper.transform.Find("Visual") ?? sweeper.transform.Find("VisualBlade");
        if (visual == null && sweeper.transform.childCount > 0)
        {
            visual = sweeper.transform.GetChild(0);
        }
        if (visual == null)
        {
            visual = sweeper.transform;
        }

        Vector3 restLocalPos = visual.localPosition;
        Quaternion restLocalRot = visual.localRotation;
        PrepareSweeperForEntry(sweeper);

        Sequence seq = DOTween.Sequence();
        seq.SetTarget(sweeper);
        seq.SetLink(sweeper, LinkBehaviour.KillOnDisable);

        if (leadOffset > 0f)
        {
            seq.AppendInterval(leadOffset);
        }

        // Phase 1: Slide in from sky to cut start with fade-in and tilt
        seq.AppendCallback(PlayEntrySound);
        seq.Append(sweeper.transform.DOMove(cutStartPos, entryDuration).SetEase(Ease.OutQuad));
        ApplyFadeIn(seq, sweeper, entryDuration);

        // Phase 2: Linear sweep with bobbing, subtle orbit tilt, and slow yaw
        seq.AppendCallback(() =>
        {
            SetSweeperParticles(sweeper, true, dir);
            PlaySweepSound();
        });

        // 100% linear root movement along X/Z guarantees contact timing is preserved
        seq.Append(sweeper.transform.DOMove(cutEndPos, sweepDuration).SetEase(Ease.Linear));

        if (visual != null && visual != sweeper.transform)
        {
            // Floating vertical bobbing
            if (bobAmplitude > 0.001f && bobCycles > 0.1f)
            {
                int cycles = Mathf.Max(1, Mathf.RoundToInt(bobCycles));
                float halfCycle = (sweepDuration / cycles) * 0.5f;

                Sequence bobSeq = DOTween.Sequence();
                bobSeq.SetTarget(sweeper);
                bobSeq.SetLink(sweeper.gameObject, LinkBehaviour.KillOnDisable);
                for (int c = 0; c < cycles; c++)
                {
                    bobSeq.Append(visual.DOLocalMoveY(restLocalPos.y + bobAmplitude, halfCycle).SetEase(Ease.InOutSine));
                    bobSeq.Append(visual.DOLocalMoveY(restLocalPos.y, halfCycle).SetEase(Ease.InOutSine));
                }
                seq.Join(bobSeq);
            }

            // Whimsical orbit tilt
            if (orbitAngle > 0.1f)
            {
                int tilts = Mathf.Max(1, Mathf.RoundToInt(orbitFrequency * sweepDuration * 2f));
                float halfTilt = sweepDuration / (tilts * 2f);

                Sequence tiltSeq = DOTween.Sequence();
                tiltSeq.SetTarget(sweeper);
                tiltSeq.SetLink(sweeper.gameObject, LinkBehaviour.KillOnDisable);
                for (int t = 0; t < tilts; t++)
                {
                    tiltSeq.Append(visual.DOLocalRotate(new Vector3(orbitAngle, 0f, 0f), halfTilt, RotateMode.LocalAxisAdd).SetEase(Ease.InOutSine));
                    tiltSeq.Append(visual.DOLocalRotate(new Vector3(-orbitAngle, 0f, 0f), halfTilt, RotateMode.LocalAxisAdd).SetEase(Ease.InOutSine));
                }
                seq.Join(tiltSeq);
            }

            // Slow magical yaw
            if (slowYawRpm > 0f)
            {
                float totalYaw = 360f * (slowYawRpm / 60f) * sweepDuration;
                seq.Join(visual.DOLocalRotate(new Vector3(0f, totalYaw, 0f), sweepDuration, RotateMode.LocalAxisAdd).SetEase(Ease.Linear));
            }
        }

        // Optional contact burst particles dispatched at waypoints
        if (contactBurstPrefab != null && timeline.waypoints != null && poolService != null)
        {
            for (int w = 0; w < timeline.waypoints.Count; w++)
            {
                var wp = timeline.waypoints[w];
                float contactTime = leadOffset + timeline.firstBlockDelay + wp.hitTime;
                Vector3 burstPos = wp.worldPos;
                seq.InsertCallback(contactTime, () =>
                {
                    poolService.SpawnObject(
                        contactBurstPrefab,
                        burstPos,
                        Quaternion.identity,
                        PoolType.ParticleSystem);
                });
            }
        }

        // Phase 3: Exit & Fade out
        seq.AppendCallback(() =>
        {
            SetSweeperParticles(sweeper, false);
            PlayExitSound();
        });
        seq.Append(sweeper.transform.DOMove(exitPos, exitDuration).SetEase(Ease.InQuad));
        ApplyFadeOut(seq, sweeper, exitDuration);

        seq.OnComplete(() =>
        {
            ResetSweeperVisualState(sweeper);
            if (visual != null && visual != sweeper.transform)
            {
                visual.localPosition = restLocalPos;
                visual.localRotation = restLocalRot;
            }
            poolService?.ReturnObjectToPool(sweeper, PoolType.GameObject);
        });

        return seq;
    }
}