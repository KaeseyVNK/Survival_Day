using UnityEngine;

public class DropItem : MonoBehaviour
{
    public DropItemData dropItemData;

    public bool isTrigger = true;

    public float radius = 0.5f;

    private void Awake()
    {
        // Đảm bảo item có một trigger collider để có thể được nhặt
        CircleCollider2D col = gameObject.GetComponent<CircleCollider2D>();
        col.isTrigger = isTrigger;
        col.radius = radius; // Bạn có thể điều chỉnh bán kính này cho phù hợp
    }

    private void Start()
    {
        GetComponentInChildren<SpriteRenderer>().sprite = dropItemData.sprite;
    }

    public void Pickup(){
        // Tương lai: Thêm hiệu ứng âm thanh/hình ảnh khi nhặt
        Destroy(gameObject);
    }
}
