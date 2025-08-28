using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float interactionDistance = 2f;
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private LayerMask interactionLayer; // Chỉ định Layer của các vật thể có thể tương tác

    private Player player; // Tham chiếu đến script Player chính

    private void Awake()
    {
        player = GetComponent<Player>();
        if (player == null)
        {
            Debug.LogError("PlayerInteraction script requires a Player script on the same GameObject.", this);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(interactionKey))
        {
            TryInteract();
        }
    }

    private void TryInteract()
    {
        // Bắn một tia raycast từ vị trí của người chơi theo hướng nhìn cuối cùng
        RaycastHit2D hit = Physics2D.Raycast(transform.position, player.lastDirection, interactionDistance, interactionLayer);

        // Vẽ một tia debug trong Scene view để dễ hình dung
        Debug.DrawRay(transform.position, player.lastDirection * interactionDistance, Color.red, 1f);

        if (hit.collider != null)
        {
            // Kiểm tra xem đối tượng va chạm có component nào triển khai IInteractable không
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();

            if (interactable != null)
            {
                // Nếu có, gọi hàm Interact() của nó
                Debug.Log($"Interacting with {hit.collider.gameObject.name}");
                interactable.Interact();
            }
        }
    }
}
