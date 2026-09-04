using UnityEngine;

public class State<T>
{
    protected T core;
    protected StateMachine<T> stateMachine;

    public State(T core, StateMachine<T> stateMachine)
    {
        this.core = core;
        this.stateMachine = stateMachine;
    }

    public virtual void EnterState()
    {
    }

    public virtual void ExitState()
    {
    }

    public virtual void FrameUpdate()
    {
    }

    public virtual void PhysicUpdate()
    {
    }

    public virtual void AnimationTriggerEvent()
    {
    }
}
