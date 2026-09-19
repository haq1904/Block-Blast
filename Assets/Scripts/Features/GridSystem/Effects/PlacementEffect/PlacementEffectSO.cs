using UnityEngine;

public abstract class PlacementEffectSO : ScriptableObject
{
    public abstract void Apply(Transform target);
}
