using UnityEngine;

public class Player_GoUp : PlayerState
{
    public Player_GoUp(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
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
        if (player.moveInput.x != 0)
            stateMachine.ChangeState(player.runLeftRightState);
        else if (player.moveInput.y <= 0) // Nếu nhả phím lên hoặc nhấn phím xuống
            stateMachine.ChangeState(player.idleState); // Chuyển về idle để idle quyết định đi xuống hay đứng yên
    }

    public override void Exit()
    {
        base.Exit();
    }
}
