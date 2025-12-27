using UnityEngine;
using System.Collections;

public class RagdollDraggerV13 : MonoBehaviour
{
    [Header("Cài đặt")]
    public string stretchableTag = "Head";
    public float mouseForce = 3000f;       // [GIẢM] Giảm lực chút cho đỡ rung
    public float dragElasticity = 100f;

    [Header("Giới hạn góc xoay khi kéo")]
    [Tooltip("Góc tối đa (độ)")]
    public float dragRotationLimit = 30f;

    [Header("Cấu hình Nảy & Hồi phục")]
    public float returnSpring = 2000f;
    public float returnDamper = 50f;
    public float jiggleDuration = 1.0f;

    // --- BIẾN LƯU TRỮ ---
    private Vector3 originalConnectedAnchor;
    private Rigidbody originalConnectedBody;
    private Quaternion originalJointTargetRotation;
    private Quaternion startLocalRotation;
    private Vector3 startLocalPosition;

    // --- BIẾN RUNTIME ---
    private Rigidbody draggedRb;
    private ConfigurableJoint draggedJoint;
    private GameObject mouseDragger;
    private SpringJoint mouseJoint;
    private bool isDragging = false;
    private float distToCamera;

    // Lưu thông số cũ
    private ConfigurableJointMotion oldX, oldY, oldZ;
    private ConfigurableJointMotion oldAngX, oldAngY, oldAngZ;
    private JointDrive oldXDrive, oldYDrive, oldZDrive;
    private JointDrive oldAngXDrive, oldAngYDrive;
    private RotationDriveMode oldDriveMode;
    private JointProjectionMode oldProjectionMode; // [MỚI] Lưu chế độ Projection

    // Lưu giới hạn cũ
    private SoftJointLimit oldLowXLimit, oldHighXLimit;
    private SoftJointLimit oldYLimit, oldZLimit;

    private Rigidbody[] allRagdollRbs;

    private enum DragMode { Stretch, Constrained }
    private DragMode currentMode;

