using UnityEngine;


public enum BoxState
{
    None,
    Shaking,
    Finished
}
public class FallingBox : MonoBehaviour
{
    [SerializeField] float shakeTime = 0.5f;
    [SerializeField] float shakeSpeed = 1f;
    [SerializeField] float shakeFactor = 0.1f;

    Rigidbody2D rb;
    BoxCollider2D boxCollider;

    private Vector3 originalPosition;
    private float timer;

    public BoxState boxState { get; private set; } = BoxState.None;

    void Start ( )
    {
        rb = GetComponent<Rigidbody2D> ( );
        boxCollider = GetComponent<BoxCollider2D> ( );
    }


    void Update ( )
    {
        switch ( boxState )
        {
            case BoxState.None:
            case BoxState.Finished:
                return;

            case BoxState.Shaking:
                // Use Perlin Noise here for a quasi-random shake. Multiply or Add extra values for noise speed variations.
                // Many different ways to produc these offset values. This is just one variation.
                var xOffset = Mathf.PerlinNoise ( Time.time * shakeSpeed, 0 );
                var yOffset = Mathf.PerlinNoise ( 0, Time.time * shakeSpeed );

                transform.position = originalPosition + new Vector3 ( xOffset, yOffset, 0 ) * shakeFactor;
                timer += Time.deltaTime;
                if ( timer > shakeTime )
                {
                    boxState = BoxState.None;
                    transform.position = originalPosition;
                    timer = 0;
                }
                break;          
        }
    }


    //Called when entering the "danger zone"
    public void ChangeStatus ( )
    {
        if ( boxState == BoxState.None )
        {
            boxState = BoxState.Shaking;
            originalPosition = transform.position;
            timer = 0;
        }
    }
}
