using UnityEngine;

public abstract class EntityState 
{
    protected StateMachine stateMachine;
    protected string animBoolName;
    protected Rigidbody2D rb;

    protected Animator anim;


    public EntityState(StateMachine stateMachine, string animBoolName)
    {
        this.stateMachine = stateMachine;
        this.animBoolName = animBoolName;
    }

    public virtual void Enter()
    {
        anim.SetBool(animBoolName, true);
    }

    public virtual void Update()
    {}

    public virtual void Exit()
    {
        anim.SetBool(animBoolName, false);
    }

    public virtual void AnimationTrigger()
    {}
}
