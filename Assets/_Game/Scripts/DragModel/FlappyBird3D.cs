using UnityEngine;

public class FlappyBird3D : MonoBehaviour
{
    [Header("--- CẤU HÌNH BAY ---")]
    public float jumpForce = 7f;
    public float forwardSpeed = 5f;

    [Header("--- TRẠNG THÁI ---")]
    public bool isWon = false;

    // [MỚI] Biến kiểm tra xem game đã bắt đầu chưa
    public bool isGameStarted = false;

    private Rigidbody rb;
    private bool jumpRequest = false;
    private CameraFollow cameraFollow;
    private Vector3 startPos;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        startPos = transform.position;

        if (Camera.main != null)
        {
            cameraFollow = Camera.main.GetComponent<CameraFollow>();
        }

        rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // [MỚI] Gọi hàm Reset ngay từ đầu để tắt trọng lực
        ResetToStartState();
    }

    void Update()
    {
        if (isWon) return;

        // Kiểm tra input
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
        {
            // [MỚI] Nếu game chưa bắt đầu thì kích hoạt game
            if (!isGameStarted)
            {
                StartGame();
            }

            // Ghi nhận lệnh nhảy
            jumpRequest = true;
        }
    }

    void FixedUpdate()
    {
        if (isWon) return;

        // [MỚI] Nếu game CHƯA bắt đầu thì không làm gì cả (đứng yên)
        if (!isGameStarted) return;

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
        // Camera chỉ chạy theo nếu chưa thắng (và game đã bắt đầu nếu muốn camera đứng yên lúc đầu)
        if (cameraFollow != null && !isWon)
        {
            cameraFollow.UpdateCam();
        }
    }

    // [MỚI] Hàm bắt đầu game (khi nhấn lần đầu)
    void StartGame()
    {
        isGameStarted = true;
        rb.useGravity = true; // Bắt đầu cho phép rơi
    }

    // [MỚI] Hàm đưa trạng thái về lúc chưa chơi (treo lơ lửng)
    void ResetToStartState()
    {
        isGameStarted = false;
        rb.useGravity = false; // Tắt trọng lực để không rơi
        rb.linearVelocity = Vector3.zero; // Dừng mọi chuyển động
        transform.position = startPos; // Về vị trí cũ
    }

    void Jump()
    {
        Vector3 vel = rb.linearVelocity;
        vel.y = 0;
        rb.linearVelocity = vel;
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (isWon) return;

        if (collision.gameObject.CompareTag("Head"))
        {
            WinGame();
        }
        else
        {
            ResetToStart();
        }
    }

    void WinGame()
    {
        isWon = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
        Debug.Log("WIN! Đã chạm vào Head.");
    }

    void ResetToStart()
    {
        // [MỚI] Thay vì chỉ reset vị trí, ta gọi hàm ResetToStartState để tắt trọng lực luôn
        ResetToStartState();

        Debug.Log("Thua! Quay lại từ đầu và chờ nhấn.");
    }
}