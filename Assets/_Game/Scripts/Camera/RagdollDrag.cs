using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Cần thư viện này để dùng Dictionary

public class RagdollDrag : MonoBehaviour, IDrag
{
    // ========================================================================
    // 1. CẤU HÌNH CHUNG
    // ========================================================================
    [Header("--- PHÂN LOẠI TAG ---")]
    public string stretchableTag = "Head"; // Tag để Kéo đầu
    public string limbTag = "Untagged";    // Tag để Xoay tay/chân

    public DragType DragType => DragType.Limb;
    // ========================================================================
    // 2. CẤU HÌNH KÉO ĐẦU (HEAD)
    // ========================================================================
    [Header("--- KÉO ĐẦU (HEAD) ---")]
    [Header("Lực kéo chuột (Mouse Joint)")]
    public float dragSpring = 500f;
    public float dragDamper = 10f;

    [Header("Cấu hình Nảy về (Return)")]
    public float returnSpring = 2000f;
    public float returnDamper = 50f;

    // --- [QUAN TRỌNG] DICTIONARY LƯU SNAPSHOT ---
    // Key: Joint của cái đầu, Value: Dữ liệu snapshot GỐC của đầu đó
    private Dictionary<ConfigurableJoint, JointSnapshot> headSnapshots = new Dictionary<ConfigurableJoint, JointSnapshot>();

    // Biến Runtime
    private bool isDraggingHead;
    private float distToCamera;

    private ConfigurableJoint currentInternalJoint; // Joint đang bị kéo hiện tại
    private SpringJoint mouseJoint;
    private Rigidbody currentRb;

    private Vector3 currentMouseWorldPos;
    private Vector3 initialRelativePos; // Lưu vị trí tương đối để check khoảng cách về đích

    // Lưu trạng thái Rigidbody tạm thời
    private float originalDrag;
    private float originalAngularDrag;
    private bool originalUseGravity;

    // ========================================================================
    // 3. CẤU HÌNH XOAY TAY CHÂN (LIMB)
    // ========================================================================
    [Header("--- XOAY TAY CHÂN (LIMB) ---")]
    public float rotateSpeed = 10f;
    [Range(0f, 170f)] public float maxAngle = 170f;
    public Vector3 pointingAxis = Vector3.up;
    public Vector3 rotateAxis = Vector3.forward;

    private bool isDraggingLimb;
    private Rigidbody limbRb;
    private ConfigurableJoint limbJoint;
    private RagdollPuppetMaster puppetMaster;

    // Lưu trạng thái cũ của Limb
    private ConfigurableJointMotion oldAngX, oldAngY, oldAngZ;
    private Vector3 capturedStartDirection;

    private Camera m_cam;
    private CameraFollow m_cameraFollow;

    private void Start()
    {
        m_cam = Camera.main;
        m_cameraFollow = GetComponent<CameraFollow>();
    }

