using DG.Tweening;
using UnityEngine;

public abstract class PreClearEffectSO : ScriptableObject
{
    public abstract void Apply(Transform target);

    public virtual void Cancel(Transform target, Vector3 originalPos, Quaternion originalRot)
    {
        if (target == null) return;
        target.DOKill();
        target.position = originalPos;
        target.rotation = originalRot;
    }
}

