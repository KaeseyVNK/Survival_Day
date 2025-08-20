using UnityEngine;

public class Player_GoDown : PlayerState
{
    public Player_GoDown(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();
        player.anim.SetBool("run", true);
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
        else if (player.moveInput.y >= 0) // Nếu nhả phím xuống hoặc nhấn phím lên
            stateMachine.ChangeState(player.idleState); // Chuyển về idle để idle quyết định đi lên hay đứng yên
    }

    public override void Exit()
    {
        base.Exit();
    }

}
