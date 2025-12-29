using System.Collections;
using UnityEngine;

public class RagdollDrag : MonoBehaviour
{
    public string stretchableTag = "Head";

    [Header("Lực kéo chuột (Mouse Joint)")]
    public float dragSpring = 500f; // GIẢM XUỐNG: 1000 hơi cứng, 500 sẽ mềm hơn
    public float dragDamper = 10f;  // TĂNG LÊN: Để triệt tiêu dao động nhanh hơn

    [Header("Cấu hình Nảy về (Return)")]
    public float returnSpring = 2000f;
    public float returnDamper = 50f;

    private Camera m_cam;
    private bool isDragging;
    private float distToCamera;

    private ConfigurableJoint internalJoint;
    private SpringJoint mouseJoint;

    private Rigidbody currentRb;
    private JointSnapshot snapshot;
    private Vector3 initialRelativePos;
    private Coroutine returnCoroutine;

    // Biến lưu vị trí chuột mượt
    private Vector3 currentMouseWorldPos;

    // Lưu thông số vật lý cũ
    private float originalDrag;
    private float originalAngularDrag;
    private bool originalUseGravity;

    private void Start()
    {
        m_cam = Camera.main;
    }

    void Update()
    {
        // 1. CLICK CHUỘT
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = m_cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider != null && hit.collider.CompareTag(stretchableTag))
                {
                    StartDraggingHead(hit.rigidbody, hit.point);
                }
            }
        }

        // 2. THẢ CHUỘT
        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            StopDraggingAndBounceHead();
        }

        // 3. TÍNH TOÁN VỊ TRÍ CHUỘT (QUAN TRỌNG: Làm ở Update để mượt)
        if (isDragging)
        {
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = distToCamera;
            currentMouseWorldPos = m_cam.ScreenToWorldPoint(mousePos);
        }
    }

    public void FixedUpdate()
    {
        // 4. ÁP DỤNG VÀO VẬT LÝ (Chỉ gán vị trí đã tính ở trên)
        if (isDragging && mouseJoint != null)
        {
            mouseJoint.connectedAnchor = currentMouseWorldPos;
        }
    }

    public void StartDraggingHead(Rigidbody hitRb, Vector3 hitPoint)
    {
        if (returnCoroutine != null) StopCoroutine(returnCoroutine); // Stop coroutine cũ nếu click liên tiếp

        currentRb = hitRb;

        // Tính khoảng cách Z so với camera ngay lúc click
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
        originalDrag = hitRb.linearDamping; // Unity 6 đổi tên thành linearDamping (bản cũ là drag)
        originalAngularDrag = hitRb.angularDamping;
        originalUseGravity = hitRb.useGravity;

        hitRb.linearDamping = 10f; // TĂNG LÊN 10 để vật đỡ trôi tự do quá trớn
        hitRb.angularDamping = 10f;
        hitRb.useGravity = false;

        // Bật Interpolate để hình ảnh mượt (nếu chưa bật)
        hitRb.interpolation = RigidbodyInterpolation.Interpolate;

        // --- C. TẠO DÂY THỪNG ẢO ---
        mouseJoint = hitRb.gameObject.AddComponent<SpringJoint>();
        mouseJoint.autoConfigureConnectedAnchor = false;

        // Neo vào điểm click trên vật
        mouseJoint.anchor = hitRb.transform.InverseTransformPoint(hitPoint);

        // Cập nhật vị trí chuột ngay lập tức để không bị giật frame đầu
        Vector3 mousePosInit = Input.mousePosition;
        mousePosInit.z = distToCamera;
        currentMouseWorldPos = m_cam.ScreenToWorldPoint(mousePosInit);
        mouseJoint.connectedAnchor = currentMouseWorldPos;

        mouseJoint.spring = dragSpring;
        mouseJoint.damper = dragDamper * 5; // Damper cao giúp giảm rung
        mouseJoint.maxDistance = 0f;

        // Lưu vị trí tương đối
        if (internalJoint != null && internalJoint.connectedBody != null)
            initialRelativePos = internalJoint.connectedBody.transform.InverseTransformPoint(hitRb.position);
        else
            initialRelativePos = hitRb.position;

        isDragging = true;
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

        if (internalJoint != null && isDragging)
        {
            isDragging = false;
            returnCoroutine = StartCoroutine(DoReturnPhysicsHead());
        }
    }

    // ... Phần Coroutine DoReturnPhysics và Struct JointSnapshot giữ nguyên như cũ ...
    IEnumerator DoReturnPhysicsHead()
    {
        // (Copy lại y nguyên đoạn code cũ của bạn vào đây)
        // Lưu ý: Đảm bảo khi restore, kiểm tra null kỹ
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

            // Điều kiện dừng nảy
            if (distance <= 0.05f && velocityMag <= 0.5f) break;

            timer += Time.deltaTime;
            yield return null;
        }

        if (internalJoint != null) snapshot.Restore(internalJoint);
        returnCoroutine = null;
        internalJoint = null;
        currentRb = null;
    }

    // ========================================================================
    // LOGIC 2: KÉO TAY/CHÂN (LOGIC MỚI - KHÓA VỊ TRÍ, CHỈ XOAY)
    // ========================================================================
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
