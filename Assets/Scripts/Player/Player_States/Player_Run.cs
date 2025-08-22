using UnityEngine;

public class Player_Run : PlayerState
{
    public Player_Run(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();
    }

    public override void Update()
    {
        base.Update();

        player.SetVelocity(player.moveInput.x * player.moveSpeed, player.moveInput.y * player.moveSpeed);

        player.UpdateLastDirection(player.moveInput);

        player.anim.SetFloat("xVelocity", player.moveInput.x);
        player.anim.SetFloat("yVelocity", player.moveInput.y);
        
        if (player.moveInput.x == 0 && player.moveInput.y == 0)
            stateMachine.ChangeState(player.idleState);

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
