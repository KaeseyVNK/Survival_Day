using UnityEngine;

public class PLayer_IdleState : PlayerState
{
    public PLayer_IdleState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {

    }

    public override void Enter()
    {
        base.Enter();
        player.anim.SetBool("run", false);
        player.SetVelocity(0, 0);
    }

    public override void Update()
    {
        base.Update();

        player.anim.SetFloat("xVelocity", player.lastDirection.x);
        player.anim.SetFloat("yVelocity", player.lastDirection.y);

        if (player.moveInput.x != 0 || player.moveInput.y != 0)
            stateMachine.ChangeState(player.runState);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            player.SetVelocity(0, 0);
            stateMachine.ChangeState(player.axeState);
        }

        if (Input.GetKeyDown(KeyCode.F)){
            player.SetVelocity(0, 0);
            stateMachine.ChangeState(player.axeExpState);
        }
    }
}
