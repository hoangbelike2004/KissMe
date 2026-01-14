using UnityEngine;

public class FlappyBird3D : MonoBehaviour
{
    [Header("--- CẤU HÌNH BAY ---")]
    public float jumpForce = 7f;
    public float forwardSpeed = 5f;

    [Header("--- TRẠNG THÁI ---")]
    public bool isWon = false; // Biến kiểm tra thắng

    private Rigidbody rb;
    private bool jumpRequest = false;
    private CameraFollow cameraFollow;
    private Vector3 startPos; // Biến lưu vị trí ban đầu để hồi sinh

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // 1. Lưu lại vị trí xuất phát ngay lúc vào game
        startPos = transform.position;

        if (Camera.main != null)
        {
            cameraFollow = Camera.main.GetComponent<CameraFollow>();
        }

        rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void Update()
    {
        // 2. Nếu đã thắng (isWon = true) thì return luôn -> KHÔNG CHO ẤN NỮA
        if (isWon) return;

        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
        {
            jumpRequest = true;
        }
    }

    void FixedUpdate()
    {
        // Nếu thắng rồi thì dừng logic bay
        if (isWon) return;

        // Bay thẳng trục X
        rb.linearVelocity = new Vector3(forwardSpeed, rb.linearVelocity.y, 0);

        if (jumpRequest)
        {
            Jump();
            jumpRequest = false;
        }
    }

    void LateUpdate()
    {
        if (cameraFollow != null && !isWon)
        {
            cameraFollow.UpdateCam();
        }
    }

    void Jump()
    {
        Vector3 vel = rb.linearVelocity;
        vel.y = 0;
        rb.linearVelocity = vel;
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    // --- XỬ LÝ VA CHẠM (LOGIC BẠN YÊU CẦU) ---
    void OnCollisionEnter(Collision collision)
    {
        // Nếu đã thắng rồi thì bỏ qua mọi va chạm khác
        if (isWon) return;

        // A. ĐIỀU KIỆN THẮNG: Va chạm với vật có Tag là "Head"
        if (collision.gameObject.CompareTag("Head"))
        {
            WinGame();
        }
        // B. ĐIỀU KIỆN THUA: Va chạm với bất cứ cái gì khác (Cống, Đất...)
        else
        {
            ResetToStart();
        }
    }

    void WinGame()
    {
        isWon = true;

        // Dừng mọi chuyển động vật lý ngay lập tức
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Bật Kinematic để chim đứng im, không bị rớt hay đẩy đi nữa
        rb.isKinematic = true;

        Debug.Log("WIN! Đã chạm vào Head.");
    }

    void ResetToStart()
    {
        // 1. Dịch chuyển tức thời về vị trí cũ (vị trí lúc Start)
        transform.position = startPos;

        // 2. Reset vận tốc về 0 để không bị quán tính cũ làm loạn nhịp
        rb.linearVelocity = Vector3.zero;

        // Chim sẽ tự động tiếp tục bay về trước nhờ logic trong FixedUpdate
        Debug.Log("Thua! Quay lại từ đầu.");
    }
}