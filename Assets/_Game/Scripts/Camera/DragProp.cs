using UnityEngine;

public class DragProp : MonoBehaviour, IDrag
{
    public DragType DragType => DragType.Prop;

    [Header("--- CẤU HÌNH ---")]
    public string propTag = "Prop";
    // Force và Damper mặc định (sẽ tự nhân với khối lượng Mass)
    public float force = 500f;
    public float damper = 50f;

    [Header("--- DEBUG ---")]
    public bool drawRope = true;
    public Color ropeColor = Color.yellow;

    private Camera m_cam;
    private SpringJoint mouseJoint;
    private Rigidbody draggedRb;
    private float distanceToCamera;

    // Biến lưu thông số cũ
    private float oldDrag;
    private float oldAngularDrag;
    private RigidbodyInterpolation oldInterpolation;

    void Start()
    {
        m_cam = Camera.main;
    }

    void Update()
    {
        // Update chỉ dùng để bắt sự kiện Click (Input)
        if (Input.GetMouseButtonDown(0)) StartDrag();
        if (Input.GetMouseButtonUp(0)) StopDrag();
    }

    void FixedUpdate()
    {
        // Gọi OnDrag ở đây để đảm bảo lò xo được cập nhật theo chu kỳ vật lý
        // (Nếu Controller của bạn đã gọi dragHandlers[i].OnDrag() trong FixedUpdate của nó rồi thì bạn có thể xóa dòng này)
        OnDrag();
    }

    // --- LOGIC VẬT LÝ CHÍNH NẰM Ở ĐÂY ---
    public void OnDrag()
    {
        if (mouseJoint == null || draggedRb == null) return;

        // 1. Tính toán vị trí chuột trong không gian 3D
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = distanceToCamera; // Giữ nguyên khoảng cách Z ban đầu
        Vector3 worldTarget = m_cam.ScreenToWorldPoint(mousePos);

        // 2. Cập nhật vị trí neo của lò xo
        mouseJoint.connectedAnchor = worldTarget;

        // 3. Vẽ dây Debug (nếu cần)
        if (drawRope)
        {
            Vector3 anchorPos = draggedRb.transform.TransformPoint(mouseJoint.anchor);
            Debug.DrawLine(anchorPos, worldTarget, ropeColor);
        }
    }

    void StartDrag()
    {
        Ray ray = m_cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (!hit.collider.CompareTag(propTag)) return;
            if (hit.rigidbody == null) return;

            draggedRb = hit.rigidbody;

            // --- LƯU TRẠNG THÁI CŨ ---
            oldDrag = draggedRb.linearDamping;
            oldAngularDrag = draggedRb.angularDamping;
            oldInterpolation = draggedRb.interpolation;

            // Tính khoảng cách Z chuẩn từ Camera đến ĐIỂM CLICK
            distanceToCamera = Vector3.Distance(m_cam.transform.position, hit.point);

            // --- SETUP VẬT LÝ MỚI ---
            // Tăng Drag để vật đi đầm hơn, không bị trôi
            draggedRb.linearDamping = 10f;
            draggedRb.angularDamping = 10f;
            draggedRb.interpolation = RigidbodyInterpolation.Interpolate;

            // --- TẠO LÒ XO ---
            mouseJoint = draggedRb.gameObject.AddComponent<SpringJoint>();
            mouseJoint.autoConfigureConnectedAnchor = false;

            // Neo lò xo vào đúng điểm click trên vật (Local)
            mouseJoint.anchor = draggedRb.transform.InverseTransformPoint(hit.point);

            // [QUAN TRỌNG 1] Set ngay đích đến ban đầu để tránh bị giật khung hình đầu tiên
            mouseJoint.connectedAnchor = hit.point;

            // [QUAN TRỌNG 2] Nhân lực với Khối lượng (Mass) để kéo vật nặng nhẹ đều mượt như nhau
            mouseJoint.spring = force * draggedRb.mass;
            mouseJoint.damper = damper * draggedRb.mass;

            // Giữ dây căng
            mouseJoint.maxDistance = 0.02f;
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
            // --- CẮT ĐUÔI QUÁN TÍNH ---
            // Dừng ngay lập tức để không bị trôi khi thả tay
            draggedRb.linearVelocity = Vector3.zero;
            draggedRb.angularVelocity = Vector3.zero;

            // Trả lại trạng thái cũ
            draggedRb.linearDamping = oldDrag;
            draggedRb.angularDamping = oldAngularDrag;
            draggedRb.interpolation = oldInterpolation;

            draggedRb = null;
        }
    }

    // --- INTERFACE ---
    public void OnActive()
    {
        this.enabled = true;
    }

    public void OnDeactive()
    {
        this.enabled = false;
    }
    void OnEnable()
    {
        Observer.OnStopDragProp += StopDrag;
    }

    void OnDisable()
    {
        Observer.OnStopDragProp -= StopDrag;
    }
}