using UnityEngine;

public class RagdollSetup : MonoBehaviour
{
    [Header("Cấu hình An toàn")]
    public bool setKinematicOnStart = true; // Bắt buộc bật: Để nhân vật không bị nổ khi vừa vào game
    public bool disableSelfCollision = true; // Tắt va chạm tay chân

    void Start()
    {
        SetupSafeMode();
    }

    [ContextMenu("Setup Safe Mode")]
    public void SetupSafeMode()
    {
        // 1. Lấy tất cả Rigidbody con
        Rigidbody[] allRbs = GetComponentsInChildren<Rigidbody>();

        // 2. Lấy Rigidbody của chính thằng cha (Root) để TRÁNH NÓ RA
        Rigidbody rootRb = GetComponent<Rigidbody>();

        foreach (Rigidbody rb in allRbs)
        {
            // QUAN TRỌNG: Nếu là Root thì bỏ qua, không đụng vào
            if (rb == rootRb) continue;

            // Cấu hình chuẩn cho Ragdoll
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.solverIterations = 30; // Tăng độ chính xác

            // Đưa về trạng thái ngủ (Kinematic) để Animator điều khiển bình thường
            // Khi nào bạn kéo (Dragger) thì Dragger sẽ tự mở khoá sau
            if (setKinematicOnStart)
            {
                rb.isKinematic = true;
            }
        }

        // 3. Tắt va chạm nội bộ (Chống giật)
        Collider[] allColliders = GetComponentsInChildren<Collider>();
        if (disableSelfCollision)
        {
            for (int i = 0; i < allColliders.Length; i++)
            {
                // Bỏ qua Collider của Root (để Root còn va chạm với đất)
                if (allColliders[i].gameObject == this.gameObject) continue;

                for (int j = i + 1; j < allColliders.Length; j++)
                {
                    if (allColliders[j].gameObject == this.gameObject) continue;

                    Physics.IgnoreCollision(allColliders[i], allColliders[j]);
                }
            }
        }

        Debug.Log("Đã Setup Ragdoll an toàn. Root được bảo vệ!");
    }
}