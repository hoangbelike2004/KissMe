using UnityEngine;

public class RagdollDragBodyOnly : MonoBehaviour, IDrag
{
    [Header("--- CẤU HÌNH ---")]
    [Tooltip("Chỉ object nào có Tag này mới được kéo (VD: BodyPart)")]
    public string bodyTag = "BodyPart";

    public DragType DragType => DragType.Body;

    [Header("--- LỰC KÉO ---")]
    public float force = 10000f;
    public float damper = 500f;
    public float distance = 0.01f;

    [Header("--- DEBUG ---")]
    public bool drawRope = true;
    public Color ropeColor = Color.green;

    // --- BIẾN NỘI BỘ ---
    private SpringJoint mouseJoint;
    private Rigidbody draggedRb;

    // [THÊM MỚI] Biến để xử lý hồi phục hình dáng
    private ConfigurableJoint draggedJoint;
    private JointSnapshot jointSnapshot;

    private Camera m_cam;
    private float distToCamera;

    // --- BIẾN ĐỂ KHÓA VỊ TRÍ & GÓC ---
    private float startZ;
    private Quaternion startRotation;

    // --- BIẾN LƯU TRẠNG THÁI CŨ ---
    private float oldDrag;
    private float oldAngularDrag;
    private bool oldUseGravity;
    private bool wasKinematic;
    private RigidbodyConstraints oldConstraints;
    private RigidbodyInterpolation oldInterpolation;

    private RagdollPuppetMaster puppetMaster;

    void Start()
    {
        m_cam = Camera.main;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) StartDrag();
        if (Input.GetMouseButtonUp(0)) StopDrag();

        if (draggedRb != null)
        {
            draggedRb.rotation = startRotation;
        }
    }

    void FixedUpdate()
    {
        OnDrag();
    }
    public void OnDrag()
    {
        if (mouseJoint != null && draggedRb != null)
        {
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = distToCamera;
            Vector3 worldPos = m_cam.ScreenToWorldPoint(mousePos);

            worldPos.z = startZ;
            mouseJoint.connectedAnchor = worldPos;

            Vector3 currentPos = draggedRb.position;
            currentPos.z = startZ;
            draggedRb.position = currentPos;

            draggedRb.MoveRotation(startRotation);

            if (drawRope) Debug.DrawLine(draggedRb.position, worldPos, ropeColor);
        }
    }

    void StartDrag()
    {
        Ray ray = m_cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (!hit.collider.CompareTag(bodyTag)) return;

            if (hit.rigidbody != null)
            {
                draggedRb = hit.rigidbody;

                // [THÊM MỚI] Lấy khớp xương và chụp lại trạng thái chuẩn ban đầu
                draggedJoint = draggedRb.GetComponent<ConfigurableJoint>();
                if (draggedJoint != null)
                {
                    jointSnapshot = new JointSnapshot();
                    jointSnapshot.Capture(draggedJoint);
                }

                // Lưu trạng thái
                startZ = draggedRb.position.z;
                startRotation = draggedRb.rotation;

                oldDrag = draggedRb.linearDamping;
                oldAngularDrag = draggedRb.angularDamping;
                oldUseGravity = draggedRb.useGravity;
                oldConstraints = draggedRb.constraints;
                wasKinematic = draggedRb.isKinematic;
                oldInterpolation = draggedRb.interpolation;

                // Mở khóa Kinematic
                if (draggedRb.isKinematic) draggedRb.isKinematic = false;

                // Setup vật lý kéo
                draggedRb.useGravity = false;
                draggedRb.linearDamping = 20f;
                draggedRb.angularDamping = 100f;
                draggedRb.interpolation = RigidbodyInterpolation.Interpolate;

                // Khóa Z và Xoay
                draggedRb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;

                puppetMaster = draggedRb.transform.root.GetComponentInChildren<RagdollPuppetMaster>();
                if (puppetMaster != null) puppetMaster.RelaxMuscle(draggedRb);

                mouseJoint = draggedRb.gameObject.AddComponent<SpringJoint>();
                mouseJoint.autoConfigureConnectedAnchor = false;
                mouseJoint.anchor = Vector3.zero;

                Vector3 screenPos = m_cam.WorldToScreenPoint(hit.point);
                distToCamera = screenPos.z;

                mouseJoint.spring = force;
                mouseJoint.damper = damper;
                mouseJoint.maxDistance = distance;
            }
        }
    }

    void StopDrag()
    {
        if (mouseJoint != null)
        {
            Destroy(mouseJoint);
            mouseJoint = null;
        }

        if (draggedRb != null)
        {
            // 1. Dừng ngay lập tức
            draggedRb.linearVelocity = Vector3.zero;
            draggedRb.angularVelocity = Vector3.zero;

            // Set vị trí chuẩn lần cuối
            draggedRb.rotation = startRotation;
            Vector3 finalPos = draggedRb.position;
            finalPos.z = startZ;
            draggedRb.position = finalPos;

            // Tắt Interpolation
            draggedRb.interpolation = RigidbodyInterpolation.None;

            // [THÊM MỚI] Hồi phục khớp xương về trạng thái cũ để hết biến dạng
            if (draggedJoint != null)
            {
                jointSnapshot.Restore(draggedJoint);
            }

            // Xử lý khóa lại (Freeze)
            if (wasKinematic)
            {
                draggedRb.isKinematic = false;
                draggedRb.useGravity = false;
                draggedRb.constraints = RigidbodyConstraints.FreezeAll;
            }
            else
            {
                draggedRb.useGravity = oldUseGravity;
                draggedRb.constraints = oldConstraints;
            }

            draggedRb.linearDamping = oldDrag;
            draggedRb.angularDamping = oldAngularDrag;

            // Reset biến
            draggedRb = null;
            draggedJoint = null; // Reset joint
        }
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