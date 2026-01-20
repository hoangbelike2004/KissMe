using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class RagdollPathDrawer : MonoBehaviour
{
    public enum DrawMode
    {
        MovePuppet,
        ConnectObjects
    }

    [Header("--- CHẾ ĐỘ HOẠT ĐỘNG ---")]
    public DrawMode currentMode = DrawMode.MovePuppet;

    // List dùng để kiểm tra logic game (A nối với B)
    private List<Transform> tfs = new List<Transform>();

    [Header("--- KẾT NỐI (Mode MovePuppet) ---")]
    public RagdollPuppetMaster puppetMaster;

    [Tooltip("Layer để vẽ đường đi hoặc tìm vật thể.")]
    public LayerMask interactLayer;

    [Header("--- CẤU HÌNH LAYER MỤC TIÊU ---")]
    [Tooltip("Tên Layer bắt buộc phải trúng khi thả tay nối (Ví dụ: Head)")]
    public string targetLayerName = "Head"; // [THÊM MỚI] Biến chứa tên Layer

    [Header("--- CẤU HÌNH VẼ ---")]
    public float minDistanceBetweenPoints = 0.1f;
    public float lineOffset = 0.05f;
    [Tooltip("Độ rộng của đường vẽ")]
    public float lineWidth = 0.2f;

    [Header("--- CẤU HÌNH DI CHUYỂN (Mode MovePuppet) ---")]
    public float moveSpeed = 5f;
    public float stoppingDistance = 0.1f;

    [Header("--- CẤU HÌNH NỐI VẬT (Mode ConnectObjects) ---")]
    public float springStrength = 100f;
    public float springDamper = 0.2f;

    private LineRenderer lineRenderer;
    private Camera mainCam;
    private CameraFollow cameraFollow;

    // Dùng chung pathPoints cho cả 2 chế độ để vẽ nét liền
    private List<Vector3> pathPoints = new List<Vector3>();

    private bool isMoving = false;
    private Vector3 currentAnchorPos;

    // Biến kiểm tra xem đã bắt đầu vẽ chưa
    private bool isDrawingConnection = false;

    void Start()
    {
        mainCam = Camera.main;
        if (mainCam != null) cameraFollow = mainCam.GetComponent<CameraFollow>();

        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 0;
        lineRenderer.useWorldSpace = true;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;

        if (puppetMaster != null)
        {
            currentAnchorPos = puppetMaster.GetAnchorPosition();
        }
    }

    void Update()
    {
        if (currentMode == DrawMode.MovePuppet)
        {
            HandleInput_MovePuppet();
        }
        else if (currentMode == DrawMode.ConnectObjects)
        {
            HandleInput_ConnectObjects();
        }
    }

    void FixedUpdate()
    {
        if (currentMode == DrawMode.MovePuppet && puppetMaster != null)
        {
            MoveAlongPath();
        }
    }

    void LateUpdate()
    {
        if (cameraFollow != null) cameraFollow.UpdateCam();
    }

    // ========================================================================
    // LOGIC 2: CONNECT OBJECTS (CÓ KIỂM TRA LAYER "HEAD")
    // ========================================================================
    void HandleInput_ConnectObjects()
    {
        // BƯỚC 1: Bấm chuột -> Chọn vật thứ nhất & Bắt đầu vẽ
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, interactLayer))
            {
                int targetLayerIndex = LayerMask.NameToLayer(targetLayerName);

                if (Physics.Raycast(ray, out RaycastHit hit2, 1000f, targetLayerIndex))
                {
                    // [THÊM MỚI] Kiểm tra: Vật bị bắn trúng có thuộc Layer "Head" không?
                    if (hit2.collider != null)
                    {
                        // Nếu đúng Layer "Head" -> Thực hiện logic nối
                        if (!tfs.Contains(hit2.transform))
                        {
                            tfs.Add(hit2.transform);
                        }
                    }
                }

                // Logic Vẽ: Khởi tạo đường vẽ nét liền
                isDrawingConnection = true;
                pathPoints.Clear();

                Vector3 startPoint = hit.point + (hit.normal * lineOffset);
                pathPoints.Add(startPoint);

                lineRenderer.positionCount = 1;
                lineRenderer.SetPosition(0, startPoint);
            }
        }
        // BƯỚC 2: Giữ chuột -> Vẽ tiếp các điểm theo chuột
        else if (Input.GetMouseButton(0))
        {
            if (isDrawingConnection)
            {
                Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 1000f, interactLayer))
                {
                    Vector3 newPoint = hit.point + (hit.normal * lineOffset);

                    if (pathPoints.Count == 0 || Vector3.Distance(pathPoints[pathPoints.Count - 1], newPoint) > minDistanceBetweenPoints)
                    {
                        pathPoints.Add(newPoint);
                        lineRenderer.positionCount = pathPoints.Count;
                        lineRenderer.SetPositions(pathPoints.ToArray());
                    }
                }
            }
        }
        // BƯỚC 3: Thả chuột -> Kiểm tra vật thứ 2 & Check Win
        else if (Input.GetMouseButtonUp(0))
        {
            if (isDrawingConnection)
            {
                Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);

                // [THÊM MỚI] Lấy Index của Layer có tên là "Head" (hoặc tên bạn nhập trong Inspector)
                int targetLayerIndex = LayerMask.NameToLayer(targetLayerName);

                if (Physics.Raycast(ray, out RaycastHit hit, 1000f, targetLayerIndex))
                {
                    // [THÊM MỚI] Kiểm tra: Vật bị bắn trúng có thuộc Layer "Head" không?
                    if (hit.collider != null)
                    {
                        // Nếu đúng Layer "Head" -> Thực hiện logic nối
                        if (!tfs.Contains(hit.transform))
                        {
                            tfs.Add(hit.transform);
                        }

                        if (tfs.Count >= 2)
                        {
                            GameComplete();
                        }
                        else
                        {
                            tfs.Clear();
                        }
                    }
                    else
                    {
                        // Nếu trúng vật khác (ví dụ sàn nhà, tường) mà không phải Head -> Reset
                        // Debug.Log("Không phải layer " + targetLayerName);
                        tfs.Clear();
                    }
                }
                else
                {
                    // Thả ra ngoài không gian -> Reset
                    tfs.Clear();
                }

                // Kết thúc vẽ
                isDrawingConnection = false;
                pathPoints.Clear();
                lineRenderer.positionCount = 0;
            }
        }
    }

    // ... (Giữ nguyên phần MovePuppet cũ của bạn) ...

    void HandleInput_MovePuppet()
    {
        if (Input.GetMouseButtonDown(0))
        {
            isMoving = false;
            pathPoints.Clear();
            lineRenderer.positionCount = 0;

            Vector3 visualStartPoint = Vector3.zero;
            bool foundStartPoint = false;

            if (puppetMaster != null)
            {
                currentAnchorPos = puppetMaster.GetAnchorPosition();
                visualStartPoint = currentAnchorPos;
                if (Physics.Raycast(currentAnchorPos + Vector3.up, Vector3.down, out RaycastHit groundHit, 100f, interactLayer))
                {
                    visualStartPoint = groundHit.point + (groundHit.normal * lineOffset);
                }
                else
                {
                    visualStartPoint.y = lineOffset;
                }
                foundStartPoint = true;
            }
            else
            {
                Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 1000f, interactLayer))
                {
                    visualStartPoint = hit.point + (hit.normal * lineOffset);
                    foundStartPoint = true;
                }
            }

            if (foundStartPoint)
            {
                pathPoints.Add(visualStartPoint);
                lineRenderer.positionCount = 1;
                lineRenderer.SetPosition(0, visualStartPoint);
            }
        }
        else if (Input.GetMouseButton(0))
        {
            if (pathPoints.Count == 0) return;

            Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, interactLayer))
            {
                Vector3 rawPoint = hit.point;
                Vector3 finalPoint = rawPoint + (hit.normal * lineOffset);

                if (Vector3.Distance(pathPoints[pathPoints.Count - 1], finalPoint) > minDistanceBetweenPoints)
                {
                    pathPoints.Add(finalPoint);
                    lineRenderer.positionCount = pathPoints.Count;
                    lineRenderer.SetPositions(pathPoints.ToArray());
                }
            }
        }
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
        if (!isMoving || pathPoints.Count == 0) return;

        Vector3 rawTarget = pathPoints[0];
        Vector3 targetWithFixedY = new Vector3(rawTarget.x, currentAnchorPos.y, rawTarget.z);

        currentAnchorPos = Vector3.MoveTowards(currentAnchorPos, targetWithFixedY, moveSpeed * Time.fixedDeltaTime);
        puppetMaster.SetAnchorPosition(currentAnchorPos);

        if (Vector3.Distance(currentAnchorPos, targetWithFixedY) < stoppingDistance)
        {
            pathPoints.RemoveAt(0);
            lineRenderer.positionCount = pathPoints.Count;
            lineRenderer.SetPositions(pathPoints.ToArray());

            if (pathPoints.Count == 0) isMoving = false;
        }
    }

    void GameComplete()
    {
        Debug.Log(1);
        Level levelparent = transform.root.GetComponent<Level>();
        for (int i = 0; i < tfs.Count; i++)
        {
            levelparent.RemoveHead(tfs[i].GetComponent<Winzone>());
        }
    }

    public void ResetDrawing()
    {
        isMoving = false;
        isDrawingConnection = false;
        pathPoints.Clear();
        lineRenderer.positionCount = 0;
        tfs.Clear();

        if (puppetMaster != null)
        {
            currentAnchorPos = puppetMaster.GetAnchorPosition();
        }
    }

    public void SetPuppetMaster(RagdollPuppetMaster ragdollPuppetMaster)
    {
        this.puppetMaster = ragdollPuppetMaster;
        if (puppetMaster != null) currentAnchorPos = puppetMaster.GetAnchorPosition();
    }
}