    void Update()
    {
        // 1. CLICK CHUỘT
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = m_cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.rigidbody != null && hit.collider != null)
                {
                    // A. KÉO ĐẦU
                    if (hit.collider.CompareTag(stretchableTag))
                    {
                        StartDraggingHead(hit.rigidbody, hit.point);
                        GameController.Instance.DestroyTutorial();
                        GameController.Instance.Vibrate();
                    }
                    // B. XOAY TAY CHÂN
                    else if (hit.collider.CompareTag(limbTag))
                    {
                        GameController.Instance.Vibrate();
                        StartDraggingLimb(hit.rigidbody);
                    }
                }
            }
        }

        // 2. THẢ CHUỘT
        if (Input.GetMouseButtonUp(0))
        {
            if (isDraggingHead) StopDraggingAndBounceHead();
            if (isDraggingLimb) StopDraggingLimb();
        }

        // 3. TÍNH VỊ TRÍ CHUỘT
        if (isDraggingHead)
        {
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = distToCamera;
            currentMouseWorldPos = m_cam.ScreenToWorldPoint(mousePos);
        }
    }

    public void FixedUpdate()
    {
        OnDrag();
    }
    public void OnDrag()
    {
        // Logic Kéo Đầu
        if (isDraggingHead && mouseJoint != null)
        {
            mouseJoint.connectedAnchor = currentMouseWorldPos;
        }

        // Logic Xoay Tay
        if (isDraggingLimb && limbRb != null)
        {
            RotateLimbTowardsMouse();
        }
    }

    void LateUpdate()
    {
        if (m_cameraFollow != null) m_cameraFollow.UpdateCam();
    }

    // ========================================================================
    // PHẦN 1: LOGIC KÉO ĐẦU (ĐÃ SỬA: SNAPSHOT MẶC ĐỊNH & PARAMETER)
    // ========================================================================

    public void StartDraggingHead(Rigidbody hitRb, Vector3 hitPoint)
    {
        currentRb = hitRb;
        currentInternalJoint = hitRb.GetComponent<ConfigurableJoint>();

        Vector3 screenPos = m_cam.WorldToScreenPoint(hitPoint);
        distToCamera = screenPos.z;

        // --- A. XỬ LÝ JOINT VÀ SNAPSHOT ---
        if (currentInternalJoint != null)
        {
            // [QUAN TRỌNG] Chỉ lưu Snapshot NẾU CHƯA CÓ
            // Điều này đảm bảo ta luôn giữ cấu hình gốc ban đầu, không bao giờ bị ghi đè bởi trạng thái lỗi
            if (!headSnapshots.ContainsKey(currentInternalJoint))
            {
                JointSnapshot tempSnap = new JointSnapshot();
                tempSnap.Capture(currentInternalJoint);
                headSnapshots.Add(currentInternalJoint, tempSnap);
            }

            // Phá vỡ cấu hình để kéo (Thả lỏng hoàn toàn)
            currentInternalJoint.xMotion = ConfigurableJointMotion.Free;
            currentInternalJoint.yMotion = ConfigurableJointMotion.Free;
            currentInternalJoint.zMotion = ConfigurableJointMotion.Free;
            currentInternalJoint.angularXMotion = ConfigurableJointMotion.Free;
            currentInternalJoint.angularYMotion = ConfigurableJointMotion.Free;
            currentInternalJoint.angularZMotion = ConfigurableJointMotion.Free;

            JointDrive zeroDrive = new JointDrive { positionSpring = 0, positionDamper = 0 };
            currentInternalJoint.xDrive = zeroDrive;
            currentInternalJoint.yDrive = zeroDrive;
            currentInternalJoint.zDrive = zeroDrive;
        }

        // --- B. CẤU HÌNH RIGIDBODY ---
        originalDrag = hitRb.linearDamping;
        originalAngularDrag = hitRb.angularDamping;
        originalUseGravity = hitRb.useGravity;

        hitRb.linearDamping = 10f;
        hitRb.angularDamping = 10f;
        hitRb.useGravity = false;
        hitRb.interpolation = RigidbodyInterpolation.Interpolate;

        // --- C. TẠO SPRING JOINT (MOUSE) ---
        mouseJoint = hitRb.gameObject.AddComponent<SpringJoint>();
        mouseJoint.autoConfigureConnectedAnchor = false;
        mouseJoint.anchor = hitRb.transform.InverseTransformPoint(hitPoint);

        Vector3 mousePosInit = Input.mousePosition;
        mousePosInit.z = distToCamera;
        currentMouseWorldPos = m_cam.ScreenToWorldPoint(mousePosInit);
        mouseJoint.connectedAnchor = currentMouseWorldPos;

        mouseJoint.spring = dragSpring;
        mouseJoint.damper = dragDamper * 5;
        mouseJoint.maxDistance = 0f;

        // Lưu vị trí tương đối để check hồi phục
        if (currentInternalJoint != null && currentInternalJoint.connectedBody != null)
            initialRelativePos = currentInternalJoint.connectedBody.transform.InverseTransformPoint(hitRb.position);
        else
            initialRelativePos = hitRb.position;

        isDraggingHead = true;
    }

    public void StopDraggingAndBounceHead()
    {
        if (mouseJoint != null) Destroy(mouseJoint);

        // Khôi phục Rigidbody ngay lập tức
        if (currentRb != null)
        {
            currentRb.useGravity = originalUseGravity;
            currentRb.linearDamping = originalDrag;
            currentRb.angularDamping = originalAngularDrag;
        }

        if (currentInternalJoint != null && isDraggingHead)
        {
            // [QUAN TRỌNG] Truyền tham số cụ thể vào Coroutine
            StartCoroutine(DoReturnPhysicsHead(currentInternalJoint, currentRb, initialRelativePos));
        }

        // Reset biến toàn cục
        isDraggingHead = false;
        currentRb = null;
        currentInternalJoint = null;
    }

    // Coroutine nhận tham số riêng biệt, sử dụng Snapshot từ Dictionary
    IEnumerator DoReturnPhysicsHead(ConfigurableJoint jointToReturn, Rigidbody rbToReturn, Vector3 initialPos)
    {
        // Lấy snapshot gốc từ Dictionary
        if (!headSnapshots.ContainsKey(jointToReturn)) yield break;
        JointSnapshot mySnapshot = headSnapshots[jointToReturn];

        // 1. Tắt trọng lực và vận tốc để bay về thẳng (không bị nặng kéo xuống)
        if (rbToReturn != null)
        {
            rbToReturn.useGravity = false;
            rbToReturn.linearVelocity = Vector3.zero;
            rbToReturn.angularVelocity = Vector3.zero;
        }

        // 2. Set Target từ Snapshot gốc (Chống thụt cổ)
        jointToReturn.targetPosition = mySnapshot.targetPosition;
        jointToReturn.targetRotation = mySnapshot.targetRotation;

        // 3. Tạo lực hồi phục
        JointDrive returnDrive = new JointDrive
        {
            positionSpring = returnSpring,
            positionDamper = returnDamper,
            maximumForce = float.MaxValue
        };

        // Áp dụng lực Vị trí (Kéo về)
        jointToReturn.xDrive = returnDrive;
        jointToReturn.yDrive = returnDrive;
        jointToReturn.zDrive = returnDrive;

        // [QUAN TRỌNG] Áp dụng lực Xoay (Chống gục đầu/sụp cổ)
        jointToReturn.angularXDrive = returnDrive;
        jointToReturn.angularYZDrive = returnDrive;

        // Mở khóa Motion để lò xo hoạt động
        jointToReturn.xMotion = ConfigurableJointMotion.Free;
        jointToReturn.yMotion = ConfigurableJointMotion.Free;
        jointToReturn.zMotion = ConfigurableJointMotion.Free;
        jointToReturn.angularXMotion = ConfigurableJointMotion.Free;
        jointToReturn.angularYMotion = ConfigurableJointMotion.Free;
        jointToReturn.angularZMotion = ConfigurableJointMotion.Free;

        float timeOut = 3.0f;
        float timer = 0;

        while (timer < timeOut)
        {
            if (jointToReturn == null || rbToReturn == null) yield break;

            Vector3 currentRelPos;
            if (jointToReturn.connectedBody != null)
                currentRelPos = jointToReturn.connectedBody.transform.InverseTransformPoint(rbToReturn.position);
            else
                currentRelPos = rbToReturn.position;

            float distance = Vector3.Distance(currentRelPos, initialPos);

            float velocityMag = rbToReturn.linearVelocity.magnitude;
            if (jointToReturn.connectedBody != null)
                velocityMag = (rbToReturn.linearVelocity - jointToReturn.connectedBody.linearVelocity).magnitude;

            // Điều kiện dừng: Về rất sát (0.01f) và đứng yên
            if (distance <= 0.01f && velocityMag <= 0.5f) break;

            timer += Time.deltaTime;
            yield return null;
        }

        // 4. Kết thúc: Phanh gấp lần cuối
        if (rbToReturn != null)
        {
            rbToReturn.linearVelocity = Vector3.zero;
            rbToReturn.angularVelocity = Vector3.zero;
        }

        // 5. Khôi phục khớp từ Snapshot gốc
        if (jointToReturn != null)
        {
            mySnapshot.Restore(jointToReturn);
            // KHÔNG xóa khỏi Dictionary để lần sau kéo vẫn dùng Snapshot gốc này
        }

        // 6. Bật lại trọng lực
        if (rbToReturn != null) rbToReturn.useGravity = true;

        // 7. Gọi PuppetMaster hồi phục
        RagdollPuppetMaster pm = jointToReturn.GetComponentInParent<RagdollPuppetMaster>();
        if (pm != null && rbToReturn != null) pm.StiffenMuscle(rbToReturn);
    }

    public void ForceStopAndReturn()
    {
        if (isDraggingHead)
        {
            StopDraggingAndBounceHead();
        }
    }

    public void ForceStopImmediate()
    {
        if (mouseJoint != null) Destroy(mouseJoint);
        isDraggingHead = false;
        currentRb = null;
        currentInternalJoint = null;
    }

    // Kiểm tra xem Rigidbody này có phải đang được kéo không (cho các script khác gọi)
    public bool IsDraggingHead(Rigidbody rb)
    {
        return currentRb == rb && isDraggingHead;
    }

    // ========================================================================
    // PHẦN 2: LOGIC XOAY TAY CHÂN (GIỮ NGUYÊN)
    // ========================================================================

    void StartDraggingLimb(Rigidbody hitRb)
    {
        isDraggingLimb = true;
        limbRb = hitRb;
        limbJoint = hitRb.GetComponent<ConfigurableJoint>();

        Debug.DrawRay(hitRb.position, hitRb.transform.TransformDirection(pointingAxis) * 2f, Color.yellow, 2f);

        puppetMaster = hitRb.GetComponentInParent<RagdollPuppetMaster>();
        if (puppetMaster != null) puppetMaster.RelaxMuscle(limbRb);

        if (limbJoint != null)
        {
            oldAngX = limbJoint.angularXMotion;
            oldAngY = limbJoint.angularYMotion;
            oldAngZ = limbJoint.angularZMotion;

            capturedStartDirection = limbRb.transform.TransformDirection(pointingAxis);

            limbJoint.xMotion = ConfigurableJointMotion.Locked;
            limbJoint.yMotion = ConfigurableJointMotion.Locked;
            limbJoint.zMotion = ConfigurableJointMotion.Locked;

            limbJoint.angularXMotion = ConfigurableJointMotion.Free;
            limbJoint.angularYMotion = ConfigurableJointMotion.Free;
            limbJoint.angularZMotion = ConfigurableJointMotion.Free;

            limbJoint.projectionMode = JointProjectionMode.PositionAndRotation;
            limbJoint.enablePreprocessing = false;
        }
        limbRb.angularDamping = 10f;
    }

    void RotateLimbTowardsMouse()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Vector3.Distance(m_cam.transform.position, limbRb.position);
        Vector3 targetPos = m_cam.ScreenToWorldPoint(mousePos);
        Vector3 directionToMouse = targetPos - limbRb.position;

        Vector3 clampedDirection = Vector3.RotateTowards(capturedStartDirection, directionToMouse, maxAngle * Mathf.Deg2Rad, 0.0f);

        Debug.DrawRay(limbRb.position, capturedStartDirection * 2, Color.white);
        Debug.DrawRay(limbRb.position, clampedDirection * 2, Color.green);

        Vector3 currentPointingDir = limbRb.transform.TransformDirection(pointingAxis);
        Vector3 torqueDir = Vector3.Cross(currentPointingDir, clampedDirection).normalized;
        float angleDiff = Vector3.Angle(currentPointingDir, clampedDirection);

        float speed = (angleDiff < 1f) ? 0 : rotateSpeed;
        Vector3 finalVelocity = torqueDir * speed * (angleDiff / 180f) * 50f;

        if (rotateAxis == Vector3.forward) finalVelocity = new Vector3(0, 0, finalVelocity.z);
        else if (rotateAxis == Vector3.up) finalVelocity = new Vector3(0, finalVelocity.y, 0);

        limbRb.angularVelocity = finalVelocity;
    }

    void StopDraggingLimb()
    {
        if (limbJoint != null)
        {
            limbJoint.angularXMotion = oldAngX;
            limbJoint.angularYMotion = oldAngY;
            limbJoint.angularZMotion = oldAngZ;
            limbJoint.projectionMode = JointProjectionMode.None;
            limbJoint.enablePreprocessing = true;
        }

        if (puppetMaster != null && limbRb != null)
        {
            puppetMaster.StiffenMuscle(limbRb);
        }

        if (limbRb != null) limbRb.angularDamping = 0.05f;

        isDraggingLimb = false;
        limbRb = null;
        limbJoint = null;
    }

    public void OnActive()
    {
        this.enabled = true;
    }

    public void OnDeactive()
    {
        this.enabled = false;
    }
}

