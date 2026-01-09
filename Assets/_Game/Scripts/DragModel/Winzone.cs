using UnityEngine;

public enum WinzoneType
{
    Normal,
    Cake,
    Frog,
    Pin,
}
public class Winzone : MonoBehaviour
{
    [Header("Cấu hình Phe phái")]
    public WinzoneType winzoneType = WinzoneType.Normal;

    [HideInInspector]
    public PoolType VFX_Pool = PoolType.VFX_Hearth;// hieu ung khi chạm

    public bool isSpecial = false;    // Tích vào = Đầu VIP (Nhân vật chính)
    public string targetTag = "Head"; // Tag để nhận diện đầu khác

    public bool isGoal = false;// phan biet doi tuong chien thang

    public bool isFollow = true;// cam se theo doi

    public bool isWinningObject = true;// phan biet doi tuong can tuong tac de chien thang

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

        if (cameraFollow != null && isFollow) cameraFollow.AddWinzone(this);

        if (transform.root.GetComponent<Level>() != null)
        {
            levelprarent = transform.root.GetComponent<Level>();
            levelprarent.AddHead(this);
        }
        if (winzoneType != WinzoneType.Normal)
        {
            if (levelprarent != null)
            {
                levelprarent.SetWinzoneType(winzoneType);
            }
        }
    }

    // Hàm xử lý chung: Dính cứng + Đóng băng + Tách rời cổ
    public void LockHeadToTarget(Rigidbody targetRb)
    {
        // --- 1. TẠO KHỚP DÍNH (Dùng ConfigurableJoint thay vì FixedJoint) ---
        // ConfigurableJoint có khả năng "Projection" giúp chống rung khi bị kẹt tường
        ConfigurableJoint newJoint = gameObject.AddComponent<ConfigurableJoint>();
        newJoint.connectedBody = targetRb;

        // Khóa tất cả các trục để nó hành xử như FixedJoint
        newJoint.xMotion = ConfigurableJointMotion.Locked;
        newJoint.yMotion = ConfigurableJointMotion.Locked;
        newJoint.zMotion = ConfigurableJointMotion.Locked;
        newJoint.angularXMotion = ConfigurableJointMotion.Locked;
        newJoint.angularYMotion = ConfigurableJointMotion.Locked;
        newJoint.angularZMotion = ConfigurableJointMotion.Locked;

        // [QUAN TRỌNG] Bật Projection: Giúp khớp tự sửa vị trí khi bị lực ép quá mạnh (như kẹt tường)
        newJoint.projectionMode = JointProjectionMode.PositionAndRotation;
        newJoint.projectionDistance = 0.01f; // Khoảng cách sai số chấp nhận được
        newJoint.projectionAngle = 1f;       // Góc sai số chấp nhận được
        newJoint.enablePreprocessing = true; // Tính toán ổn định hơn

        // --- 2. BỎ QUA VA CHẠM (Để 2 đầu không đẩy nhau) ---
        Collider myCol = GetComponent<Collider>();
        Collider targetCol = targetRb.GetComponent<Collider>();
        if (myCol != null && targetCol != null)
        {
            Physics.IgnoreCollision(myCol, targetCol);
        }

        // --- 3. XỬ LÝ VẬT LÝ (Để đứng yên tại chỗ) ---
        Rigidbody myRb = GetComponent<Rigidbody>();
        if (myRb != null)
        {
            // [FIX JITTER] Tăng khối lượng: 0.01 quá nhẹ, dễ bị nổ vật lý.
            myRb.mass = 1f;

            // [FIX JITTER] Tắt Interpolate: Tránh rung hình khi dính vào vật khác
            myRb.interpolation = RigidbodyInterpolation.None;

            // Triệt tiêu vận tốc
            myRb.linearVelocity = Vector3.zero;
            myRb.angularVelocity = Vector3.zero;

            // Tăng ma sát để hãm độ rung
            myRb.linearDamping = 5f;
            myRb.angularDamping = 5f;

            // C. Tắt lực cơ bắp (Spring/Damper)
            RagdollPuppetMaster myPuppetMaster = GetComponentInParent<RagdollPuppetMaster>();
            if (myPuppetMaster != null)
            {
                myPuppetMaster.RelaxMuscle(myRb);
            }

            // D. XÓA KHỚP CỔ CŨ (Tách hoàn toàn khỏi thân)
            // Lấy tất cả Joint đang có trên đầu
            ConfigurableJoint[] allJoints = GetComponents<ConfigurableJoint>();
            foreach (var j in allJoints)
            {
                // Nếu là cái joint mới tạo để dính vào A thì giữ lại
                if (j == newJoint) continue;

                // Còn lại (khớp cổ nối với thân) thì xóa đi
                Destroy(j);
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
                ParticelPool particelPool = SimplePool.Spawn<ParticelPool>(VFX_Pool, contact.point, Quaternion.Euler(-90, 0, 0));
                if (particelPool != null) particelPool.PlayVFX();
                otherHead.gameObject.tag = "Complete";
            }

            // --- CASE 1: TÔI LÀ VIP & THẮNG (VIP húc Thường) ---
            levelprarent.RemoveHead(this);
            RagdollDragBodyOnly ragdollDragBodyOnly = Camera.main.GetComponent<RagdollDragBodyOnly>();
            if (ragdollDragBodyOnly != null && ragdollDragBodyOnly.enabled) ragdollDragBodyOnly.DisableSnapBack();
            if (this.isSpecial != otherHead.isSpecial)
            {
                if (this.isSpecial)
                {
                    // A (Special) chạm B -> A thắng, A thu dây về
                    if (dragManager != null)
                    {
                        dragManager.ForceStopAndReturn();
                    }
                }
                else
                {
                    // B (Thường) chạm A -> B thua, B dính vào A
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
                // Cắt dây chuột ngay lập tức
                if (dragManager != null)
                {
                    dragManager.ForceStopImmediate();
                }
                // Dính vào đối phương
                LockHeadToTarget(collision.rigidbody);
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        CheckCollision(collision);
    }
}
