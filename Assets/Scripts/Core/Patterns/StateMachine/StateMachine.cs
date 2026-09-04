using UnityEngine;

public class StateMachine<T>
{
    public State<T> CurrentState { get; private set; }

    public void Initialize(State<T> startingState)
    {
        CurrentState = startingState;
        CurrentState.EnterState();
    }

    public void ChangeState(State<T> newState)
    {
        if (CurrentState != null)
            CurrentState.ExitState();
            
        CurrentState = newState;
        CurrentState.EnterState();
    }
}
