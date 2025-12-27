using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class ConfigurableRagdollBuilder : EditorWindow
{
    // Các biến để chứa xương (giống hệt Unity Ragdoll Wizard)
    public Transform pelvis;

    public Transform leftHips;
    public Transform leftKnee;
    public Transform leftFoot;

    public Transform rightHips;
    public Transform rightKnee;
    public Transform rightFoot;

    public Transform leftArm;
    public Transform leftElbow;

    public Transform rightArm;
    public Transform rightElbow;

    public Transform middleSpine;
    public Transform head;

    public float totalMass = 20f;
    public float strength = 0f; // Độ cứng của khớp (0 = lỏng như bún)

    [MenuItem("Tools/Configurable Ragdoll Builder")]
    public static void ShowWindow()
    {
        GetWindow<ConfigurableRagdollBuilder>("Configurable Ragdoll");
    }

    void OnGUI()
    {
        GUILayout.Label("Cấu hình Xương (Kéo thả từ Hierarchy)", EditorStyles.boldLabel);

        pelvis = (Transform)EditorGUILayout.ObjectField("Pelvis (Hông)", pelvis, typeof(Transform), true);

        GUILayout.Space(5);
        GUILayout.Label("Chân Trái", EditorStyles.boldLabel);
        leftHips = (Transform)EditorGUILayout.ObjectField("Left Hips", leftHips, typeof(Transform), true);
        leftKnee = (Transform)EditorGUILayout.ObjectField("Left Knee", leftKnee, typeof(Transform), true);
        leftFoot = (Transform)EditorGUILayout.ObjectField("Left Foot", leftFoot, typeof(Transform), true);

        GUILayout.Space(5);
        GUILayout.Label("Chân Phải", EditorStyles.boldLabel);
        rightHips = (Transform)EditorGUILayout.ObjectField("Right Hips", rightHips, typeof(Transform), true);
        rightKnee = (Transform)EditorGUILayout.ObjectField("Right Knee", rightKnee, typeof(Transform), true);
        rightFoot = (Transform)EditorGUILayout.ObjectField("Right Foot", rightFoot, typeof(Transform), true);

        GUILayout.Space(5);
        GUILayout.Label("Tay Trái", EditorStyles.boldLabel);
        leftArm = (Transform)EditorGUILayout.ObjectField("Left Arm", leftArm, typeof(Transform), true);
        leftElbow = (Transform)EditorGUILayout.ObjectField("Left Elbow", leftElbow, typeof(Transform), true);

        GUILayout.Space(5);
        GUILayout.Label("Tay Phải", EditorStyles.boldLabel);
        rightArm = (Transform)EditorGUILayout.ObjectField("Right Arm", rightArm, typeof(Transform), true);
        rightElbow = (Transform)EditorGUILayout.ObjectField("Right Elbow", rightElbow, typeof(Transform), true);

        GUILayout.Space(5);
        GUILayout.Label("Thân & Đầu", EditorStyles.boldLabel);
        middleSpine = (Transform)EditorGUILayout.ObjectField("Middle Spine", middleSpine, typeof(Transform), true);
        head = (Transform)EditorGUILayout.ObjectField("Head", head, typeof(Transform), true);

        GUILayout.Space(15);
        totalMass = EditorGUILayout.FloatField("Total Mass", totalMass);
        strength = EditorGUILayout.FloatField("Joint Spring (Độ cứng)", strength);

        GUILayout.Space(20);

        if (GUILayout.Button("TẠO RAGDOLL (CONFIGURABLE JOINT)", GUILayout.Height(40)))
        {
            if (CheckConsistency())
            {
                CreateRagdoll();
            }
        }
    }

    bool CheckConsistency()
    {
        if (!pelvis || !leftHips || !leftKnee || !leftFoot || !rightHips || !rightKnee || !rightFoot ||
            !leftArm || !leftElbow || !rightArm || !rightElbow || !middleSpine || !head)
        {
            EditorUtility.DisplayDialog("Thiếu thông tin", "Vui lòng điền đủ tất cả các xương.", "OK");
            return false;
        }
        return true;
    }

    void CreateRagdoll()
    {
        Cleanup(pelvis);

        // 1. Setup Pelvis (Gốc)
        BuildBone(pelvis, null, new Transform[] { leftHips, rightHips, middleSpine }, false);

        // 2. Setup Chân
        BuildBone(leftHips, pelvis, new Transform[] { leftKnee }, true);
        BuildBone(leftKnee, leftHips, new Transform[] { leftFoot }, true);
        BuildBone(leftFoot, leftKnee, null, true);

        BuildBone(rightHips, pelvis, new Transform[] { rightKnee }, true);
        BuildBone(rightKnee, rightHips, new Transform[] { rightFoot }, true);
        BuildBone(rightFoot, rightKnee, null, true);

        // 3. Setup Thân
        BuildBone(middleSpine, pelvis, new Transform[] { leftArm, rightArm, head }, false);
        BuildBone(head, middleSpine, null, false);

        // 4. Setup Tay
        BuildBone(leftArm, middleSpine, new Transform[] { leftElbow }, true);
        BuildBone(leftElbow, leftArm, null, true);

        BuildBone(rightArm, middleSpine, new Transform[] { rightElbow }, true);
        BuildBone(rightElbow, rightArm, null, true);

        Debug.Log("Đã tạo Ragdoll với Configurable Joint thành công!");
    }

    void BuildBone(Transform bone, Transform parent, Transform[] children, bool isLimb)
    {
        // Thêm Rigidbody
        Rigidbody rb = bone.GetComponent<Rigidbody>();
        if (!rb) rb = bone.gameObject.AddComponent<Rigidbody>();
        rb.mass = totalMass / 15f; // Chia đều khối lượng tạm thời

        // Thêm Collider (Tính toán chiều dài dựa trên con)
        CapsuleCollider collider = bone.GetComponent<CapsuleCollider>();
        if (!collider) collider = bone.gameObject.AddComponent<CapsuleCollider>();

        CalculateCapsuleLogic(bone, children, collider);

        // Nếu có cha -> Tạo Joint nối với cha
        if (parent != null)
        {
            ConfigurableJoint joint = bone.GetComponent<ConfigurableJoint>();
            if (!joint) joint = bone.gameObject.AddComponent<ConfigurableJoint>();

            joint.connectedBody = parent.GetComponent<Rigidbody>();

            // Cấu hình Configurable Joint cơ bản cho Ragdoll
            SetupJointSettings(joint);
        }
    }

    void SetupJointSettings(ConfigurableJoint joint)
    {
        // Khóa vị trí (quan trọng để không bị rời xương)
        joint.xMotion = ConfigurableJointMotion.Locked;
        joint.yMotion = ConfigurableJointMotion.Locked;
        joint.zMotion = ConfigurableJointMotion.Locked;

        // Giới hạn xoay
        joint.angularXMotion = ConfigurableJointMotion.Limited;
        joint.angularYMotion = ConfigurableJointMotion.Limited;
        joint.angularZMotion = ConfigurableJointMotion.Limited;

        // Set giới hạn góc mặc định (để không bị cứng đờ)
        SoftJointLimit limit = new SoftJointLimit();
        limit.limit = 45f; // Góc xoay cho phép
        joint.lowAngularXLimit = new SoftJointLimit() { limit = -45f };
        joint.highAngularXLimit = limit;
        joint.angularYLimit = limit;
        joint.angularZLimit = limit;

        // Cài đặt Spring (Độ nảy/cứng)
        JointDrive drive = new JointDrive();
        drive.positionSpring = strength;
        drive.positionDamper = 1f;
        drive.maximumForce = float.MaxValue;

        if (strength > 0)
        {
            joint.angularXDrive = drive;
            joint.angularYZDrive = drive;
        }
    }

    void CalculateCapsuleLogic(Transform bone, Transform[] children, CapsuleCollider collider)
    {
        // Logic đơn giản để tính hướng và chiều cao của Collider
        // Dựa trên vị trí của xương con đầu tiên tìm thấy
        if (children != null && children.Length > 0)
        {
            Transform child = children[0];
            Vector3 direction = child.position - bone.position;
            float length = direction.magnitude;

            collider.height = length;
            collider.center = bone.InverseTransformPoint(bone.position + direction * 0.5f);

            // Xác định trục (X, Y hay Z) dựa trên hướng xương
            Vector3 localDir = bone.InverseTransformDirection(direction);
            if (Mathf.Abs(localDir.x) > Mathf.Abs(localDir.y) && Mathf.Abs(localDir.x) > Mathf.Abs(localDir.z))
                collider.direction = 0; // X-Axis
            else if (Mathf.Abs(localDir.y) > Mathf.Abs(localDir.x) && Mathf.Abs(localDir.y) > Mathf.Abs(localDir.z))
                collider.direction = 1; // Y-Axis
            else
                collider.direction = 2; // Z-Axis

            collider.radius = length * 0.2f; // Bán kính ước lượng
        }
        else
        {
            // Nếu không có con (ví dụ bàn tay/bàn chân), tạo collider mặc định nhỏ
            collider.height = 0.2f;
            collider.radius = 0.05f;
            collider.center = Vector3.zero;
        }
    }

    void Cleanup(Transform root)
    {
        // Hàm dọn dẹp các component vật lý cũ nếu muốn làm lại (tùy chọn)
        // Hiện tại để an toàn tôi không tự xóa để tránh mất dữ liệu khác của bạn
    }
}