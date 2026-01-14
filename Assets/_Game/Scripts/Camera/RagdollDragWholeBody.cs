using UnityEngine;
using System.Collections;

public class RagdollDragBodyOnly : MonoBehaviour, IDrag
{
    public DragType DragType => DragType.Body;

    [Header("--- LỰC KÉO CHUỘT ---")]
    public float baseForce = 1000f;
    public float damper = 50f;

    [Header("--- ỔN ĐỊNH VẬT LÝ ---")]
    public float dragDamping = 10f;
    public float angularDamping = 10f;

    [Header("--- DEBUG ---")]
    public bool drawRope = true;

    // Các biến nội bộ
    private SpringJoint mouseJoint;
    private Rigidbody draggedRb;

    // Lưu trữ khớp xương đang bị kéo để xử lý
    private ConfigurableJoint draggedInternalJoint;
    private ConfigurableJointMotion oldAngX, oldAngY, oldAngZ; // Lưu trạng thái giới hạn cũ

    private Camera m_cam;
    private float distToCamera;

    private RagdollPuppetMaster puppetMaster;
    private CameraFollow m_cameraFollow;

    // Lưu trạng thái Rigidbody cũ
    private float oldDrag;
    private float oldAngularDrag;
    private bool oldIsKinematic;
    private int oldSolverIterations;

    private void Start()
    {
        m_cam = Camera.main;
        m_cameraFollow = GetComponent<CameraFollow>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) StartDrag();
        if (Input.GetMouseButtonUp(0)) StopDrag();
    }

    void FixedUpdate()
    {
        OnDrag();
    }

    void LateUpdate()
    {
        if (m_cameraFollow != null) m_cameraFollow.UpdateCam();
    }

    public void OnDrag()
    {
        if (mouseJoint != null && draggedRb != null)
        {
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = distToCamera;
            Vector3 worldPos = m_cam.ScreenToWorldPoint(mousePos);
            mouseJoint.connectedAnchor = worldPos;
        }
    }

    void StartDrag()
    {
        Ray ray = m_cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (!hit.collider.CompareTag("Untagged")) return;

            Rigidbody hitRb = hit.rigidbody;
            if (hitRb != null)
            {
                // 1. Setup PuppetMaster
                puppetMaster = hitRb.GetComponentInParent<RagdollPuppetMaster>();
                if (puppetMaster != null)
                {
                    puppetMaster.LoosenPin();
                    puppetMaster.RelaxMuscle(hitRb);
                }

                draggedRb = hitRb;

                // 2. Lưu & Tăng chỉ số Solver (Giúp tính toán va chạm mượt hơn, giảm giật)
                oldSolverIterations = draggedRb.solverIterations;
                draggedRb.solverIterations = 20; // Tăng độ chính xác khi đang kéo

                // 3. Setup Ma sát (Như code trước)
                oldDrag = draggedRb.linearDamping;
                oldAngularDrag = draggedRb.angularDamping;
                oldIsKinematic = draggedRb.isKinematic;

                draggedRb.linearDamping = dragDamping;
                draggedRb.angularDamping = angularDamping;
                draggedRb.isKinematic = false;
                draggedRb.interpolation = RigidbodyInterpolation.Interpolate;

                // 4. [FIX QUAN TRỌNG] "Tháo khớp" tạm thời
                // Nếu khớp bị giới hạn (Limited), khi kéo quá tay nó sẽ bị giật.
                // Ta tạm thời cho nó xoay tự do (Free) để đi theo chuột, xong rồi khóa lại sau.
                draggedInternalJoint = draggedRb.GetComponent<ConfigurableJoint>();
                if (draggedInternalJoint != null)
                {
                    // Lưu trạng thái cũ
                    oldAngX = draggedInternalJoint.angularXMotion;
                    oldAngY = draggedInternalJoint.angularYMotion;
                    oldAngZ = draggedInternalJoint.angularZMotion;

                    // Mở khóa hoàn toàn trục xoay để không bị kẹt giới hạn
                    draggedInternalJoint.angularXMotion = ConfigurableJointMotion.Free;
                    draggedInternalJoint.angularYMotion = ConfigurableJointMotion.Free;
                    draggedInternalJoint.angularZMotion = ConfigurableJointMotion.Free;
                }

                // 5. Tạo dây kéo
                mouseJoint = draggedRb.gameObject.AddComponent<SpringJoint>();
                mouseJoint.autoConfigureConnectedAnchor = false;
                mouseJoint.anchor = draggedRb.transform.InverseTransformPoint(hit.point);

                // [FIX QUAN TRỌNG] Mass Scale
                // Giúp lò xo xử lý tốt việc vật nhẹ (tay) bị kéo bởi vật nặng vô hình (chuột)
                mouseJoint.massScale = 1f;
                mouseJoint.connectedMassScale = 1f;

                Vector3 screenPos = m_cam.WorldToScreenPoint(hit.point);
                distToCamera = screenPos.z;

                float massFactor = Mathf.Max(draggedRb.mass, 1f);
                mouseJoint.spring = baseForce * massFactor;
                mouseJoint.damper = damper * massFactor;
                mouseJoint.maxDistance = 0f;

                mouseJoint.connectedAnchor = hit.point;
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

        // Khôi phục khớp xương về trạng thái ban đầu (Khóa giới hạn lại)
        if (draggedInternalJoint != null)
        {
            draggedInternalJoint.angularXMotion = oldAngX;
            draggedInternalJoint.angularYMotion = oldAngY;
            draggedInternalJoint.angularZMotion = oldAngZ;
            draggedInternalJoint = null;
        }

        if (draggedRb != null)
        {
            draggedRb.linearDamping = oldDrag;
            draggedRb.angularDamping = oldAngularDrag;
            draggedRb.isKinematic = oldIsKinematic;
            draggedRb.solverIterations = oldSolverIterations; // Trả lại setting cũ

            if (puppetMaster != null)
            {
                puppetMaster.TightenPin();
                puppetMaster.StiffenMuscle(draggedRb);
            }
        }

        draggedRb = null;
        puppetMaster = null;
    }

    void OnDrawGizmos()
    {
        if (!drawRope) return;

        if (mouseJoint != null && draggedRb != null)
        {
            Gizmos.color = Color.green;
            Vector3 anchorPos = draggedRb.transform.TransformPoint(mouseJoint.anchor);
            Gizmos.DrawLine(anchorPos, mouseJoint.connectedAnchor);
            Gizmos.DrawWireSphere(anchorPos, 0.05f);
        }
    }

    public void OnActive() { this.enabled = true; }
    public void OnDeactive() { this.enabled = false; }
    public void DisableSnapBack() { if (puppetMaster != null) puppetMaster.StopReturning(); }
    public void EnableSnapBack() { }
}