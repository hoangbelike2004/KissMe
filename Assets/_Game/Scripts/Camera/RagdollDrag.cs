using UnityEngine;
using System.Collections;

public class RagdollDrag : MonoBehaviour
{
    // ========================================================================
    // 1. CẤU HÌNH CHUNG
    // ========================================================================
    [Header("--- PHÂN LOẠI TAG ---")]
    public string stretchableTag = "Head"; // Tag để Kéo đầu
    public string limbTag = "Untagged";    // Tag để Xoay tay/chân

    // ========================================================================
    // 2. BIẾN & CẤU HÌNH CỦA SCRIPT GỐC (KÉO ĐẦU)
    // ========================================================================
    [Header("--- KÉO ĐẦU (HEAD) ---")]
    [Header("Lực kéo chuột (Mouse Joint)")]
    public float dragSpring = 500f;
    public float dragDamper = 10f;

    [Header("Cấu hình Nảy về (Return)")]
    public float returnSpring = 2000f;
    public float returnDamper = 50f;

    private bool isDraggingHead; // Đổi tên nhẹ để phân biệt với Limb
    private float distToCamera;

    private ConfigurableJoint internalJoint;
    private SpringJoint mouseJoint;

    private Rigidbody currentRb;
    private JointSnapshot snapshot;
    private Vector3 initialRelativePos;
    private Coroutine returnCoroutine;
    private Vector3 currentMouseWorldPos;

    private float originalDrag;
    private float originalAngularDrag;
    private bool originalUseGravity;

    // ========================================================================
    // 3. BIẾN & CẤU HÌNH CỦA SCRIPT XOAY (LIMB)
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
                    // A. NẾU LÀ ĐẦU -> CHẠY LOGIC GỐC CỦA BẠN
                    if (hit.collider.CompareTag(stretchableTag))
                    {
                        StartDraggingHead(hit.rigidbody, hit.point);
                    }
                    // B. NẾU LÀ TAY/CHÂN -> CHẠY LOGIC XOAY
                    else if (hit.collider.CompareTag(limbTag))
                    {
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

        // 3. TÍNH TOÁN VỊ TRÍ CHUỘT (Cho Head)
        if (isDraggingHead)
        {
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = distToCamera;
            currentMouseWorldPos = m_cam.ScreenToWorldPoint(mousePos);
        }
    }

    public void FixedUpdate()
    {
        // LOGIC KÉO ĐẦU (Giữ nguyên)
        if (isDraggingHead && mouseJoint != null)
        {
            mouseJoint.connectedAnchor = currentMouseWorldPos;
        }

        // LOGIC XOAY TAY (Mới thêm)
        if (isDraggingLimb && limbRb != null)
        {
            RotateLimbTowardsMouse();
        }
    }
    void LateUpdate()
    {
        m_cameraFollow.UpdateCam();
    }

    // ========================================================================
    // PHẦN LOGIC 1: KÉO ĐẦU (COPY Y NGUYÊN TỪ SCRIPT GỐC CỦA BẠN)
    // ========================================================================

    public void StartDraggingHead(Rigidbody hitRb, Vector3 hitPoint)
    {
        if (returnCoroutine != null) StopCoroutine(returnCoroutine);

        currentRb = hitRb;

        Vector3 screenPos = m_cam.WorldToScreenPoint(hitPoint);
        distToCamera = screenPos.z;

        // --- A. XỬ LÝ JOINT NỘI TẠI ---
        internalJoint = hitRb.GetComponent<ConfigurableJoint>();
        if (internalJoint != null)
        {
            snapshot.Capture(internalJoint);

            // Thả lỏng hoàn toàn
            internalJoint.xMotion = ConfigurableJointMotion.Free;
            internalJoint.yMotion = ConfigurableJointMotion.Free;
            internalJoint.zMotion = ConfigurableJointMotion.Free;
            internalJoint.angularXMotion = ConfigurableJointMotion.Free;
            internalJoint.angularYMotion = ConfigurableJointMotion.Free;
            internalJoint.angularZMotion = ConfigurableJointMotion.Free;

            JointDrive zeroDrive = new JointDrive { positionSpring = 0, positionDamper = 0 };
            internalJoint.xDrive = zeroDrive;
            internalJoint.yDrive = zeroDrive;
            internalJoint.zDrive = zeroDrive;
        }

        // --- B. LƯU & CHỈNH SỬA RIGIDBODY ---
        originalDrag = hitRb.linearDamping;
        originalAngularDrag = hitRb.angularDamping;
        originalUseGravity = hitRb.useGravity;

        hitRb.linearDamping = 10f; // TĂNG LÊN 10 -> Đây là lý do nó không bị xoay!
        hitRb.angularDamping = 10f;
        hitRb.useGravity = false;

        hitRb.interpolation = RigidbodyInterpolation.Interpolate;

        // --- C. TẠO DÂY THỪNG ẢO ---
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

        if (internalJoint != null && internalJoint.connectedBody != null)
            initialRelativePos = internalJoint.connectedBody.transform.InverseTransformPoint(hitRb.position);
        else
            initialRelativePos = hitRb.position;

        isDraggingHead = true;
    }

