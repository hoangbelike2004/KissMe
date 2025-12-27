using UnityEngine;

public class RagdollDragger : MonoBehaviour
{
    [Header("Cấu hình Lực kéo chuột")]
    public float mouseSpringForce = 1000f;
    public float mouseDamper = 10f;
    public float dragDistance = 0.2f;

    [Header("Cấu hình Raycast")]
    public LayerMask draggableLayer;
    public float maxRayDistance = 100f;

    // Các biến nội bộ để xử lý logic
    private SpringJoint mouseJoint;             // Lò xo tạo ra bởi chuột
    private Rigidbody currentDraggedObject;     // Vật đang bị kéo
    private float objectDistanceFromCamera;

    // Biến lưu trữ trạng thái lò xo "núng nính" có sẵn của nhân vật
    private SpringJoint characterInternalSpring;
    private float originalCharacterSpringForce;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            StartDragging();
        }

        if (Input.GetMouseButton(0) && mouseJoint != null)
        {
            UpdateDragging();
        }

        if (Input.GetMouseButtonUp(0))
        {
            StopDragging();
        }
    }

    void StartDragging()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxRayDistance, draggableLayer))
        {
            if (hit.rigidbody != null)
            {
                currentDraggedObject = hit.rigidbody;
                objectDistanceFromCamera = Vector3.Distance(Camera.main.transform.position, currentDraggedObject.position);

                // --- XỬ LÝ HIỆU ỨNG NÚNG NÍNH ---
                // Kiểm tra xem bộ phận này có lò xo kết nối với thân không (SpringJoint)
                characterInternalSpring = currentDraggedObject.GetComponent<SpringJoint>();
                if (characterInternalSpring != null)
                {
                    // Lưu lại lực lò xo ban đầu
                    originalCharacterSpringForce = characterInternalSpring.spring;
                    // Tắt lực lò xo nội tại để chuột kéo nhẹ nhàng hơn
                    characterInternalSpring.spring = 0;
                }
                // ---------------------------------

                // Tạo lò xo kéo của chuột
                mouseJoint = currentDraggedObject.gameObject.AddComponent<SpringJoint>();
                mouseJoint.spring = mouseSpringForce;
                mouseJoint.damper = mouseDamper;
                mouseJoint.maxDistance = dragDistance;
                mouseJoint.autoConfigureConnectedAnchor = false;
                mouseJoint.anchor = currentDraggedObject.transform.InverseTransformPoint(hit.point);
                mouseJoint.connectedAnchor = GetMouseWorldPosition();
            }
        }
    }

    void UpdateDragging()
    {
        mouseJoint.connectedAnchor = GetMouseWorldPosition();
    }

    void StopDragging()
    {
        // 1. Xóa lò xo của chuột
        if (mouseJoint != null)
        {
            Destroy(mouseJoint);
            mouseJoint = null;
        }

        // 2. KÍCH HOẠT LẠI HIỆU ỨNG NÚNG NÍNH
        if (characterInternalSpring != null)
        {
            // Trả lại lực lò xo cũ -> Đầu sẽ tự bay về vị trí cũ
            characterInternalSpring.spring = originalCharacterSpringForce;

            // Reset biến
            characterInternalSpring = null;
        }

        currentDraggedObject = null;
    }

    Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = objectDistanceFromCamera;
        return Camera.main.ScreenToWorldPoint(mousePoint);
    }
}