using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraDragRagdollHead_FreezeBody : MonoBehaviour
{
    [Header("Drag Settings")]
    [SerializeField] private float dragForce = 150f;
    [SerializeField] private float maxDragDistance = 3f;

    [Header("Damping While Dragging")]
    [SerializeField] private float headLinearDamping = 10f;
    [SerializeField] private float headAngularDamping = 10f;

    [Header("Body To Freeze (NOT include Head)")]
    [SerializeField] private Rigidbody[] bodyRigidbodies;

    private Camera cam;
    private Rigidbody headRb;
    private bool isDragging;
    private float dragDepth;

    // lưu damping gốc
    private float originalLinearDamping;
    private float originalAngularDamping;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Update()
    {
        HandleInput();
    }

    private void FixedUpdate()
    {
        if (isDragging && headRb != null)
        {
            DragHead();
        }
    }

    // ================= INPUT =================

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
            TryPickHead();

        if (Input.GetMouseButtonUp(0))
            ReleaseHead();
    }

    private void TryPickHead()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit))
            return;

        if (!hit.transform.CompareTag("Head"))
            return;

        headRb = hit.rigidbody;
        if (headRb == null)
            return;

        dragDepth = Vector3.Distance(
            cam.transform.position,
            headRb.position
        );

        // lưu damping gốc
        originalLinearDamping = headRb.linearDamping;
        originalAngularDamping = headRb.angularDamping;

        // giảm rung đầu
        headRb.linearDamping = headLinearDamping;
        headRb.angularDamping = headAngularDamping;

        // 🔒 FREEZE BODY (CỰC KỲ QUAN TRỌNG)
        foreach (var rb in bodyRigidbodies)
        {
            if (rb != null)
                rb.isKinematic = true;
        }

        isDragging = true;
    }

    private void DragHead()
    {
        Vector3 mouseWorldPos = cam.ScreenToWorldPoint(
            new Vector3(
                Input.mousePosition.x,
                Input.mousePosition.y,
                dragDepth
            )
        );

        Vector3 forceDir = mouseWorldPos - headRb.position;

        if (forceDir.magnitude > maxDragDistance)
            forceDir = forceDir.normalized * maxDragDistance;

        headRb.AddForce(forceDir * dragForce, ForceMode.Force);
    }

    private void ReleaseHead()
    {
        if (headRb != null)
        {
            headRb.linearDamping = originalLinearDamping;
            headRb.angularDamping = originalAngularDamping;
        }

        // 🔓 UNFREEZE BODY
        foreach (var rb in bodyRigidbodies)
        {
            if (rb != null)
                rb.isKinematic = false;
        }

        headRb = null;
        isDragging = false;
    }
}
