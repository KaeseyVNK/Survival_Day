using UnityEngine;

// Lớp quản lý việc tạo ra các dòng chữ nổi
// Đây là một Singleton để dễ dàng truy cập từ bất kỳ đâu
public class FloatingTextManager : MonoBehaviour
{
    public static FloatingTextManager instance;

    [Tooltip("Kéo Prefab của Floating Text vào đây")]
    [SerializeField] private GameObject floatingTextPrefab;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
        }
    }

    public void Show(string message, Vector3 position)
    {
        if (floatingTextPrefab == null)
        {
            Debug.LogError("FloatingTextManager: Chưa gán Prefab cho chữ nổi!");
            return;
        }

        // Tạo một instance của prefab chữ nổi
        GameObject textObject = Instantiate(floatingTextPrefab, position, Quaternion.identity);

        // Lấy component FloatingText và gán nội dung cho nó
        FloatingText floatingText = textObject.GetComponent<FloatingText>();
        if (floatingText != null)
        {
            floatingText.SetText(message);
        }
    }
}
