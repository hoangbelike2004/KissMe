using UnityEngine;

public class Winzone : MonoBehaviour
{
    [Header("Cấu hình Phe phái")]
    public bool isSpecial = false;    // Tích vào = Đầu VIP (Nhân vật chính)
    public string targetTag = "Head"; // Tag để nhận diện đầu khác

    public bool isGoal = false;

    protected RagdollDrag dragManager;  // Tham chiếu script kéo chuột

    private CameraFollow cameraFollow;

    protected Level levelprarent;
    void Start()
    {
        OnInit();
    }
    public virtual void OnInit()
    {
        // Lấy Manager từ Camera Main (Nhanh & Tối ưu)
        if (Camera.main != null)
        {
            dragManager = Camera.main.GetComponent<RagdollDrag>();
            cameraFollow = Camera.main.GetComponent<CameraFollow>();
        }

        if (dragManager == null)
            Debug.LogError("❌ Không tìm thấy RagdollDrag trên Main Camera!");

        if (cameraFollow != null) cameraFollow.AddWinzone(this);
        levelprarent = transform.root.GetComponent<Level>();
        levelprarent.AddHead(this);
    }
    // Hàm xử lý chung: Dính cứng + Đóng băng + Tách rời cổ
    public void LockHeadToTarget(Rigidbody targetRb)
    {
        //hasStuck = true; // Đánh dấu đã chết/dính -> Không hồi phục nữa

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
            myRb.linearDamping = 0;
            myRb.angularDamping = 0;

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

    public virtual void CheckCollision(Collision collision)
    {
        if (collision.gameObject.CompareTag(targetTag))
        {
            ContactPoint contact = collision.GetContact(0);

            HeadGameplay otherHead = collision.gameObject.GetComponent<HeadGameplay>();
            if (otherHead == null) return;
            if (!isGoal && !otherHead.isGoal) return;
            if (isGoal)
            {
                ParticelPool particelPool = SimplePool.Spawn<ParticelPool>(PoolType.VFX_Hearth, contact.point, Quaternion.Euler(-90, 0, 0));
                particelPool.PlayVFX();
                otherHead.gameObject.tag = "Complete";
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
                        // if (!dragManager.IsDraggingHead(GetComponent<Rigidbody>()))
                        // {
                        //     dragManager.ForceStopImmediate();
                        // }
                    }
                }
                else
                {
                    if (dragManager != null && dragManager.IsDraggingHead(GetComponent<Rigidbody>()))
                    {
                        dragManager.ForceStopImmediate();
                    }
                    LockHeadToTarget(collision.rigidbody);
                }
            }
            // --- CASE 2: HUỀ (Cùng loại va nhau) ---
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

    void OnCollisionEnter(Collision collision)
    {
        CheckCollision(collision);
    }
}
