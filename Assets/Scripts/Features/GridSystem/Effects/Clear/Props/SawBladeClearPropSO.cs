using DG.Tweening;
using UnityEngine;

/// <summary>
/// Specialized ScriptableObject for the circular Saw Blade line sweeper prop.
/// Orchestrates a 3-phase high-speed cutting choreography: Slide in -> Linear cutting sweep -> Fly out exit.
/// Adheres strictly to dotween.md (Stateless SO) and visual_effects.md.
/// </summary>
[CreateAssetMenu(fileName = "NewSawBladeClearProp", menuName = "Block Blast/Effects/Clear Prop/Saw Blade Prop")]
public class SawBladeClearPropSO : ClearPropBase
{
    [Header("Saw Blade Rotation Settings")]
    [Tooltip("Rotations per minute for the circular saw blade disc.")]
    public float rpm = 1200f;

    [Tooltip("Local axis around which the blade disc spins.")]
    public Vector3 rotationAxis = Vector3.up;

    [Header("Saw Blade Cutting Shake")]
    [Tooltip("Shake amplitude for the blade visual during wood cutting.")]
    public float shakeStrength = 0.035f;

    [Tooltip("Shake vibrato frequency for high-speed mechanical jitter.")]
    public int shakeVibrato = 40;

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
        Vector3 cutStartPos = startCellPos - dir * 0.5f + forwardVec + Vector3.up * heightOffset;
        Vector3 spawnPos = cutStartPos - dir * spawnDistance + Vector3.up * dropHeight;
        Vector3 cutEndPos = endCellPos + dir * 0.5f + forwardVec + Vector3.up * heightOffset;
        Vector3 exitPos = cutEndPos + dir * 1.0f + Vector3.up * heightOffset;

        Quaternion propRot = Mathf.Abs(dir.z) > Mathf.Abs(dir.x)
            ? Quaternion.Euler(90f, 90f, 0f)
            : Quaternion.Euler(90f, 0f, 0f);

        prop.transform.position = spawnPos;
        prop.transform.rotation = propRot;

        Transform bladeVisual = prop.transform.Find("VisualBlade") ?? prop.transform.Find("Blade") ?? prop.transform.Find("Visual");
        if (bladeVisual == null && prop.transform.childCount > 0)
        {
            bladeVisual = prop.transform.GetChild(0);
        }
        if (bladeVisual == null)
        {
            bladeVisual = prop.transform;
        }

        bladeVisual.localPosition = Vector3.zero;
        bladeVisual.localRotation = Quaternion.identity;
        PreparePropForEntry(prop);

        Sequence seq = DOTween.Sequence();
        seq.SetTarget(prop);
        seq.SetLink(prop, LinkBehaviour.KillOnDisable);

        if (leadOffset > 0f)
        {
            seq.AppendInterval(leadOffset);
        }

        // Phase 1: Slide in from sky to cut start & Fade in & Spin
        seq.AppendCallback(PlayEntrySound);
        seq.Append(prop.transform.DOMove(cutStartPos, entryDuration).SetEase(Ease.OutQuad));
        ApplyFadeIn(seq, prop, entryDuration);
        if (rpm > 0f && bladeVisual != null)
        {
            float rot1 = 360f * (rpm / 60f) * entryDuration;
            seq.Join(bladeVisual.DOLocalRotate(rotationAxis.normalized * rot1, entryDuration, RotateMode.LocalAxisAdd).SetEase(Ease.Linear));
        }

        // Phase 2: Linear sweep across the line with continuous spin, sparks and mechanical micro-shake
        seq.AppendCallback(() =>
        {
            SetPropParticles(prop, true, dir);
            PlaySweepSound();
        });
        seq.Append(prop.transform.DOMove(cutEndPos, sweepDuration).SetEase(Ease.Linear));
        if (rpm > 0f && bladeVisual != null)
        {
            float rot2 = 360f * (rpm / 60f) * sweepDuration;
            seq.Join(bladeVisual.DOLocalRotate(rotationAxis.normalized * rot2, sweepDuration, RotateMode.LocalAxisAdd).SetEase(Ease.Linear));
        }
        if (bladeVisual != null && bladeVisual != prop.transform)
        {
            seq.Join(bladeVisual.DOShakePosition(sweepDuration, shakeStrength, shakeVibrato, 90f, false, false));
        }

        // Phase 3: Exit & Fade out & Spin
        seq.AppendCallback(() =>
        {
            SetPropParticles(prop, false);
            PlayExitSound();
        });
        seq.Append(prop.transform.DOMove(exitPos, exitDuration).SetEase(Ease.InQuad));
        ApplyFadeOut(seq, prop, exitDuration);
        if (rpm > 0f && bladeVisual != null)
        {
            float rot3 = 360f * (rpm / 60f) * exitDuration;
            seq.Join(bladeVisual.DOLocalRotate(rotationAxis.normalized * rot3, exitDuration, RotateMode.LocalAxisAdd).SetEase(Ease.Linear));
        }

        seq.OnComplete(() =>
        {
            ResetPropVisualState(prop);
            if (bladeVisual != null && bladeVisual != prop.transform)
            {
                bladeVisual.localPosition = Vector3.zero;
                bladeVisual.localRotation = Quaternion.identity;
            }
            poolService?.ReturnObjectToPool(prop);
        });

        return seq;
    }
}
