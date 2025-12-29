using UnityEngine;

public class RotatorDrag : MonoBehaviour
{
    public string stretchableTag = "Untagged";

    [Header("Cấu hình xoay")]
    public float rotateSpeed = 10f;

    [Range(0f, 170f)]
    public float maxAngle = 45f; // Góc mở rộng tối đa tính từ điểm bắt đầu

    [Header("Cấu hình Nảy về")]
    public float returnSpring = 1000f;
    public float returnDamper = 50f;

    public Vector3 pointingAxis = Vector3.up;
    public Vector3 rotateAxis = Vector3.forward;

    private Camera m_cam;
    private Rigidbody currentRb;
    private ConfigurableJoint joint;

    // --- BIẾN LƯU TRẠNG THÁI ---
    private ConfigurableJointMotion oldAngX, oldAngY, oldAngZ;
    private JointDrive oldAngXDrive, oldAngYZDrive;

    // [QUAN TRỌNG] Biến lưu hướng gốc lúc vừa bấm chuột
    private Vector3 capturedStartDirection;

    void Start()
    {
        m_cam = Camera.main;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = m_cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.rigidbody != null && hit.collider.CompareTag(stretchableTag))
                {
                    StartControl(hit.rigidbody);
                }
            }
        }

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
        Debug.Log($"Vật đang kéo: {name} | Góc MaxAngle code đang dùng là: {maxAngle}");
        // 1. Tính hướng chuột
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Vector3.Distance(m_cam.transform.position, currentRb.position);
        Vector3 targetPos = m_cam.ScreenToWorldPoint(mousePos);
        Vector3 directionToMouse = targetPos - currentRb.position;

        // 2. [QUAN TRỌNG NHẤT] KẸP GÓC DỰA TRÊN MỐC BAN ĐẦU
        // capturedStartDirection: Hướng lúc vừa bấm chuột (Mốc 20 độ của bạn)
        // directionToMouse: Hướng chuột hiện tại
        // maxAngle: Độ mở cho phép (vd: +/- 30 độ)

        // Hàm này tạo ra một cái "Hình nón" (Cone). 
        // Tâm nón là capturedStartDirection. Góc mở là maxAngle.
        // Chuột không bao giờ kéo vật ra khỏi hình nón này được.
        Vector3 clampedDirection = Vector3.RotateTowards(capturedStartDirection, directionToMouse, maxAngle * Mathf.Deg2Rad, 0.0f);

        // Debug vẽ tia:
        // - Trắng: Mốc ban đầu (cố định)
        // - Xanh: Hướng vật sẽ quay theo (đã bị giới hạn)
        Debug.DrawRay(currentRb.position, capturedStartDirection * 2, Color.white);
        Debug.DrawRay(currentRb.position, clampedDirection * 2, Color.green);

        // 3. Áp dụng lực xoay
        Vector3 currentPointingDir = currentRb.transform.TransformDirection(pointingAxis);
        Vector3 torqueDir = Vector3.Cross(currentPointingDir, clampedDirection).normalized;
        float angleDiff = Vector3.Angle(currentPointingDir, clampedDirection);

        float speed = rotateSpeed;
        if (angleDiff < 1f) speed = 0;

        Vector3 finalVelocity = torqueDir * speed * (angleDiff / 180f) * 50f;

        if (rotateAxis == Vector3.forward) finalVelocity = new Vector3(0, 0, finalVelocity.z);
        else if (rotateAxis == Vector3.up) finalVelocity = new Vector3(0, finalVelocity.y, 0);

        currentRb.angularVelocity = finalVelocity;
    }

    void StartControl(Rigidbody rb)
    {
        currentRb = rb;
        joint = rb.GetComponent<ConfigurableJoint>();

        if (joint != null)
        {
            // A. LƯU TRẠNG THÁI CŨ
            oldAngX = joint.angularXMotion; oldAngY = joint.angularYMotion; oldAngZ = joint.angularZMotion;
            oldAngXDrive = joint.angularXDrive; oldAngYZDrive = joint.angularYZDrive;

            // B. [MẤU CHỐT] CHỤP LẠI HƯỚNG HIỆN TẠI LÀM MỐC
            // Ví dụ lúc này đang là 20 độ, nó sẽ lưu vector 20 độ lại
            capturedStartDirection = currentRb.transform.TransformDirection(pointingAxis);

            // C. KHÓA VỊ TRÍ, MỞ XOAY
            joint.xMotion = ConfigurableJointMotion.Locked;
            joint.yMotion = ConfigurableJointMotion.Locked;
            joint.zMotion = ConfigurableJointMotion.Locked;

            joint.angularXMotion = ConfigurableJointMotion.Free;
            joint.angularYMotion = ConfigurableJointMotion.Free;
            joint.angularZMotion = ConfigurableJointMotion.Free;

            // D. TẮT LÒ XO
            JointDrive zeroDrive = new JointDrive { positionSpring = 0, positionDamper = 0 };
            joint.angularXDrive = zeroDrive;
            joint.angularYZDrive = zeroDrive;

            joint.projectionMode = JointProjectionMode.PositionAndRotation;
            joint.enablePreprocessing = false;
        }
        currentRb.angularDamping = 10f;
    }

    void StopControl()
    {
        if (joint != null)
        {
            // TRẢ LẠI TRẠNG THÁI CŨ
            joint.angularXMotion = oldAngX; joint.angularYMotion = oldAngY; joint.angularZMotion = oldAngZ;

            // BẬT LÒ XO NẢY VỀ
            JointDrive returnDrive = new JointDrive
            {
                positionSpring = returnSpring,
                positionDamper = returnDamper,
                maximumForce = float.MaxValue
            };
            joint.angularXDrive = returnDrive;
            joint.angularYZDrive = returnDrive;

            // QUAN TRỌNG: Nảy về đâu? 
            // Nếu bạn muốn nó nảy về góc 0 chuẩn của khớp xương:
            joint.targetRotation = Quaternion.identity;

            // (Nếu bạn muốn nó nảy về cái góc 20 độ lúc nãy thì dùng dòng dưới đây thay thế)
            // joint.targetRotation = Quaternion.Inverse(joint.connectedBody.transform.rotation) * Quaternion.LookRotation(capturedStartDirection);

            joint.projectionMode = JointProjectionMode.None;
            joint.enablePreprocessing = true;
        }

        if (currentRb != null) currentRb.angularDamping = 0.05f;
        currentRb = null;
    }
}