    void Start()
    {
        allRagdollRbs = transform.root.GetComponentsInChildren<Rigidbody>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.rigidbody != null)
                {
                    DragMode mode = hit.collider.CompareTag(stretchableTag) ? DragMode.Stretch : DragMode.Constrained;
                    StartDragging(hit.rigidbody, hit.point, mode);
                }
            }
        }

        if (isDragging && mouseDragger != null)
        {
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = distToCamera;
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
            mouseDragger.GetComponent<Rigidbody>().MovePosition(worldPos);
        }

        if (isDragging && Input.GetMouseButtonUp(0))
        {
            StopDragging();
        }
    }

    void StartDragging(Rigidbody hitRb, Vector3 hitPoint, DragMode mode)
    {
        draggedRb = hitRb;
        draggedJoint = draggedRb.GetComponent<ConfigurableJoint>();
        if (draggedJoint == null) return;

        isDragging = true;
        currentMode = mode;
        distToCamera = Vector3.Distance(Camera.main.transform.position, hitRb.position);

        // 1. Đóng băng các bộ phận khác
        foreach (Rigidbody rb in allRagdollRbs)
        {
            if (rb != draggedRb) rb.isKinematic = true;
        }

        // 2. Lưu trạng thái gốc
        startLocalRotation = draggedRb.transform.localRotation;
        startLocalPosition = draggedRb.transform.localPosition;

        originalConnectedBody = draggedJoint.connectedBody;
        originalConnectedAnchor = draggedJoint.connectedAnchor;
        originalJointTargetRotation = draggedJoint.targetRotation;

        oldX = draggedJoint.xMotion; oldY = draggedJoint.yMotion; oldZ = draggedJoint.zMotion;
        oldAngX = draggedJoint.angularXMotion; oldAngY = draggedJoint.angularYMotion; oldAngZ = draggedJoint.angularZMotion;
        oldXDrive = draggedJoint.xDrive; oldYDrive = draggedJoint.yDrive; oldZDrive = draggedJoint.zDrive;
        oldAngXDrive = draggedJoint.angularXDrive; oldAngYDrive = draggedJoint.angularYZDrive;
        oldDriveMode = draggedJoint.rotationDriveMode;
        oldProjectionMode = draggedJoint.projectionMode; // [MỚI]

        oldLowXLimit = draggedJoint.lowAngularXLimit;
        oldHighXLimit = draggedJoint.highAngularXLimit;
        oldYLimit = draggedJoint.angularYLimit;
        oldZLimit = draggedJoint.angularZLimit;

        // 3. Cấu hình theo chế độ
        if (currentMode == DragMode.Stretch)
        {
            // --- KÉO ĐẦU (GIỮ NGUYÊN) ---
            Vector3 currentWorldAnchor = originalConnectedBody.transform.TransformPoint(originalConnectedAnchor);
            currentWorldAnchor.x = originalConnectedBody.transform.position.x + originalConnectedAnchor.x;

            draggedJoint.autoConfigureConnectedAnchor = false;
            draggedJoint.connectedBody = null;
            draggedJoint.connectedAnchor = currentWorldAnchor;

            draggedJoint.xMotion = ConfigurableJointMotion.Free;
            draggedJoint.yMotion = ConfigurableJointMotion.Free;
            draggedJoint.zMotion = ConfigurableJointMotion.Free;
            draggedJoint.angularXMotion = ConfigurableJointMotion.Locked;
            draggedJoint.angularYMotion = ConfigurableJointMotion.Locked;
            draggedJoint.angularZMotion = ConfigurableJointMotion.Locked;

            JointDrive dragDrive = new JointDrive { positionSpring = dragElasticity, positionDamper = 5f, maximumForce = float.MaxValue };
            draggedJoint.xDrive = dragDrive; draggedJoint.yDrive = dragDrive; draggedJoint.zDrive = dragDrive;
        }
        else
        {
            // --- KÉO TAY/CHÂN (ĐÃ SỬA LỖI GIẬT & DÃN) ---

            // A. Cài đặt giới hạn góc 30 độ
            SoftJointLimit limit = new SoftJointLimit();
            limit.limit = dragRotationLimit;
            limit.bounciness = 0f; // Không cho nảy ở giới hạn (giảm giật)
            limit.contactDistance = 0.05f;

            SoftJointLimit negLimit = new SoftJointLimit();
            negLimit.limit = -dragRotationLimit;
            negLimit.bounciness = 0f;

            draggedJoint.lowAngularXLimit = negLimit;
            draggedJoint.highAngularXLimit = limit;
            draggedJoint.angularYLimit = limit;
            draggedJoint.angularZLimit = limit;

            // B. Giới hạn xoay
            draggedJoint.angularXMotion = ConfigurableJointMotion.Limited;
            draggedJoint.angularYMotion = ConfigurableJointMotion.Limited;
            draggedJoint.angularZMotion = ConfigurableJointMotion.Limited;

            // C. KHÓA CỨNG VỊ TRÍ (CHỐNG DÃN CHÂN)
            draggedJoint.xMotion = ConfigurableJointMotion.Locked;
            draggedJoint.yMotion = ConfigurableJointMotion.Locked;
            draggedJoint.zMotion = ConfigurableJointMotion.Locked;

            // D. [QUAN TRỌNG] TẮT CƠ BẮP (Joint Drive)
            // Để tay chân "mềm nhũn" khi kéo, tránh việc cơ bắp cũ đánh nhau với chuột
            JointDrive zeroDrive = new JointDrive { positionSpring = 0, positionDamper = 0, maximumForce = 0 };
            draggedJoint.xDrive = zeroDrive;
            draggedJoint.yDrive = zeroDrive;
            draggedJoint.zDrive = zeroDrive;
            draggedJoint.angularXDrive = zeroDrive;
            draggedJoint.angularYZDrive = zeroDrive;

            // E. [QUAN TRỌNG] BẬT PROJECTION (CHỐNG DÃN)
            // Nếu chân lỡ bị kéo dãn ra do lỗi vật lý, nó sẽ tự hít lại
            draggedJoint.projectionMode = JointProjectionMode.PositionAndRotation;
            draggedJoint.projectionDistance = 0.01f;
            draggedJoint.projectionAngle = 1f;
        }

        draggedRb.angularVelocity = Vector3.zero;

        // Tạo chuột ảo
        mouseDragger = new GameObject("MouseDragger");
        mouseDragger.transform.position = hitPoint;
        Rigidbody draggerRb = mouseDragger.AddComponent<Rigidbody>();
        draggerRb.isKinematic = true;

        mouseJoint = mouseDragger.AddComponent<SpringJoint>();
        mouseJoint.connectedBody = draggedRb;
        mouseJoint.spring = mouseForce;

        // [QUAN TRỌNG] Tăng Damper chuột lên để kéo mượt hơn, giảm rung
        mouseJoint.damper = 50f;

        mouseJoint.maxDistance = 0f;
        mouseJoint.autoConfigureConnectedAnchor = false;
        mouseJoint.connectedAnchor = draggedRb.transform.InverseTransformPoint(hitPoint);
    }

    void StopDragging()
    {
        isDragging = false;
        if (mouseJoint) Destroy(mouseJoint);
        if (mouseDragger) Destroy(mouseDragger);

        StartCoroutine(JiggleBackRoutine());
    }

    IEnumerator JiggleBackRoutine()
    {
        if (draggedJoint != null)
        {
            // 1. Setup lò xo hồi phục
            if (currentMode == DragMode.Stretch && originalConnectedBody != null)
            {
                draggedJoint.connectedBody = originalConnectedBody;
                draggedJoint.autoConfigureConnectedAnchor = false;
                draggedJoint.connectedAnchor = originalConnectedAnchor;
            }

            JointDrive snapDrive = new JointDrive
            {
                positionSpring = returnSpring,
                positionDamper = returnDamper,
                maximumForce = float.MaxValue
            };

            draggedJoint.rotationDriveMode = RotationDriveMode.Slerp;
            draggedJoint.slerpDrive = snapDrive;

            // Nếu là tay chân, lúc nãy ta đã tắt X/Y/Z drive, giờ phải set lại nếu cần
            // Nhưng tốt nhất là Stretch mode dùng Free, còn Constrained mode giữ Locked
            if (currentMode == DragMode.Stretch)
            {
                draggedJoint.xMotion = ConfigurableJointMotion.Free;
                draggedJoint.yMotion = ConfigurableJointMotion.Free;
                draggedJoint.zMotion = ConfigurableJointMotion.Free;
                draggedJoint.xDrive = snapDrive; draggedJoint.yDrive = snapDrive; draggedJoint.zDrive = snapDrive;

                draggedJoint.angularXMotion = ConfigurableJointMotion.Free;
                draggedJoint.angularYMotion = ConfigurableJointMotion.Free;
                draggedJoint.angularZMotion = ConfigurableJointMotion.Free;
            }

            draggedJoint.targetRotation = originalJointTargetRotation;
            draggedJoint.targetPosition = Vector3.zero;

            // 2. Đợi nảy về
            yield return new WaitForSeconds(jiggleDuration);

            // 3. Reset cứng
            draggedRb.transform.localRotation = startLocalRotation;
            draggedRb.transform.localPosition = startLocalPosition;
            draggedRb.angularVelocity = Vector3.zero;
#if UNITY_6000_0_OR_NEWER
            draggedRb.linearVelocity = Vector3.zero;
#else
            draggedRb.velocity = Vector3.zero;
#endif

            // 4. Trả lại cài đặt cũ
            draggedJoint.lowAngularXLimit = oldLowXLimit;
            draggedJoint.highAngularXLimit = oldHighXLimit;
            draggedJoint.angularYLimit = oldYLimit;
            draggedJoint.angularZLimit = oldZLimit;

            draggedJoint.xMotion = oldX; draggedJoint.yMotion = oldY; draggedJoint.zMotion = oldZ;
            draggedJoint.angularXMotion = oldAngX; draggedJoint.angularYMotion = oldAngY; draggedJoint.angularZMotion = oldAngZ;
            draggedJoint.xDrive = oldXDrive; draggedJoint.yDrive = oldYDrive; draggedJoint.zDrive = oldZDrive;
            draggedJoint.angularXDrive = oldAngXDrive; draggedJoint.angularYZDrive = oldAngYDrive;
            draggedJoint.rotationDriveMode = oldDriveMode;
            draggedJoint.projectionMode = oldProjectionMode; // Trả lại Projection

            // 5. Xả đóng băng
            foreach (Rigidbody rb in allRagdollRbs)
            {
                if (rb != null) rb.isKinematic = false;
            }
        }
    }
}