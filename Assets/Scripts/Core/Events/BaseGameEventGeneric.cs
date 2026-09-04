
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "BaseGameEventGeneric", menuName = "Events/BaseGameEventGeneric")]
public class BaseGameEventGeneric<T> : ScriptableObject
{
    private List<BaseGameEventListenerGeneric<T>> listeners = new List<BaseGameEventListenerGeneric<T>>();

    public void RegisterListener(BaseGameEventListenerGeneric<T> listener) => listeners.Add(listener);

    public void UnregisterListener(BaseGameEventListenerGeneric<T> listener) => listeners.Remove(listener);

    public void Raise(T data)
    {
        for (int i = listeners.Count-1; i >=0; i--)
        {
            listeners[i].OnEventRaise(data);
        }
    }
}
