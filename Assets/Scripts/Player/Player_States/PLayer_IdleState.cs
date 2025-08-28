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

        // Kiểm tra input tấn công
        if (Input.GetMouseButtonDown(0)) // 0 là chuột trái
        {
            // Lấy item đang được chọn trên hotbar
            InventoryItem selectedItem = inventoryManager.hotbarItems[inventoryManager.selectedSlot];

            // Nếu không cầm gì thì thôi
            if (selectedItem == null || !(selectedItem.data is ToolItemData))
            {
                // Có thể thêm state đấm tay không ở đây trong tương lai
                return;
            }

            ToolItemData tool = selectedItem.data as ToolItemData;

            // Dựa vào loại công cụ để chuyển sang state tương ứng
            switch (tool.toolType)
            {
                case ToolType.Axe:
                    stateMachine.ChangeState(player.axeExpState);
                    break;
                case ToolType.Pickaxe:
                    stateMachine.ChangeState(player.pickaxeState);
                    break;
                // Thêm các trường hợp khác cho các công cụ khác
            }
        }
    }
}
