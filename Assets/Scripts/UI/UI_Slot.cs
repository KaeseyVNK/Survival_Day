using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_Slot : MonoBehaviour
{
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private GameObject selectionBorder;

    private void Awake()
    {
        // Mặc định tắt border đi
        ToggleSelection(false);
    }

    public void ToggleSelection(bool isSelected)
    {
        if (selectionBorder != null)
        {
            selectionBorder.SetActive(isSelected);
        }
    }

    public void UpdateSlot(InventoryItem item)
    {
        if (item != null)
        {
            itemIcon.gameObject.SetActive(true);
            quantityText.gameObject.SetActive(true);

            itemIcon.sprite = item.data.sprite;
            quantityText.text = item.quantity.ToString();
        }
        else
        {
            // Nếu không có item, ẩn icon và text đi
            itemIcon.gameObject.SetActive(false);
            quantityText.gameObject.SetActive(false);
        }
    }
}
