using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class RagdollPuppetMaster : MonoBehaviour
{
    [Header("--- AUTO SETUP ---")]
    public GameObject ghostPrefab; // Kéo Prefab (đã có Animation) vào đây
    public bool hideGhost = true;  // Tích vào để ẩn đi

    [Header("--- TRẠNG THÁI ---")]
    public Transform animatedTargetRoot;

    [Header("Sức mạnh xoay (Angular)")]
    public float muscleSpring = 1500f;
    public float muscleDamper = 50f;

    [Header("Sức mạnh vị trí (Hips)")]
    public float moveSpring = 2000f;
    public float moveDamper = 100f;

    [Header("Tốc độ hồi phục")]
    public float recoverDuration = 0.5f;

    [System.Serializable]
    public class PuppetLimb
    {
        public string name;
        public ConfigurableJoint joint;
        public Transform targetBone;
        public Quaternion initialRotation;
        public Vector3 initialPosition;
        public bool drivePosition;
        public bool isDragging = false;
    }

    [SerializeField]
    private List<PuppetLimb> limbs = new List<PuppetLimb>();

    void Start()
    {
        // 1. Tự động Spawn Ghost từ Prefab
        if (animatedTargetRoot == null && ghostPrefab != null)
        {
            SpawnGhost();
        }

        // 2. Setup khớp xương
        if (animatedTargetRoot != null)
        {
            SetupLimbs();
        }
    }

    void SpawnGhost()
    {
        // A. Tạo ra bản sao từ Prefab
        GameObject ghostInstance = Instantiate(ghostPrefab, transform.position, transform.rotation);
        ghostInstance.name = ghostPrefab.name + "_Ghost_Hidden";

        // B. CẤU HÌNH ANIMATOR (Lấy trực tiếp từ Prefab)
        Animator ghostAnim = ghostInstance.GetComponent<Animator>();

        if (ghostAnim != null)
        {
            // Bắt buộc Animation luôn chạy dù có bị ẩn hình ảnh (Quan trọng!)
            ghostAnim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }
        else
        {
            // Chỉ cảnh báo nếu chính cái Prefab kia quên gắn Animator
            Debug.LogWarning($"⚠️ CẢNH BÁO: Prefab '{ghostPrefab.name}' không có Animator! Ragdoll sẽ đứng yên.");
        }

        // C. Ẩn hình ảnh Ghost (Tắt Mesh Renderer)
        if (hideGhost)
        {
            foreach (var r in ghostInstance.GetComponentsInChildren<Renderer>()) r.enabled = false;
        }

        // D. Tìm xương Hips để kết nối
        var ragdollHips = GetComponentsInChildren<ConfigurableJoint>()
            .FirstOrDefault(j => j.connectedBody == null || j.name.Contains("Hips") || j.name.Contains("Pelvis"));

        if (ragdollHips != null)
        {
            Transform ghostHips = FindRecursive(ghostInstance.transform, ragdollHips.name);
            if (ghostHips != null)
            {
                animatedTargetRoot = ghostHips;
            }
            else
            {
                Debug.LogError($"❌ LỖI TÊN XƯƠNG: Không tìm thấy xương tên '{ragdollHips.name}' trong Prefab Ghost!");
            }
        }
        else
        {
            // Fallback nếu không tìm thấy Hips (dùng root luôn)
            animatedTargetRoot = ghostInstance.transform;
        }
    }

    void SetupLimbs()
    {
        limbs.Clear();
        ConfigurableJoint[] allJoints = GetComponentsInChildren<ConfigurableJoint>();

        foreach (var joint in allJoints)
        {
            Transform target = FindRecursive(animatedTargetRoot, joint.name);
            if (target != null)
            {
                // Cấu hình Motion
                joint.rotationDriveMode = RotationDriveMode.Slerp;
                joint.angularXMotion = ConfigurableJointMotion.Free;
                joint.angularYMotion = ConfigurableJointMotion.Free;
                joint.angularZMotion = ConfigurableJointMotion.Free;

                // Xác định Hông (Hips)
                bool isHips = (joint.connectedBody == null || joint.name.Contains("Hips") || joint.name.Contains("Pelvis"));

                if (isHips)
                {
                    joint.xMotion = ConfigurableJointMotion.Free;
                    joint.yMotion = ConfigurableJointMotion.Free;
                    joint.zMotion = ConfigurableJointMotion.Free;
                }
                else
                {
                    joint.xMotion = ConfigurableJointMotion.Locked;
                    joint.yMotion = ConfigurableJointMotion.Locked;
                    joint.zMotion = ConfigurableJointMotion.Locked;
                }

                PuppetLimb newLimb = new PuppetLimb
                {
                    name = joint.name,
                    joint = joint,
                    targetBone = target,
                    initialRotation = joint.transform.localRotation,
                    initialPosition = joint.transform.localPosition,
                    drivePosition = isHips,
                    isDragging = false
                };

                if (newLimb.drivePosition) SetJointStrength(joint, muscleSpring, moveSpring);
                else SetJointStrength(joint, muscleSpring, 0f);

                limbs.Add(newLimb);
            }
        }
    }

    void FixedUpdate()
    {
        foreach (var limb in limbs)
        {
            if (limb.joint == null || limb.targetBone == null) continue;

            // Bỏ qua nếu đang bị chuột kéo
            if (limb.isDragging) continue;

            // Đồng bộ
            limb.joint.targetRotation = Quaternion.Inverse(limb.targetBone.localRotation) * limb.initialRotation;

            if (limb.drivePosition)
            {
                limb.joint.targetPosition = limb.targetBone.localPosition;
            }
        }
    }

    // --- CÁC HÀM GỌI TỪ BÊN NGOÀI ---

    public void RelaxMuscle(Rigidbody boneRb)
    {
        StopAllCoroutines();
        ConfigurableJoint j = boneRb.GetComponent<ConfigurableJoint>();
        if (j != null)
        {
            var foundLimb = limbs.Find(l => l.joint == j);
            if (foundLimb != null) foundLimb.isDragging = true;

            JointDrive zeroDrive = new JointDrive { positionSpring = 0, positionDamper = 0, maximumForce = float.MaxValue };
            j.angularXDrive = zeroDrive; j.angularYZDrive = zeroDrive;
            j.xDrive = zeroDrive; j.yDrive = zeroDrive; j.zDrive = zeroDrive;
        }
    }

    public void StiffenMuscle(Rigidbody boneRb)
    {
        ConfigurableJoint j = boneRb.GetComponent<ConfigurableJoint>();
        if (j != null)
        {
            var foundLimb = limbs.Find(l => l.joint == j);
            if (foundLimb != null) foundLimb.isDragging = false;

            bool isHips = (j.connectedBody == null || j.name.Contains("Hips") || j.name.Contains("Pelvis"));
            float targetMove = isHips ? moveSpring : 0f;
            StartCoroutine(SmoothReturnRoutine(j, muscleSpring, targetMove, recoverDuration));
        }
    }

    IEnumerator SmoothReturnRoutine(ConfigurableJoint joint, float targetAngSpring, float targetLinSpring, float duration)
    {
        float timer = 0f;
        float startAng = joint.angularXDrive.positionSpring;
        float startLin = joint.xDrive.positionSpring;

        while (timer < duration)
        {
            if (joint == null) yield break;
            timer += Time.fixedDeltaTime;
            float t = timer / duration;
            float smoothT = Mathf.SmoothStep(0, 1, t);

            float currentAng = Mathf.Lerp(startAng, targetAngSpring, smoothT);
            float currentLin = Mathf.Lerp(startLin, targetLinSpring, smoothT);

            SetJointStrength(joint, currentAng, currentLin);
            yield return new WaitForFixedUpdate();
        }
        if (joint != null) SetJointStrength(joint, targetAngSpring, targetLinSpring);
    }

    void SetJointStrength(ConfigurableJoint joint, float angularSpring, float linearSpring)
    {
        JointDrive angDrive = new JointDrive { positionSpring = angularSpring, positionDamper = muscleDamper, maximumForce = float.MaxValue };
        joint.angularXDrive = angDrive; joint.angularYZDrive = angDrive;

        if (linearSpring > 0)
        {
            JointDrive linDrive = new JointDrive { positionSpring = linearSpring, positionDamper = moveDamper, maximumForce = float.MaxValue };
            joint.xDrive = linDrive; joint.yDrive = linDrive; joint.zDrive = linDrive;
        }
        else
        {
            JointDrive zeroDrive = new JointDrive { positionSpring = 0, positionDamper = 0 };
            joint.xDrive = zeroDrive; joint.yDrive = zeroDrive; joint.zDrive = zeroDrive;
        }
    }

    Transform FindRecursive(Transform current, string name)
    {
        if (current.name == name) return current;
        foreach (Transform child in current) { Transform found = FindRecursive(child, name); if (found != null) return found; }
        return null;
    }
}