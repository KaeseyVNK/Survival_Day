using UnityEngine;

public abstract class EntityState 
{
    protected StateMachine stateMachine;
    protected string animBoolName;
    protected Rigidbody2D rb;
    protected Animator anim;



    protected float stateTimer;
    protected bool triggerCalled;

    protected virtual void Awake()
    {
        
    }

    public EntityState(StateMachine stateMachine, string animBoolName)
    {
        this.stateMachine = stateMachine;
        this.animBoolName = animBoolName;
    }

    public virtual void Enter()
    {
        anim.SetBool(animBoolName, true);
        triggerCalled = false;
    }

    public virtual void Update()
    {
        stateTimer -= Time.deltaTime;
        UpdateAnimationParameters();
    }

    public virtual void UpdateAnimationParameters()
    {

    }

    public virtual void Exit()
    {
        anim.SetBool(animBoolName, false);
    }

    public virtual void AnimationTrigger()
    {
        triggerCalled = true;
    }
}
