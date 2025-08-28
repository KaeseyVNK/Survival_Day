using UnityEngine;
using TMPro;

// Script này điều khiển hành vi của một đối tượng chữ nổi
public class FloatingText : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private float fadeOutDuration = 1f;
    [SerializeField] private Vector3 moveDirection = Vector3.up;

    private TextMeshPro textMesh;
    private Color textColor;
    private float fadeTimer;

    private void Awake()
    {
        // Lấy component TextMeshPro từ chính đối tượng này
        textMesh = GetComponent<TextMeshPro>();
        if (textMesh == null)
        {
            Debug.LogError("FloatingText prefab requires a TextMeshPro component!", this);
            return;
        }

        textColor = textMesh.color;
        fadeTimer = fadeOutDuration;

        // Tự hủy đối tượng sau một khoảng thời gian
        Destroy(gameObject, fadeOutDuration);
    }

    /// <summary>
    /// Gán nội dung cho text.
    /// </summary>
    public void SetText(string text)
    {
        if (textMesh != null)
        {
            textMesh.text = text;
        }
    }

    private void Update()
    {
        // Di chuyển chữ
        transform.position += moveDirection * moveSpeed * Time.deltaTime;

        // Làm mờ chữ theo thời gian
        fadeTimer -= Time.deltaTime;
        if (fadeTimer < 0)
        {
            // Để chắc chắn rằng nó sẽ biến mất
            textColor.a = 0;
        }
        else
        {
            // Tính toán độ trong suốt (alpha) dựa trên thời gian còn lại
            textColor.a = fadeTimer / fadeOutDuration;
        }

        if (textMesh != null)
        {
            textMesh.color = textColor;
        }
    }
}
