using UnityEngine;
using System.Collections;

public class RagdollDragBodyOnly : MonoBehaviour, IDrag
{
    [Header("--- CẤU HÌNH ---")]
    [Tooltip("Tên bộ phận muốn kéo (thường là Spine1 hoặc Hips)")]
    public string targetBoneName = "Spine1";

    public DragType DragType => DragType.Body;

    [Header("--- LỰC KÉO CHUỘT ---")]
    public float force = 5000f;
    public float damper = 100f;
    public float distance = 0.05f;

    [Header("--- DEBUG ---")]
    public bool drawRope = true;

    // Các biến nội bộ
    private SpringJoint mouseJoint;
    private Rigidbody draggedRb;
    private Camera m_cam;
    private float distToCamera;

    private RagdollPuppetMaster puppetMaster;
    private CameraFollow m_cameraFollow;

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
            if (hit.collider.CompareTag("Head")) return;
            Rigidbody hitRb = hit.rigidbody;
            if (hitRb != null)
            {
                // --- SỬA LỖI Ở ĐÂY ---
                // Thay vì tìm từ root (gốc to nhất), ta tìm từ hitRb tìm ngược lên cha của nó.
                // Như vậy bấm vào B sẽ ra PuppetMaster của B, bấm A ra A.
                puppetMaster = hitRb.GetComponentInParent<RagdollPuppetMaster>();

                if (puppetMaster != null)
                {
                    puppetMaster.LoosenPin();
                    puppetMaster.RelaxMuscle(hitRb);
                }

                // Xác định xương cần kéo
                Rigidbody foundSpine = null;
                // Cũng sửa chỗ này luôn cho an toàn: tìm trong các con của PuppetMaster tìm thấy
                Transform searchScope = (puppetMaster != null) ? puppetMaster.transform : hitRb.transform.root;

                Rigidbody[] allRbs = searchScope.GetComponentsInChildren<Rigidbody>();
                foreach (var rb in allRbs)
                {
                    if (rb.name.Contains(targetBoneName))
                    {
                        foundSpine = rb;
                        break;
                    }
                }
                draggedRb = foundSpine != null ? foundSpine : hitRb;

                // Tạo dây kéo chuột
                draggedRb.isKinematic = false;
                mouseJoint = draggedRb.gameObject.AddComponent<SpringJoint>();
                mouseJoint.autoConfigureConnectedAnchor = false;
                mouseJoint.anchor = Vector3.zero;

                Vector3 screenPos = m_cam.WorldToScreenPoint(hit.point);
                distToCamera = screenPos.z;

                mouseJoint.spring = force * draggedRb.mass;
                mouseJoint.damper = damper * draggedRb.mass;
                mouseJoint.maxDistance = distance;
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

        if (puppetMaster != null && draggedRb != null)
        {
            puppetMaster.TightenPin();
            puppetMaster.StiffenMuscle(draggedRb);
        }

        draggedRb = null;
        puppetMaster = null; // Reset biến này để không bị nhớ nhầm sang lần sau
    }

    void OnDrawGizmos()
    {
        if (!drawRope) return;

        if (mouseJoint != null && draggedRb != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(draggedRb.position, mouseJoint.connectedAnchor);
        }
    }

    public void OnActive() { this.enabled = true; }
    public void OnDeactive() { this.enabled = false; }
    public void DisableSnapBack() { if (puppetMaster != null) puppetMaster.StopReturning(); }
    public void EnableSnapBack() { }
}