
using UnityEngine;
using UnityEngine.Events;

public class BaseGameEventListenerGeneric<T> : MonoBehaviour
{
    [SerializeField] private BaseGameEventGeneric<T> Event;
    [SerializeField] private UnityEvent<T> Respond;

    private void OnEnable() => Event.RegisterListener(this);
    private void OnDisable() => Event.UnregisterListener(this);

    public void OnEventRaise(T data) => Respond.Invoke(data);
}
