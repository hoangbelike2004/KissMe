using UnityEngine;

public class HeadGameplay : MonoBehaviour
{
    [Header("Cấu hình Phe phái")]
    public bool isSpecial = false;    // Tích vào = Đầu VIP (Nhân vật chính)
    public string targetTag = "Head"; // Tag để nhận diện đầu khác

    [Header("Cấu hình Va chạm")]
    [Range(-1f, 1f)]
    public float hitAngleThreshold = 0.2f; // > 0: Phía trước. Càng lớn càng yêu cầu chính diện.

    private bool hasStuck = false;    // Cờ kiểm tra (True = đã xong phim)
    private RagdollDrag dragManager;  // Tham chiếu script kéo chuột

    private Level levelprarent;

    void Start()
    {
        // Lấy Manager từ Camera Main (Nhanh & Tối ưu)
        if (Camera.main != null)
        {
            dragManager = Camera.main.GetComponent<RagdollDrag>();
        }

        if (dragManager == null)
            Debug.LogError("❌ Không tìm thấy RagdollDrag trên Main Camera!");
        levelprarent = transform.root.GetComponent<Level>();
        levelprarent.AddHead(this);
    }

    void OnCollisionEnter(Collision collision)
    {
        // 1. Nếu đã dính rồi thì thôi (Chết đứng tại chỗ)
        if (hasStuck) return;

        // 2. Kiểm tra Tag
        if (collision.gameObject.CompareTag(targetTag))
        {
            // --- [BƯỚC KIỂM TRA GÓC VA CHẠM] ---
            // Lấy điểm va chạm đầu tiên
            ContactPoint contact = collision.GetContact(0);

            // Tính hướng từ tâm đầu mình -> điểm va chạm
            Vector3 directionToHit = (contact.point - transform.position).normalized;

            // Tính Dot Product (Tích vô hướng)
            // transform.forward là hướng mặt đang nhìn
            float dotProduct = Vector3.Dot(transform.forward, directionToHit);

            // Nếu góc va chạm nhỏ hơn ngưỡng -> Bỏ qua (coi như va chạm sượt)
            if (dotProduct < hitAngleThreshold)
            {
                // Debug.Log("❌ Va chạm sai góc (húc sượt/sau lưng) - Bỏ qua.");
                return;
            }
            // ------------------------------------

            HeadGameplay otherHead = collision.gameObject.GetComponent<HeadGameplay>();
            if (otherHead == null) return;

            if (this.GetInstanceID() > otherHead.GetInstanceID())
            {
                ParticelPool particelPool = SimplePool.Spawn<ParticelPool>(PoolType.VFX_Hearth, contact.point, Quaternion.Euler(-90, 0, 0));
                particelPool.PlayVFX();
            }
            // --- CASE 1: TÔI LÀ VIP & THẮNG (VIP húc Thường) ---
            if (this.isSpecial != otherHead.isSpecial)
            {
                levelprarent.RemoveHead(this);
                if (this.isSpecial)
                {
                    // Ra lệnh cho Mouse Joint buông ra và nảy về cổ
                    if (dragManager != null)
                    {
                        dragManager.ForceStopAndReturn();
                    }
                }
                else
                {
                    if (dragManager != null)
                    {
                        dragManager.ForceStopImmediate();
                    }
                    LockHeadToTarget(collision.rigidbody);
                }
            }

            // --- CASE 2: TÔI LÀ THƯỜNG & THUA (Thường bị VIP húc) ---
            // else if (!this.isSpecial && otherHead.isSpecial)
            // {
            //     Debug.Log("💀 THUA CUỘC! (Dính vào VIP)");

            //     // Dính vào VIP và rụng khỏi cổ
            //     LockHeadToTarget(collision.rigidbody);
            // }

            // --- CASE 3: HUỀ (Cùng loại va nhau) ---
            else
            {

                // 1. QUAN TRỌNG: Cắt dây chuột ngay lập tức, KHÔNG nảy về
                if (dragManager != null)
                {
                    dragManager.ForceStopImmediate();
                }

                // 2. Dính vào đối phương và đóng băng tại chỗ
                LockHeadToTarget(collision.rigidbody);
            }
        }
    }

    // Hàm xử lý chung: Dính cứng + Đóng băng + Tách rời cổ
    void LockHeadToTarget(Rigidbody targetRb)
    {
        hasStuck = true; // Đánh dấu đã chết/dính -> Không hồi phục nữa

        // 1. TẠO KHỚP DÍNH (Hàn chặt vào đối phương)
        FixedJoint joint = gameObject.AddComponent<FixedJoint>();
        joint.connectedBody = targetRb;

        // 2. XỬ LÝ VẬT LÝ (Để đứng yên tại chỗ)
        Rigidbody myRb = GetComponent<Rigidbody>();
        if (myRb != null)
        {
            myRb.mass = 0.01f; // Giảm khối lượng

            // A. Triệt tiêu vận tốc (STOP DEAD)
            // (Unity 6 dùng linearVelocity, Unity cũ dùng velocity)
            myRb.linearVelocity = Vector3.zero;
            myRb.angularVelocity = Vector3.zero;

            // B. Tăng ma sát cực đại (Để không bị trôi)
            myRb.linearDamping = 100f;
            myRb.angularDamping = 100f;

            // C. Tắt lực cơ bắp (Spring/Damper)
            RagdollPuppetMaster myPuppetMaster = GetComponentInParent<RagdollPuppetMaster>();
            if (myPuppetMaster != null)
            {
                myPuppetMaster.RelaxMuscle(myRb);
            }

            // D. Mở khóa vị trí (Unlock Motion) -> Rời khỏi cổ
            ConfigurableJoint myJoint = GetComponent<ConfigurableJoint>();
            if (myJoint != null)
            {
                myJoint.xMotion = ConfigurableJointMotion.Free;
                myJoint.yMotion = ConfigurableJointMotion.Free;
                myJoint.zMotion = ConfigurableJointMotion.Free;

                myJoint.angularXMotion = ConfigurableJointMotion.Free;
                myJoint.angularYMotion = ConfigurableJointMotion.Free;
                myJoint.angularZMotion = ConfigurableJointMotion.Free;
            }
        }
    }
}