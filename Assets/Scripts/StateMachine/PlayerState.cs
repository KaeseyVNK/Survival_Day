using UnityEngine;

public class PlayerState : EntityState
{
    protected Player player;
    protected Player_Input input;
    

    public PlayerState(Player player, StateMachine stateMachine, string animBoolName ) : base(stateMachine, animBoolName)
    {
        this.player = player;
        anim = player.anim;
        rb = player.rb;
        input = player.input;
    }

    public override void Update(){
        base.Update();
    }
}
