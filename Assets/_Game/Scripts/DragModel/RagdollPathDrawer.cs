using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class RagdollPathDrawer : MonoBehaviour
{
    [Header("--- KẾT NỐI ---")]
    public RagdollPuppetMaster puppetMaster;

    [Tooltip("Chọn Layer của SÀN NHÀ (Ví dụ: 'Ground').")]
    public LayerMask drawLayer;

    [Header("--- CẤU HÌNH VẼ ---")]
    public float minDistanceBetweenPoints = 0.1f;
    public float lineOffset = 0.05f;

    [Header("--- CẤU HÌNH DI CHUYỂN ---")]
    public float moveSpeed = 5f;
    public float stoppingDistance = 0.1f;

    // --- BIẾN NỘI BỘ ---
    private LineRenderer lineRenderer;
    private List<Vector3> pathPoints = new List<Vector3>();
    private Camera mainCam;

    private CameraFollow cameraFollow;
    private bool isMoving = false;
    private Vector3 currentAnchorPos;

    void Start()
    {
        mainCam = Camera.main;
        if (mainCam != null) cameraFollow = mainCam.GetComponent<CameraFollow>();

        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 0;
        lineRenderer.useWorldSpace = true;

        if (puppetMaster != null) currentAnchorPos = puppetMaster.GetAnchorPosition();
    }

    void Update()
    {
        HandleInput();
    }

    void FixedUpdate()
    {
        MoveAlongPath();
    }

    void LateUpdate()
    {
        if (cameraFollow != null) cameraFollow.UpdateCam();
    }

    void HandleInput()
    {
        // 1. NHẤN CHUỘT: Tìm điểm bắt đầu dưới mặt đất
        if (Input.GetMouseButtonDown(0))
        {
            isMoving = false;
            pathPoints.Clear();
            lineRenderer.positionCount = 0;

            if (puppetMaster != null)
            {
                // Lấy vị trí Neo thực (để di chuyển)
                currentAnchorPos = puppetMaster.GetAnchorPosition();

                // --- [SỬA LỖI ĐỘ CAO TẠI ĐÂY] ---
                Vector3 visualStartPoint = currentAnchorPos;

                // Bắn Raycast từ vị trí nhân vật thẳng xuống đất
                RaycastHit groundHit;
                // Bắn từ cao hơn đầu nhân vật 1 chút (currentAnchorPos + Vector3.up) xuống dưới
                if (Physics.Raycast(currentAnchorPos + Vector3.up, Vector3.down, out groundHit, 100f, drawLayer))
                {
                    // Tìm thấy đất -> Lấy điểm va chạm + offset
                    visualStartPoint = groundHit.point + (groundHit.normal * lineOffset);
                }
                else
                {
                    // Không thấy đất (trường hợp hiếm) -> Ép độ cao bằng offset
                    visualStartPoint.y = lineOffset;
                }

                // Thêm điểm đã được hạ thấp xuống đất vào danh sách
                pathPoints.Add(visualStartPoint);

                lineRenderer.positionCount = 1;
                lineRenderer.SetPosition(0, visualStartPoint);
            }
        }
        // 2. GIỮ CHUỘT: Vẽ tiếp
        else if (Input.GetMouseButton(0))
        {
            Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, drawLayer))
            {
                Vector3 rawPoint = hit.point;
                Vector3 finalPoint = rawPoint + (hit.normal * lineOffset);

                if (pathPoints.Count == 0 || Vector3.Distance(pathPoints[pathPoints.Count - 1], finalPoint) > minDistanceBetweenPoints)
                {
                    pathPoints.Add(finalPoint);
                    lineRenderer.positionCount = pathPoints.Count;
                    lineRenderer.SetPositions(pathPoints.ToArray());
                }
            }
        }
        // 3. THẢ CHUỘT: Di chuyển
        else if (Input.GetMouseButtonUp(0))
        {
            if (pathPoints.Count > 0)
            {
                isMoving = true;
                if (puppetMaster != null)
                {
                    puppetMaster.EnableReturning();
                    currentAnchorPos = puppetMaster.GetAnchorPosition();
                }
            }
        }
    }

    void MoveAlongPath()
    {
        if (!isMoving || pathPoints.Count == 0 || puppetMaster == null) return;

        // Lấy điểm đích tiếp theo (Điểm này đang nằm sát đất do logic vẽ ở trên)
        Vector3 rawTarget = pathPoints[0];

        // --- KHÓA TRỤC Y ---
        // Khi di chuyển, ta KHÔNG dùng độ cao của điểm vẽ (vì nó sát đất)
        // Mà ta dùng độ cao của Neo hiện tại (currentAnchorPos.y) để nhân vật không bị chúi đầu xuống
        Vector3 targetWithFixedY = new Vector3(rawTarget.x, currentAnchorPos.y, rawTarget.z);

        // Di chuyển Neo ảo
        currentAnchorPos = Vector3.MoveTowards(currentAnchorPos, targetWithFixedY, moveSpeed * Time.fixedDeltaTime);

        // Cập nhật vị trí Neo THẬT
        puppetMaster.SetAnchorPosition(currentAnchorPos);

        if (Vector3.Distance(currentAnchorPos, targetWithFixedY) < stoppingDistance)
        {
            pathPoints.RemoveAt(0);

            lineRenderer.positionCount = pathPoints.Count;
            lineRenderer.SetPositions(pathPoints.ToArray());

            if (pathPoints.Count == 0)
            {
                isMoving = false;
            }
        }
    }

    public void ResetDrawing()
    {
        isMoving = false;
        pathPoints.Clear();
        lineRenderer.positionCount = 0;

        if (puppetMaster != null)
        {
            currentAnchorPos = puppetMaster.GetAnchorPosition();
        }
    }
}