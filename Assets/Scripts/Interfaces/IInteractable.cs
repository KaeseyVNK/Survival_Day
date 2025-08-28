public interface IInteractable
{
    // Bất kỳ đối tượng nào muốn người chơi có thể tương tác
    // sẽ phải triển khai (implement) interface này.
    void Interact();

    // (Tùy chọn trong tương lai) Bạn cũng có thể thêm các hàm khác như:
    // string GetInteractionText(); // Để hiển thị "Nhấn E để Mở"
}
