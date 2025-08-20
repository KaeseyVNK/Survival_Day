using UnityEngine;

public class PLayer_IdleState : PlayerState
{
    public PLayer_IdleState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {

    }

    public override void Enter()
    {
        base.Enter();
        // Tắt animation chạy khi vào trạng thái idle
        player.anim.SetBool("run", false);
        player.SetVelocity(0, 0);
    }

    public override void Update()
    {
        base.Update();

        // Ưu tiên di chuyển ngang
        if (player.moveInput.x != 0)
            stateMachine.ChangeState(player.runLeftRightState);
        // Nếu không di chuyển ngang, kiểm tra di chuyển dọc
        else if (player.moveInput.y > 0)
            stateMachine.ChangeState(player.goUpState);
        else if (player.moveInput.y < 0)
            stateMachine.ChangeState(player.goDownState);
    }
}
