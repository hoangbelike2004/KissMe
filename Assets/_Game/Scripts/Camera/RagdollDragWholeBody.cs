using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RagdollDragBodyOnly : MonoBehaviour, IDrag
{
    [Header("--- CẤU HÌNH ---")]
    [Tooltip("Tag của bộ phận Ragdoll (VD: BodyPart)")]
    public string bodyTag = "BodyPart";
    [Tooltip("Tên chính xác của bộ phận muốn điều khiển")]
    public string targetBoneName = "Spine1";

    public DragType DragType => DragType.Body;

    [Header("--- TRẠNG THÁI ---")]
    [Tooltip("Biến này quyết định có giật về hay không. Gọi hàm DisableSnapBack() để tắt nó.")]
    public bool canSnapBack = true;

    [Header("--- LỰC KÉO CHUỘT (DRAG) ---")]
    public float force = 5000f;
    public float damper = 100f;
    public float distance = 0.05f;

    [Header("--- LỰC GIẬT VỀ (RETURN SPRING) ---")]
    public float snapForce = 2000f;
    public float snapDamper = 10f;
    public float returnThreshold = 0.05f;

    [Header("--- DEBUG ---")]
    public bool drawRope = true;
    public Color ropeColor = Color.green;

    // --- CẤU TRÚC LƯU TRỮ ---
    private class OriginalState
    {
        public float drag;
        public float angularDrag;
    }

    private Dictionary<Rigidbody, OriginalState> stateMemory = new Dictionary<Rigidbody, OriginalState>();

    private SpringJoint mouseJoint;
    private SpringJoint returnJoint;
    private Rigidbody draggedRb;

    private Vector3 originalWorldPos;

    private ConfigurableJoint draggedJoint;
    private JointSnapshot jointSnapshot;

    private Camera m_cam;
    private float distToCamera;
    private RagdollPuppetMaster puppetMaster;
    private CameraFollow m_cameraFollow;
    private Coroutine returnRoutine;

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

    // --- HÀM BẠN CẦN GỌI KHI CHIẾN THẮNG ---
    // Gọi hàm này: GetComponent<RagdollDragBodyOnly>().DisableSnapBack();
    public void DisableSnapBack()
    {
        // 1. Tắt biến cờ để các lần thả tay sau không bị giật về
        canSnapBack = false;

        // 2. Nếu đang trong quá trình giật về (Routine đang chạy) -> Ngắt ngay lập tức
        if (returnRoutine != null)
        {
            StopCoroutine(returnRoutine);
            returnRoutine = null;
        }

        // 3. Cắt dây lò xo giật về nếu đang có
        if (returnJoint != null)
        {
            Destroy(returnJoint);
            returnJoint = null;
        }

        // 4. Nếu đang giữ chuột (mouseJoint), ta vẫn giữ nguyên để người chơi kéo nốt
        // Nhưng khi thả tay ra, nó sẽ lọt vào logic 'StopDragAndDrop'
        Debug.Log("Đã tắt chế độ giật về (Win State)");
    }

    // Hàm để bật lại nếu cần (ví dụ Replay)
    public void EnableSnapBack()
    {
        canSnapBack = true;
    }
    // ------------------------------------------

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
            if (hit.rigidbody != null)
            {
                if (returnRoutine != null) StopCoroutine(returnRoutine);
                if (returnJoint != null) Destroy(returnJoint);

                Rigidbody hitRb = hit.rigidbody;
                Rigidbody foundSpine = null;

                Transform characterScope = hitRb.transform;
                Animator myAnim = hitRb.GetComponentInParent<Animator>();
                if (myAnim != null) characterScope = myAnim.transform;
                else
                {
                    var myPuppet = hitRb.GetComponentInParent<RagdollPuppetMaster>();
                    if (myPuppet != null) characterScope = myPuppet.transform;
                }

                Rigidbody[] allRbs = characterScope.GetComponentsInChildren<Rigidbody>();
                foreach (var rb in allRbs)
                {
                    if (rb.name.Contains(targetBoneName))
                    {
                        foundSpine = rb;
                        break;
                    }
                }

                if (foundSpine != null) draggedRb = foundSpine;
                else draggedRb = hitRb;

                originalWorldPos = draggedRb.position;

                if (!stateMemory.ContainsKey(draggedRb))
                {
                    OriginalState newState = new OriginalState();
                    newState.drag = draggedRb.linearDamping;
                    newState.angularDrag = draggedRb.angularDamping;
                    stateMemory.Add(draggedRb, newState);

                    draggedJoint = draggedRb.GetComponent<ConfigurableJoint>();
                    if (draggedJoint != null)
                    {
                        jointSnapshot = new JointSnapshot();
                        jointSnapshot.Capture(draggedJoint);
                    }
                }

                draggedRb.isKinematic = false;
                draggedRb.WakeUp();

                puppetMaster = draggedRb.transform.root.GetComponentInChildren<RagdollPuppetMaster>();
                if (puppetMaster != null) puppetMaster.RelaxMuscle(draggedRb);

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

        if (draggedRb != null)
        {
            // Kiểm tra biến cờ canSnapBack
            if (canSnapBack)
            {
                returnRoutine = StartCoroutine(SpringSnapBackRoutine(draggedRb));
            }
            else
            {
                // Nếu đã gọi DisableSnapBack(), thả rơi tự do
                StopDragAndDrop(draggedRb);
            }
        }
    }

    void StopDragAndDrop(Rigidbody rb)
    {
        if (draggedJoint != null && jointSnapshot != null)
        {
            jointSnapshot.Restore(draggedJoint);
        }

        if (stateMemory.ContainsKey(rb))
        {
            rb.linearDamping = stateMemory[rb].drag;
            rb.angularDamping = stateMemory[rb].angularDrag;
        }

        if (puppetMaster != null) puppetMaster.StiffenMuscle(rb);

        draggedRb = null;
        returnJoint = null;
    }

    IEnumerator SpringSnapBackRoutine(Rigidbody rb)
    {
        returnJoint = rb.gameObject.AddComponent<SpringJoint>();
        returnJoint.autoConfigureConnectedAnchor = false;
        returnJoint.anchor = Vector3.zero;
        returnJoint.connectedAnchor = originalWorldPos;

        returnJoint.spring = snapForce * rb.mass;
        returnJoint.damper = snapDamper * rb.mass;
        returnJoint.minDistance = 0;
        returnJoint.maxDistance = 0;
        returnJoint.tolerance = 0;

        float timer = 0f;
        while (timer < 5f)
        {
            timer += Time.deltaTime;

            // Kiểm tra liên tục: Nếu giữa chừng bị gọi DisableSnapBack -> Dừng luôn
            if (!canSnapBack)
            {
                if (returnJoint != null) Destroy(returnJoint);
                StopDragAndDrop(rb);
                yield break;
            }

            if (Vector3.Distance(rb.position, originalWorldPos) < returnThreshold
                && rb.linearVelocity.magnitude < 0.2f)
            {
                break;
            }
            yield return null;
        }

        if (returnJoint != null) Destroy(returnJoint);

        // Chỉ snap cứng vị trí nếu vẫn được phép snap back
        if (canSnapBack)
        {
            rb.position = originalWorldPos;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        StopDragAndDrop(rb);
    }

    void OnDrawGizmos()
    {
        if (!drawRope) return;

        if (mouseJoint != null && draggedRb != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(draggedRb.position, mouseJoint.connectedAnchor);
            Gizmos.color = new Color(1, 0, 0, 0.5f);
            Gizmos.DrawSphere(originalWorldPos, 0.1f);
        }

        if (canSnapBack && returnJoint != null && draggedRb != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(originalWorldPos, 0.2f);
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(draggedRb.position, originalWorldPos);
        }
    }

    public void OnActive() { this.enabled = true; }
    public void OnDeactive() { this.enabled = false; }
}