// ========================================================================
// STRUCT JOINT SNAPSHOT
// ========================================================================
public class JointSnapshot
{
    // 1. Motion
    public ConfigurableJointMotion xMotion, yMotion, zMotion;
    public ConfigurableJointMotion angXMotion, angYMotion, angZMotion;

    // 2. Drive
    public JointDrive xDrive, yDrive, zDrive;
    public JointDrive angXDrive, angYZDrive;
    public RotationDriveMode driveMode;

    // 3. Limits
    public SoftJointLimit lowXLimit, highXLimit;
    public SoftJointLimit yLimit, zLimit;

    // 4. Target & Projection
    public Vector3 targetPosition;
    public Quaternion targetRotation;
    public JointProjectionMode projectionMode;

    public void Capture(ConfigurableJoint joint)
    {
        xMotion = joint.xMotion; yMotion = joint.yMotion; zMotion = joint.zMotion;
        angXMotion = joint.angularXMotion; angYMotion = joint.angularYMotion; angZMotion = joint.angularZMotion;
        xDrive = joint.xDrive; yDrive = joint.yDrive; zDrive = joint.zDrive;
        angXDrive = joint.angularXDrive; angYZDrive = joint.angularYZDrive;
        driveMode = joint.rotationDriveMode;
        lowXLimit = joint.lowAngularXLimit; highXLimit = joint.highAngularXLimit;
        yLimit = joint.angularYLimit; zLimit = joint.angularZLimit;
        targetPosition = joint.targetPosition;
        targetRotation = joint.targetRotation;
        projectionMode = joint.projectionMode;
    }

    public void Restore(ConfigurableJoint joint)
    {
        joint.lowAngularXLimit = lowXLimit; joint.highAngularXLimit = highXLimit;
        joint.angularYLimit = yLimit; joint.angularZLimit = zLimit;
        joint.xMotion = xMotion; joint.yMotion = yMotion; joint.zMotion = zMotion;
        joint.angularXMotion = angXMotion; joint.angularYMotion = angYMotion; joint.angularZMotion = angZMotion;
        joint.xDrive = xDrive; joint.yDrive = yDrive; joint.zDrive = zDrive;
        joint.angularXDrive = angXDrive; joint.angularYZDrive = angYZDrive;
        joint.rotationDriveMode = driveMode;
        joint.targetPosition = targetPosition;
        joint.targetRotation = targetRotation;
        joint.projectionMode = projectionMode;
    }
}