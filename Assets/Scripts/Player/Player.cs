using UnityEngine;

public class Player : Entity
{
    public static Player instance;

    public Player_Input input { get; private set; }


    public PLayer_IdleState idleState { get; private set; }
    public Player_GoDown goDownState { get; private set; }
    public Player_GoUp goUpState { get; private set; }
    public Player_Run_Left_Right runLeftRightState { get; private set; }
    
    public Vector2 moveInput { get; private set; }
    public Vector2 mousePosition { get; private set; }

    public float moveSpeed;

    protected override void Awake()
    {
        base.Awake();
        instance = this;
        input = new Player_Input();

        idleState = new PLayer_IdleState(this, stateMachine, "idle");
        goDownState = new Player_GoDown(this, stateMachine, "run");
        goUpState = new Player_GoUp(this, stateMachine, "run");
        runLeftRightState = new Player_Run_Left_Right(this, stateMachine, "run");
    }

    protected override void Start()
    {
        base.Start();
        stateMachine.Initialize(idleState);
    }

    private void OnEnable()
    {
        input.Enable();

        input.Player.Movements.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        input.Player.Movements.canceled += ctx => moveInput = Vector2.zero;

    }
    private void OnDisable()
    {
        input.Disable();
    }


}
