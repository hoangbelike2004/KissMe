using UnityEngine;
using DG.Tweening; // Bắt buộc phải có DOTween

[RequireComponent(typeof(Collider))] // Tự động thêm Collider nếu quên
public class ElasticHeadController : MonoBehaviour
{
    [Header("Cấu hình Kéo")]
    [Tooltip("Độ dài tối đa có thể kéo cổ ra")]
    public float maxStretchRadius = 1.5f;

    [Tooltip("Lớp (Layer) để nhận diện chuột (thường là Default hoặc Player)")]
    public LayerMask hitLayer = 1;

    [Header("Cấu hình Hiệu ứng Thạch (Jelly)")]
    [Tooltip("Độ nảy khi thả tay (Càng cao càng văng xa)")]
    public float elasticAmplitude = 1.2f;

    [Tooltip("Độ rung lắc (Càng nhỏ rung càng nhanh)")]
    public float elasticPeriod = 0.5f;

    [Tooltip("Thời gian để bật về vị trí cũ")]
    public float snapDuration = 1.0f;

    // --- Biến nội bộ ---
    private Camera mainCam;
    private bool isDragging = false;
    private float zDepth;
    private Transform parentBone; // Xương cổ (cha của xương đầu)

    // Biến quan trọng: Lưu độ lệch mà ta cộng thêm vào Animation
    private Vector3 currentPullOffset = Vector3.zero;

    // Lưu vị trí gốc ban đầu (Local) để tham chiếu
    private Vector3 initialLocalPos;

    void Start()
    {
        mainCam = Camera.main;
        parentBone = transform.parent;

        if (parentBone == null)
        {
            Debug.LogError("Lỗi: Script này phải gắn vào Xương Đầu (đã có xương Cổ là cha)!");
            this.enabled = false;
            return;
        }

        // Lưu vị trí tương đối ban đầu của đầu so với cổ
        initialLocalPos = transform.localPosition;
    }

    void OnMouseDown()
    {
        isDragging = true;

        // Tính độ sâu Z từ Camera đến cái đầu để convert chuột chuẩn 3D
        zDepth = mainCam.WorldToScreenPoint(transform.position).z;

        // Kill tất cả tween cũ đang chạy trên object này để tránh xung đột
        transform.DOKill();
        DOTween.Kill("JellyReturn"); // Kill tween bật về cụ thể
    }

    void OnMouseDrag()
    {
        if (!isDragging) return;

        // 1. Lấy vị trí chuột trong không gian 3D
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = zDepth;
        Vector3 mouseWorldPos = mainCam.ScreenToWorldPoint(mouseScreenPos);

        // 2. Tính toán vị trí gốc mà Animation muốn cái đầu ở đó
        // (Dựa trên vị trí của xương Cổ + Offset ban đầu)
        Vector3 animDesiredPos = parentBone.TransformPoint(initialLocalPos);

        // 3. Tính Vector hướng từ [Vị trí Gốc] -> [Chuột]
        Vector3 direction = mouseWorldPos - animDesiredPos;

        // 4. Giới hạn độ dài (Clamp) để không kéo cổ dài vô tận
        Vector3 clampedOffset = Vector3.ClampMagnitude(direction, maxStretchRadius);

        // 5. Cập nhật biến Offset (Đây là giá trị ta sẽ cộng dồn vào LateUpdate)
        currentPullOffset = clampedOffset;

        // (Tuỳ chọn) Xoay đầu hướng về phía chuột cho tự nhiên
        // transform.LookAt(mouseWorldPos); 
    }

    void OnMouseUp()
    {
        isDragging = false;

        // --- HIỆU ỨNG THẠCH (JELLY EFFECT) ---

        // 1. Tween giá trị Offset về (0,0,0) với hiệu ứng đàn hồi
        // Dùng DOTween.To để thay đổi giá trị biến số 'currentPullOffset'
        DOTween.To(() => currentPullOffset, x => currentPullOffset = x, Vector3.zero, snapDuration)
            .SetEase(Ease.OutElastic, elasticAmplitude, elasticPeriod)
            .SetId("JellyReturn");

        // 2. Hiệu ứng Rung lắc hình dáng (Scale) cho giống cục thạch
        // PunchScale(Vector sức mạnh, thời gian, số lần rung, độ đàn hồi)
        transform.DOPunchScale(new Vector3(0.2f, -0.2f, 0.2f), snapDuration * 0.8f, 5, 0.5f);

        // 3. (Tuỳ chọn) Rung lắc xoay nhẹ
        transform.DOPunchRotation(new Vector3(10f, 0f, 0f), snapDuration * 0.8f, 3, 0.5f);
    }

    // Dùng LateUpdate để ghi đè lên Animator
    void LateUpdate()
    {
        // Lúc này Animator đã chạy xong và đặt đầu về vị trí chuẩn.
        // Ta cộng thêm độ lệch (Offset) vào vị trí đó.
        if (currentPullOffset != Vector3.zero)
        {
            transform.position += currentPullOffset;
        }
    }

    // Vẽ Gizmos để dễ nhìn vùng kéo trong Editor
    void OnDrawGizmosSelected()
    {
        if (parentBone != null)
        {
            Gizmos.color = Color.yellow;
            // Vẽ cầu giới hạn xung quanh vị trí gốc dự kiến
            Vector3 center = parentBone.TransformPoint(initialLocalPos != Vector3.zero ? initialLocalPos : transform.localPosition);
            Gizmos.DrawWireSphere(center, maxStretchRadius);
        }
    }
}