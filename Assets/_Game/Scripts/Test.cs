using UnityEngine;

public class Test : MonoBehaviour
{
    private ConfigurableJoint joint;

    // Biến để lưu trạng thái gốc
    private Quaternion initialTargetRotation;

    [Header("Góc muốn xoay thử")]
    public Vector3 rotationAngle = new Vector3(45, 0, 0); // Ví dụ xoay 45 độ trục X

    void Start()
    {
        joint = GetComponent<ConfigurableJoint>();

        // 1. [QUAN TRỌNG] Lưu lại targetRotation gốc ngay khi game bắt đầu
        initialTargetRotation = joint.targetRotation;

        // Đảm bảo Joint có lò xo để tự hồi phục
        SetupJointSpring();
    }

    void Update()
    {
        // Nhấn phím R để Xoay (Rotate)
        if (Input.GetKeyDown(KeyCode.R))
        {
            RotateJoint();
        }

        // Nhấn phím Space để Phục hồi (Recover)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            RecoverJoint();
        }
    }

    // Hàm xoay đi chỗ khác
    void RotateJoint()
    {
        Debug.Log("Đang xoay...");
        Quaternion targetRot = Quaternion.Euler(rotationAngle);

        // Công thức xoay ConfigurableJoint
        joint.targetRotation = Quaternion.Inverse(targetRot) * initialTargetRotation;
    }

    // Hàm phục hồi về như cũ
    void RecoverJoint()
    {
        Debug.Log("Đang phục hồi...");

        // 2. Gán lại giá trị gốc đã lưu -> Lò xo sẽ tự kéo về
        joint.targetRotation = initialTargetRotation;
    }

    // Cài đặt phụ để đảm bảo Joint có lực lò xo (nếu bạn chưa chỉnh trong Inspector)
    void SetupJointSpring()
    {
        JointDrive drive = new JointDrive
        {
            positionSpring = 1000f, // Lực kéo về
            positionDamper = 50f,   // Độ giảm chấn (chống rung)
            maximumForce = float.MaxValue
        };

        joint.rotationDriveMode = RotationDriveMode.Slerp;
        joint.slerpDrive = drive;
    }
}