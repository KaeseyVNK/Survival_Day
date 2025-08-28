using UnityEngine;

public class Player_Pickaxe : PlayerState
{
    public Player_Pickaxe(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {

    }

    public override void Enter()
    {
        base.Enter();
        player.SetVelocity(0, 0);
        stateTimer = 0.8f;
    }

    public override void Update()
    {
        base.Update();

        if (triggerCalled)
        {
            stateMachine.ChangeState(player.idleState);
        }
        else if (stateTimer < 0)
        {
            stateMachine.ChangeState(player.idleState);
        }
    }

    public override void Exit(){
        base.Exit();
    }
}
