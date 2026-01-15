using UnityEngine;

public class ClickTriggerJump : MonoBehaviour
{
    [Header("--- KẾT NỐI ---")]
    [Tooltip("Kéo Rigidbody của ĐỐI TƯỢNG KHÁC (nhân vật/bóng) vào đây")]
    public Rigidbody targetRb;

    [Header("--- CẤU HÌNH ---")]
    public float jumpForce = 10f;

    private Camera _mainCamera;

    void Start()
    {
        _mainCamera = Camera.main;

        // Kiểm tra xem đã gán đối tượng chưa để tránh lỗi
        if (targetRb == null)
        {
            Debug.LogError("Bạn chưa gán đối tượng cần nhảy vào ô Target Rb!");
        }
    }

    void Update()
    {
        CheckClickOnMe();
    }

    void CheckClickOnMe()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // Bắn tia ray
            if (Physics.Raycast(ray, out hit))
            {
                // KIỂM TRA: Nếu tia ray bắn trúng "Cái nút này" (this.transform)
                if (hit.transform == this.transform)
                {
                    Debug.Log(1);
                    MakeTargetJump();
                }
            }
        }
    }

    void MakeTargetJump()
    {
        if (targetRb != null)
        {
            // 1. Reset vận tốc Y của đối tượng kia về 0 (để nhảy dứt khoát)
            // Lưu ý: Unity 6 dùng .linearVelocity, bản cũ dùng .velocity
            Vector3 currentVel = targetRb.linearVelocity;
            targetRb.linearVelocity = new Vector3(currentVel.x, 0, currentVel.z);

            // 2. Tác động lực lên đối tượng kia
            targetRb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

            // (Tùy chọn) Có thể thêm hiệu ứng nút bị ấn xuống 1 chút ở đây
        }
    }
}