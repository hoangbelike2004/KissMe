using UnityEngine;
using System.Collections.Generic;

public class ActiveRagdollController : MonoBehaviour
{
    [Header("1. Cài đặt bản sao (Animation)")]
    [Tooltip("Kéo Prefab nhân vật gốc (có Animator) vào đây. Script sẽ tự tạo bản sao.")]
    public GameObject characterPrefab;
    [Tooltip("Vị trí xuất hiện của bản sao (thường để trùng với Ragdoll)")]
    public Transform spawnPoint;
    
    [Header("2. Cài đặt sức mạnh (Joint Drive)")]
    [Tooltip("Độ cứng của khớp (càng cao càng bám sát Animation)")]
    public float jointSpring = 1000f;
    [Tooltip("Độ hãm (chống rung lắc)")]
    public float jointDamper = 50f;

    // Các biến nội bộ
    private Animator targetAnimator; // Animator của bản sao
    private GameObject cloneObject;  // Object bản sao
    
    // Danh sách các cặp xương để đồng bộ
    private List<BonePair> bonePairs = new List<BonePair>();

    // Class lưu trữ cặp xương (Xương thật - Xương ảo)
    class BonePair
    {
        public ConfigurableJoint physicalJoint; // Khớp trên người Ragdoll
        public Transform animatedBone;          // Xương tương ứng trên bản sao
        public Quaternion initialRotation;      // Góc xoay mặc định (để tính toán bù trừ)
    }

    void Start()
    {
        // 1. TẠO BẢN SAO (ANIMATED CLONE)
        if (characterPrefab != null)
        {
            cloneObject = Instantiate(characterPrefab, transform.position, transform.rotation);
            cloneObject.name = "Animated_Ghost";
            
            // Xoá hết vật lý trên bản sao để nó không va chạm lung tung
            DestroyPhysicsRecursive(cloneObject.transform);
            
            // Lấy Animator của bản sao để điều khiển
            targetAnimator = cloneObject.GetComponent<Animator>();
            if (targetAnimator == null) targetAnimator = cloneObject.GetComponentInChildren<Animator>();

            // Làm bản sao trong suốt (tuỳ chọn - để dễ nhìn Debug)
            // MakeTransparent(cloneObject); 
        }

        // 2. ÁNH XẠ XƯƠNG (TÌM CẶP)
        // Tìm tất cả Joint trên người thật (Ragdoll)
        ConfigurableJoint[] allJoints = GetComponentsInChildren<ConfigurableJoint>();

        foreach (var joint in allJoints)
        {
            // Tìm xương có tên tương ứng bên phía Bản sao
            Transform foundAnimBone = FindBoneRecursively(cloneObject.transform, joint.gameObject.name);

            if (foundAnimBone != null)
            {
                // Cấu hình sức mạnh cho Joint luôn
                SetupJointDrive(joint);

                // Lưu vào danh sách để Update xử lý
                BonePair pair = new BonePair();
                pair.physicalJoint = joint;
                pair.animatedBone = foundAnimBone;
                pair.initialRotation = joint.transform.localRotation; // Lưu thế trạm ban đầu
                
                bonePairs.Add(pair);
            }
        }
        
        // Tắt va chạm giữa các bộ phận của chính Ragdoll để đỡ bị kẹt
        foreach (var joint in allJoints)
        {
            joint.enableCollision = false; 
        }
    }

    void FixedUpdate()
    {
        // 3. ĐỒNG BỘ CHUYỂN ĐỘNG (MAGIC HAPPENS HERE)
        foreach (var pair in bonePairs)
        {
            if (pair.physicalJoint != null && pair.animatedBone != null)
            {
                // Configurable Joint dùng TargetRotation để xoay
                // Công thức này giúp map góc xoay từ Animation sang Physics
                pair.physicalJoint.targetRotation = Quaternion.Inverse(pair.animatedBone.localRotation) * pair.initialRotation;
            }
        }
        
        // Đồng bộ vị trí tổng thể (Root) của bản sao đi theo Ragdoll 
        // (Hoặc ngược lại tuỳ game, ở đây tôi để bản sao bám theo Ragdoll để Animation chạy tại chỗ)
        if (cloneObject != null)
        {
            cloneObject.transform.position = transform.position;
            // cloneObject.transform.rotation = transform.rotation; // Có thể bật nếu muốn xoay theo
        }
    }

    // --- CÁC HÀM HỖ TRỢ ---

    // Đệ quy tìm xương theo tên
    Transform FindBoneRecursively(Transform current, string name)
    {
        if (current.name == name) return current;
        foreach (Transform child in current)
        {
            Transform found = FindBoneRecursively(child, name);
            if (found != null) return found;
        }
        return null;
    }

    // Xoá vật lý trên bản sao
    void DestroyPhysicsRecursive(Transform t)
    {
        // Xoá Rigidbody
        var rb = t.GetComponent<Rigidbody>();
        if (rb != null) Destroy(rb);

        // Xoá Collider
        var col = t.GetComponent<Collider>();
        if (col != null) Destroy(col);

        // Xoá Joint
        var joint = t.GetComponent<Joint>();
        if (joint != null) Destroy(joint);

        // Duyệt con
        foreach (Transform child in t)
        {
            DestroyPhysicsRecursive(child);
        }
    }

    // Setup lực cho Joint
    void SetupJointDrive(ConfigurableJoint joint)
    {
        // Cài đặt Angular X Drive (Gập duỗi)
        JointDrive drive = new JointDrive();
        drive.positionSpring = jointSpring;
        drive.positionDamper = jointDamper;
        drive.maximumForce = float.MaxValue;

        joint.angularXDrive = drive;
        joint.angularYZDrive = drive; // Cài luôn cho xoay ngang

        // Bắt buộc set Rotation Drive Mode
        joint.rotationDriveMode = RotationDriveMode.XYAndZ;
    }
    
    // Hàm public để bạn gọi Animation từ bên ngoài
    public void PlayAnimation(string animName)
    {
        if (targetAnimator != null)
        {
            targetAnimator.Play(animName);
        }
    }
}