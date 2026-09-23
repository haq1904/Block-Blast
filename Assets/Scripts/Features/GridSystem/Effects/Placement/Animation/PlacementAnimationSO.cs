using UnityEngine;

public abstract class PlacementAnimationSO : ScriptableObject
{
    public abstract void Apply(Transform target);
}
