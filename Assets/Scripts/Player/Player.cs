using UnityEngine;

public class Player : Entity
{
    public static Player instance;
    public Player_Input input { get; private set; }



    public PLayer_IdleState idleState { get; private set; }
    public Player_Run runState { get; private set; }
    public Player_Axe axeState { get; private set; }
    public Player_Axe_Exp axeExpState { get; private set; }





    public Vector2 moveInput { get; private set; }
    public Vector2 mousePosition { get; private set; }
    public Vector2 lastDirection  { get; private set; } = Vector2.down;





    public float moveSpeed;

    protected override void Awake()
    {
        base.Awake();
        instance = this;
        input = new Player_Input();

        idleState = new PLayer_IdleState(this, stateMachine, "idle");
        runState = new Player_Run(this, stateMachine, "run");
        axeState = new Player_Axe(this, stateMachine, "Axe");
        axeExpState = new Player_Axe_Exp(this, stateMachine, "axe");
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

    public void UpdateLastDirection(Vector2 direction)
{
    if (direction != Vector2.zero)
    {

        Vector2 normalizedDir = direction.normalized;
        
        float angle = Mathf.Atan2(normalizedDir.y, normalizedDir.x) * Mathf.Rad2Deg;
        angle = Mathf.Round(angle / 45f) * 45f; 
        
        float rad = angle * Mathf.Deg2Rad;
        lastDirection = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }
}


}
