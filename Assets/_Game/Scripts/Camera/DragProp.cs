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
    private CameraFollow m_cameraFollow;

    private void Start()
    {
        m_cam = Camera.main;
        m_cameraFollow = GetComponent<CameraFollow>();
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
    void LateUpdate()
    {
        if (m_cameraFollow != null) m_cameraFollow.UpdateCam();
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
// using UnityEngine;

// public class DragProp : MonoBehaviour, IDrag
// {
//     public DragType DragType => DragType.Prop;

//     [Header("--- CẤU HÌNH ---")]
//     public string propTag = "Prop";

//     [Header("1. Cấu hình VẬT THƯỜNG (Normal)")]
//     public float normalForce = 2000f; // Lực mạnh để dính chuột
//     public float normalDrag = 10f;    // Drag cao để không trôi

//     [Header("2. Cấu hình VẬT DÂY THUN (Elastic)")]
//     public float elasticForce = 5000f; // [QUAN TRỌNG] Lực phải to gấp 10 lần lò xo cũ (5000 vs 500)
//     public float elasticDamper = 5f;   // Damper thấp để tay cầm có độ rung nhẹ

//     [Header("--- DEBUG ---")]
//     public bool drawRope = true;
//     public Color normalColor = Color.yellow;
//     public Color elasticColor = Color.cyan;

//     private Camera m_cam;
//     private SpringJoint mouseJoint; // Đây là cái dây của chuột
//     private Rigidbody draggedRb;
//     private float distanceToCamera;

//     // Biến lưu thông số cũ
//     private float oldDrag;
//     private float oldAngularDrag;
//     private RigidbodyInterpolation oldInterpolation;
//     private CameraFollow m_cameraFollow;

//     // Biến kiểm tra loại vật
//     private bool isElasticObject = false;

//     private void Start()
//     {
//         m_cam = Camera.main;
//         m_cameraFollow = GetComponent<CameraFollow>();
//     }

//     void Update()
//     {
//         if (Input.GetMouseButtonDown(0)) StartDrag();
//         if (Input.GetMouseButtonUp(0)) StopDrag();
//     }

//     void FixedUpdate()
//     {
//         OnDrag();
//     }

//     void LateUpdate()
//     {
//         if (m_cameraFollow != null) m_cameraFollow.UpdateCam();
//     }

//     public void OnDrag()
//     {
//         if (mouseJoint == null || draggedRb == null) return;

//         Vector3 mousePos = Input.mousePosition;
//         mousePos.z = distanceToCamera;
//         Vector3 worldTarget = m_cam.ScreenToWorldPoint(mousePos);

//         mouseJoint.connectedAnchor = worldTarget;

//         if (drawRope)
//         {
//             Vector3 anchorPos = draggedRb.transform.TransformPoint(mouseJoint.anchor);
//             Debug.DrawLine(anchorPos, worldTarget, isElasticObject ? elasticColor : normalColor);
//         }
//     }

//     void StartDrag()
//     {
//         Ray ray = m_cam.ScreenPointToRay(Input.mousePosition);
//         if (Physics.Raycast(ray, out RaycastHit hit))
//         {
//             if (!hit.collider.CompareTag(propTag)) return;
//             if (hit.rigidbody == null) return;

//             draggedRb = hit.rigidbody;

//             // --- 1. TỰ ĐỘNG PHÁT HIỆN DÂY THUN ---
//             // Kiểm tra xem vật có cái SpringJoint nào khác đang giữ nó không
//             SpringJoint[] existingJoints = draggedRb.GetComponents<SpringJoint>();
//             isElasticObject = existingJoints.Length > 0;

//             // Lưu thông số cũ
//             oldDrag = draggedRb.linearDamping;
//             oldAngularDrag = draggedRb.angularDamping;
//             oldInterpolation = draggedRb.interpolation;
//             distanceToCamera = Vector3.Distance(m_cam.transform.position, hit.point);

//             draggedRb.interpolation = RigidbodyInterpolation.Interpolate;

//             // --- 2. CẤU HÌNH DRAG (MA SÁT) ---
//             if (isElasticObject)
//             {
//                 // [LOẠI DÂY THUN] -> Giữ nguyên ma sát cũ (thường là 0) để nó trơn tru, dễ kéo
//                 draggedRb.linearDamping = oldDrag;
//                 draggedRb.angularDamping = oldAngularDrag;
//             }
//             else
//             {
//                 // [LOẠI THƯỜNG] -> Tăng ma sát để đầm tay
//                 draggedRb.linearDamping = normalDrag;
//                 draggedRb.angularDamping = normalDrag;
//             }

//             // --- 3. TẠO LÒ XO CHUỘT ---
//             mouseJoint = draggedRb.gameObject.AddComponent<SpringJoint>();
//             mouseJoint.autoConfigureConnectedAnchor = false;
//             mouseJoint.anchor = draggedRb.transform.InverseTransformPoint(hit.point);
//             mouseJoint.connectedAnchor = hit.point;

//             // --- 4. CẤU HÌNH LỰC KÉO (FIX LỖI KÉO KHÔNG ĐƯỢC) ---
//             if (isElasticObject)
//             {
//                 // [QUAN TRỌNG NHẤT]
//                 // Lực cực mạnh để thắng lò xo cũ
//                 mouseJoint.spring = elasticForce * draggedRb.mass;
//                 mouseJoint.damper = elasticDamper * draggedRb.mass;

//                 // [FIX LỖI CŨ CỦA BẠN Ở ĐÂY]
//                 // Trước đây để 10 -> Dây quá dài -> Lực không tác dụng
//                 // Bây giờ để 0.2 -> Chỉ cần nhích chuột là lực 5000 tác dụng ngay
//                 mouseJoint.maxDistance = 0.2f;
//             }
//             else
//             {
//                 mouseJoint.spring = normalForce * draggedRb.mass;
//                 mouseJoint.damper = 50f * draggedRb.mass;
//                 mouseJoint.maxDistance = 0.01f; // Dính sát
//             }
//         }
//     }

//     void StopDrag()
//     {
//         if (mouseJoint != null)
//         {
//             Destroy(mouseJoint);
//             mouseJoint = null;
//         }

//         if (draggedRb != null)
//         {
//             draggedRb.linearDamping = oldDrag;
//             draggedRb.angularDamping = oldAngularDrag;
//             draggedRb.interpolation = oldInterpolation;

//             // --- 5. XỬ LÝ KHI THẢ TAY ---
//             if (isElasticObject)
//             {
//                 // [DÂY THUN] -> KHÔNG reset vận tốc -> Để nó tự bay về
//             }
//             else
//             {
//                 // [THƯỜNG] -> Dừng lại ngay
//                 draggedRb.linearVelocity = Vector3.zero;
//                 draggedRb.angularVelocity = Vector3.zero;
//             }

//             draggedRb = null;
//         }
//     }

//     // --- INTERFACE ---
//     public void OnActive() { this.enabled = true; }
//     public void OnDeactive() { this.enabled = false; }
//     void OnEnable() { Observer.OnStopDragProp += StopDrag; }
//     void OnDisable() { Observer.OnStopDragProp -= StopDrag; }
// }