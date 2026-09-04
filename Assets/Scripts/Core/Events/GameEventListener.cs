
using UnityEngine;
using UnityEngine.Events;

public class GameEventListener : MonoBehaviour
{
    public GameEvent Event;

    public UnityEvent Respond;

    public void OnEnable() => Event.RegisterListener(this);

    public void OnDisable() => Event.UnregisterListener(this);

    public void OnEventRaise() => Respond?.Invoke();
}
