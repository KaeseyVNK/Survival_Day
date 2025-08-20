using UnityEngine;

public class Player_Run_Left_Right : PlayerState
{
    public Player_Run_Left_Right(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();
    }

    public override void Update()
    {
        base.Update();

        // Áp dụng di chuyển
        player.SetVelocity(player.moveInput.x * player.moveSpeed, player.moveInput.y * player.moveSpeed);

        // Cập nhật tham số cho Blend Tree
        player.anim.SetFloat("xVelocity", player.moveInput.x);
        player.anim.SetFloat("yVelocity", player.moveInput.y);


        // Kiểm tra điều kiện chuyển state
        if (player.moveInput.x == 0 && player.moveInput.y > 0)
            stateMachine.ChangeState(player.goUpState);
        else if (player.moveInput.x == 0 && player.moveInput.y < 0)
            stateMachine.ChangeState(player.goDownState);
        else if (player.moveInput.x == 0 && player.moveInput.y == 0)
            stateMachine.ChangeState(player.idleState);
    }

}