    public void StopDraggingAndBounceHead()
    {
        if (mouseJoint != null) Destroy(mouseJoint);

        if (currentRb != null)
        {
            currentRb.useGravity = originalUseGravity;
            currentRb.linearDamping = originalDrag;
            currentRb.angularDamping = originalAngularDrag;
        }

        if (internalJoint != null && isDraggingHead)
        {
            isDraggingHead = false;
            returnCoroutine = StartCoroutine(DoReturnPhysicsHead());
        }
        else
        {
            isDraggingHead = false;
            currentRb = null; // Reset nếu không có Joint
        }
    }

    IEnumerator DoReturnPhysicsHead()
    {
        internalJoint.targetPosition = Vector3.zero;
        internalJoint.targetRotation = Quaternion.identity;

        JointDrive returnDrive = new JointDrive
        {
            positionSpring = returnSpring,
            positionDamper = returnDamper,
            maximumForce = float.MaxValue
        };

        internalJoint.xDrive = returnDrive;
        internalJoint.yDrive = returnDrive;
        internalJoint.zDrive = returnDrive;

        float timeOut = 3.0f;
        float timer = 0;

        while (timer < timeOut)
        {
            if (internalJoint == null || currentRb == null) yield break;

            Vector3 currentRelPos;
            if (internalJoint.connectedBody != null)
                currentRelPos = internalJoint.connectedBody.transform.InverseTransformPoint(currentRb.position);
            else
                currentRelPos = currentRb.position;

            float distance = Vector3.Distance(currentRelPos, initialRelativePos);

            float velocityMag = currentRb.linearVelocity.magnitude;
            if (internalJoint.connectedBody != null)
                velocityMag = (currentRb.linearVelocity - internalJoint.connectedBody.linearVelocity).magnitude;

            if (distance <= 0.05f && velocityMag <= 0.5f) break;

            timer += Time.deltaTime;
            yield return null;
        }

        if (internalJoint != null) snapshot.Restore(internalJoint);

        // Hồi phục lại cho PuppetMaster nếu có (để đảm bảo đồng bộ)
        RagdollPuppetMaster pm = internalJoint.GetComponentInParent<RagdollPuppetMaster>();
        if (pm != null && currentRb != null) pm.StiffenMuscle(currentRb);

        returnCoroutine = null;
        internalJoint = null;
        currentRb = null;
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
        if (returnCoroutine != null) StopCoroutine(returnCoroutine);

        isDraggingHead = false;
        currentRb = null;
        internalJoint = null;
    }

    // ========================================================================
    // PHẦN LOGIC 2: XOAY TAY CHÂN (THÊM MỚI VÀO)
    // ========================================================================

    void StartDraggingLimb(Rigidbody hitRb)
    {
        isDraggingLimb = true;
        limbRb = hitRb;
        limbJoint = hitRb.GetComponent<ConfigurableJoint>();

        Debug.DrawRay(hitRb.position, hitRb.transform.TransformDirection(pointingAxis) * 2f, Color.yellow, 2f);

        // Với tay chân, ta PHẢI dùng RelaxMuscle để không bị giật lại
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


    //kiem tra xem thang nay co phai la thang dang duoc keo hay khong
    public bool IsDraggingHead(Rigidbody rb)
    {
        return currentRb == rb && isDraggingHead;
    }
}
// Struct JointSnapshot giữ nguyên

public struct JointSnapshot
{
    // 1. Motion (Độ tự do)
    public ConfigurableJointMotion xMotion, yMotion, zMotion;
    public ConfigurableJointMotion angXMotion, angYMotion, angZMotion;

    // 2. Drive (Lực lò xo)
    public JointDrive xDrive, yDrive, zDrive;
    public JointDrive angXDrive, angYZDrive;
    public RotationDriveMode driveMode;

    // 3. Limits (Giới hạn khớp)
    public SoftJointLimit lowXLimit, highXLimit;
    public SoftJointLimit yLimit, zLimit;

    // 4. Target & Projection
    public Vector3 targetPosition;
    public Quaternion targetRotation;
    public JointProjectionMode projectionMode;

    // --- HÀM LƯU TRẠNG THÁI ---
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

    // --- HÀM KHÔI PHỤC TRẠNG THÁI ---
    public void Restore(ConfigurableJoint joint)
    {
        // Trả lại Limits trước (quan trọng)
        joint.lowAngularXLimit = lowXLimit; joint.highAngularXLimit = highXLimit;
        joint.angularYLimit = yLimit; joint.angularZLimit = zLimit;

        // Trả lại Motion
        joint.xMotion = xMotion; joint.yMotion = yMotion; joint.zMotion = zMotion;
        joint.angularXMotion = angXMotion; joint.angularYMotion = angYMotion; joint.angularZMotion = angZMotion;

        // Trả lại Drive
        joint.xDrive = xDrive; joint.yDrive = yDrive; joint.zDrive = zDrive;
        joint.angularXDrive = angXDrive; joint.angularYZDrive = angYZDrive;
        joint.rotationDriveMode = driveMode;

        // Trả lại các cài đặt khác
        joint.targetPosition = targetPosition;
        joint.targetRotation = targetRotation;
        joint.projectionMode = projectionMode;
    }
}