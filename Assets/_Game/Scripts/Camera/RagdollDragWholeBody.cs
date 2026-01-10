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
        public Vector3 initialPosition; // [CHANGE] Thêm biến lưu vị trí gốc
    }

    private Dictionary<Rigidbody, OriginalState> stateMemory = new Dictionary<Rigidbody, OriginalState>();

    private SpringJoint mouseJoint;
    private SpringJoint returnJoint;
    private Rigidbody draggedRb;

    private Vector3 originalWorldPos; // Biến này sẽ lấy giá trị từ Dictionary ra

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

    public void DisableSnapBack()
    {
        canSnapBack = false;
        if (returnRoutine != null)
        {
            StopCoroutine(returnRoutine);
            returnRoutine = null;
        }
        if (returnJoint != null)
        {
            Destroy(returnJoint);
            returnJoint = null;
        }
        Debug.Log("Đã tắt chế độ giật về (Win State)");
    }

    public void EnableSnapBack()
    {
        canSnapBack = true;
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

                // [CHANGE] LOGIC QUAN TRỌNG Ở ĐÂY
                // Kiểm tra xem Dictionary đã có thằng này chưa
                if (!stateMemory.ContainsKey(draggedRb))
                {
                    // Nếu chưa có (Lần đầu tiên chạm vào), thì lưu lại
                    OriginalState newState = new OriginalState();
                    newState.drag = draggedRb.linearDamping;
                    newState.angularDrag = draggedRb.angularDamping;
                    newState.initialPosition = draggedRb.position; // Lưu vị trí hiện tại làm vị trí GỐC

                    stateMemory.Add(draggedRb, newState);

                    // Chỉ chụp snapshot Joint lần đầu tiên (nếu cần thiết logic này cũng chỉ nên chạy 1 lần)
                    draggedJoint = draggedRb.GetComponent<ConfigurableJoint>();
                    if (draggedJoint != null)
                    {
                        jointSnapshot = new JointSnapshot();
                        jointSnapshot.Capture(draggedJoint);
                    }
                }

                // [CHANGE] Lấy vị trí gốc từ Dictionary ra, thay vì lấy vị trí hiện tại
                originalWorldPos = stateMemory[draggedRb].initialPosition;

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
            if (canSnapBack)
            {
                returnRoutine = StartCoroutine(SpringSnapBackRoutine(draggedRb));
            }
            else
            {
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
            // Không restore position ở đây vì đây là lúc thả tay ra
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

        // originalWorldPos lúc này đã là vị trí được lưu từ lần đầu tiên
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
            // originalWorldPos này giờ luôn cố định ở vị trí lần đầu tiên
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
    public void OnDeactive()
    {
        stateMemory.Clear();
        canSnapBack = true;
        this.enabled = false;
    }
}