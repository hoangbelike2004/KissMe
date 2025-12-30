using UnityEngine;

public class RotatorDrag : MonoBehaviour
{
    public string stretchableTag = "Untagged"; // Nhớ đặt Tag cho các bộ phận cơ thể (VD: "Player")

    [Header("Cấu hình xoay")]
    public float rotateSpeed = 10f;

    [Range(0f, 170f)]
    public float maxAngle = 170f; // Góc mở rộng tối đa tính từ điểm bắt đầu kéo

    [Header("Cấu hình Trục (Quan trọng)")]
    // Tay thường là (0, 1, 0). Chân thường là (0, -1, 0). Hãy nhìn tia màu Vàng để chỉnh.
    public Vector3 pointingAxis = Vector3.up;
    public Vector3 rotateAxis = Vector3.forward;

    private Camera m_cam;
    private Rigidbody currentRb;
    private ConfigurableJoint joint;
    private RagdollPuppetMaster puppetMaster; // Tham chiếu đến ông trùm

    // --- BIẾN LƯU TRẠNG THÁI ---
    private ConfigurableJointMotion oldAngX, oldAngY, oldAngZ;

    // Biến lưu hướng gốc lúc vừa bấm chuột để làm mốc kẹp góc
    private Vector3 capturedStartDirection;

    void Start()
    {
        m_cam = Camera.main;
    }

    void Update()
    {
        // 1. BẮT ĐẦU KÉO
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = m_cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // Kiểm tra xem có trúng bộ phận cơ thể và có Rigidbody không
                if (hit.rigidbody != null && hit.collider.CompareTag(stretchableTag))
                {
                    StartControl(hit.rigidbody);
                }
            }
        }

        // 2. KẾT THÚC KÉO
        if (Input.GetMouseButtonUp(0) && currentRb != null)
        {
            StopControl();
        }
    }

    void FixedUpdate()
    {
        if (currentRb != null)
        {
            RotateTowardsMouse();
        }
    }

    void RotateTowardsMouse()
    {
        // A. Tính hướng chuột
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Vector3.Distance(m_cam.transform.position, currentRb.position);
        Vector3 targetPos = m_cam.ScreenToWorldPoint(mousePos);
        Vector3 directionToMouse = targetPos - currentRb.position;

        // B. [QUAN TRỌNG] KẸP GÓC (MATH CLAMP)
        // Tạo hình nón giới hạn dựa trên capturedStartDirection
        Vector3 clampedDirection = Vector3.RotateTowards(capturedStartDirection, directionToMouse, maxAngle * Mathf.Deg2Rad, 0.0f);

        // --- DEBUG VẼ TIA (Giúp bạn nhìn thấy góc kẹp) ---
        Debug.DrawRay(currentRb.position, capturedStartDirection * 2, Color.white); // Mốc gốc
        Debug.DrawRay(currentRb.position, clampedDirection * 2, Color.green);       // Hướng giới hạn

        // C. TÍNH TOÁN LỰC XOAY
        Vector3 currentPointingDir = currentRb.transform.TransformDirection(pointingAxis);
        Vector3 torqueDir = Vector3.Cross(currentPointingDir, clampedDirection).normalized;
        float angleDiff = Vector3.Angle(currentPointingDir, clampedDirection);

        float speed = rotateSpeed;
        if (angleDiff < 1f) speed = 0; // Vùng chết để chống rung

        Vector3 finalVelocity = torqueDir * speed * (angleDiff / 180f) * 50f;

        // Lọc trục xoay (chỉ cho xoay quanh trục quy định)
        if (rotateAxis == Vector3.forward) finalVelocity = new Vector3(0, 0, finalVelocity.z);
        else if (rotateAxis == Vector3.up) finalVelocity = new Vector3(0, finalVelocity.y, 0);

        currentRb.angularVelocity = finalVelocity;
    }

    void StartControl(Rigidbody rb)
    {
        currentRb = rb;
        joint = rb.GetComponent<ConfigurableJoint>();

        // --- DEBUG KIỂM TRA TRỤC (Nếu tia Vàng chĩa ngược -> Đảo dấu Pointing Axis) ---
        Debug.DrawRay(rb.position, rb.transform.TransformDirection(pointingAxis) * 2f, Color.yellow, 2f);

        // 1. TÌM ÔNG TRÙM VÀ RA LỆNH THẢ LỎNG
        puppetMaster = rb.transform.root.GetComponent<RagdollPuppetMaster>();
        if (puppetMaster != null)
        {
            puppetMaster.RelaxMuscle(currentRb);
        }

        if (joint != null)
        {
            // 2. LƯU TRẠNG THÁI CŨ
            oldAngX = joint.angularXMotion; oldAngY = joint.angularYMotion; oldAngZ = joint.angularZMotion;

            // 3. CHỤP ẢNH HƯỚNG HIỆN TẠI LÀM MỐC (SNAPSHOT)
            capturedStartDirection = currentRb.transform.TransformDirection(pointingAxis);

            // 4. KHÓA VỊ TRÍ, MỞ XOAY (FREE) ĐỂ CODE TOÁN HỌC ĐIỀU KHIỂN
            joint.xMotion = ConfigurableJointMotion.Locked;
            joint.yMotion = ConfigurableJointMotion.Locked;
            joint.zMotion = ConfigurableJointMotion.Locked;

            joint.angularXMotion = ConfigurableJointMotion.Free;
            joint.angularYMotion = ConfigurableJointMotion.Free;
            joint.angularZMotion = ConfigurableJointMotion.Free;

            // 5. CẤU HÌNH PHỤ TRỢ
            joint.projectionMode = JointProjectionMode.PositionAndRotation;
            joint.enablePreprocessing = false;
        }

        // Tăng ma sát xoay để dừng lại nhanh hơn khi thả chuột
        currentRb.angularDamping = 10f;
    }

    void StopControl()
    {
        if (joint != null)
        {
            // 1. TRẢ LẠI MOTION CŨ
            joint.angularXMotion = oldAngX; joint.angularYMotion = oldAngY; joint.angularZMotion = oldAngZ;
            joint.projectionMode = JointProjectionMode.None;
            joint.enablePreprocessing = true;
        }

        // 2. RA LỆNH CHO ÔNG TRÙM HỒI PHỤC CƠ BẮP (MƯỢT MÀ)
        // Lưu ý: Không cần set JointDrive thủ công ở đây nữa, PuppetMaster sẽ lo
        if (puppetMaster != null && currentRb != null)
        {
            puppetMaster.StiffenMuscle(currentRb);
        }

        if (currentRb != null) currentRb.angularDamping = 0.05f;
        currentRb = null;
    }
}