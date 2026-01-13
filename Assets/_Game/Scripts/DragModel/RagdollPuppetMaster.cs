using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class RagdollPuppetMaster : MonoBehaviour
{
    [Header("--- AUTO SETUP ---")]
    public GameObject ghostPrefab;
    public bool hideGhost = true;

    [Header("--- CONTROL (ĐIỀU KHIỂN) ---")]
    [Tooltip("TRUE: Lò xo căng (kéo về cọc neo). FALSE: Lò xo lỏng (tự do).")]
    public bool returnToGhost = true;

    [Header("--- TARGETS (MỤC TIÊU) ---")]
    public Transform animatedTargetRoot;
    public Transform pinTargetBone;

    [Header("--- MUSCLE SETTINGS ---")]
    public float muscleSpring = 1500f;
    public float muscleDamper = 50f;

    [Header("--- BALANCE ---")]
    [Range(1f, 20f)]
    public float legMuscleMultiplier = 10f;

    [Header("--- VIRTUAL ROPE (SPRING JOINT) ---")]
    public bool enablePinning = true;

    [Tooltip("Lực lò xo giữ nhân vật lại tại điểm neo.")]
    public float pinSpring = 2000;
    public float pinDamper = 200f;

    private Rigidbody pinAnchorRb;
    private SpringJoint pinSpringJoint;

    private bool isExternalDragging = false;

    [Header("--- DEBUG VISUALS ---")]
    public bool showDebug = true;
    public Color ropeColor = Color.cyan;

    [Header("--- RECOVERY ---")]
    public float recoverDuration = 0.5f;

    [System.Serializable]
    public class PuppetLimb
    {
        public string name;
        public ConfigurableJoint joint;
        public Transform targetBone;
        public Quaternion initialRotation;
        public bool drivePosition;
        public bool isDragging = false;
        public bool isLeg = false;
    }

    [SerializeField]
    private List<PuppetLimb> limbs = new List<PuppetLimb>();

    void Start()
    {
        // 1. Setup Ghost và Limbs trước
        if (animatedTargetRoot == null && ghostPrefab != null) SpawnGhost();
        if (animatedTargetRoot != null) SetupLimbs();

        // 2. Setup Pinning (Quan trọng: Phải lấy Hips của nhân vật thật)
        if (enablePinning) SetupPinning();

        Rigidbody hipsRb = GetHipsRigidbody();
        if (hipsRb != null) hipsRb.centerOfMass = new Vector3(0, -0.5f, 0);

        SetupRigidbodySettings();
    }

    void SetupRigidbodySettings()
    {
        Rigidbody[] allRigidbodies = GetComponentsInChildren<Rigidbody>();
        foreach (var rb in allRigidbodies)
        {
            if (pinAnchorRb != null && rb == pinAnchorRb) continue;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }
    }

    Rigidbody GetHipsRigidbody()
    {
        // Tìm Rigidbody trên chính nhân vật này (Ragdoll)
        var rb = GetComponentInChildren<Rigidbody>();
        if (rb != null && (rb.name.Contains("Hips") || rb.name.Contains("Pelvis"))) return rb;
        var allRbs = GetComponentsInChildren<Rigidbody>();
        foreach (var r in allRbs)
        {
            if (r.name.Contains("Hips") || r.name.Contains("Pelvis")) return r;
        }
        if (allRbs.Length > 0) return allRbs[0];
        return null;
    }

    void SetupPinning()
    {
        // YÊU CẦU: Lấy vị trí của chính nhân vật (Ragdoll) làm gốc
        Rigidbody hipsRb = GetHipsRigidbody();

        if (hipsRb == null)
        {
            Debug.LogError("Vẫn không tìm thấy Rigidbody của nhân vật!");
            return;
        }

        // Tạo một cái Neo (Anchor) vô hình
        GameObject anchorObj = new GameObject("Virtual_Pin_Anchor_FIXED");

        // --- QUAN TRỌNG: Đặt Neo tại vị trí Hips của nhân vật lúc Start ---
        anchorObj.transform.position = hipsRb.position;
        anchorObj.transform.rotation = hipsRb.rotation;

        anchorObj.transform.SetParent(transform.root);

        pinAnchorRb = anchorObj.AddComponent<Rigidbody>();
        pinAnchorRb.isKinematic = true; // Neo cứng, không bị vật lý tác động
        pinAnchorRb.useGravity = false;

        // Gắn lò xo từ Hips nhân vật vào cái Neo cứng đó
        pinSpringJoint = hipsRb.gameObject.AddComponent<SpringJoint>();
        pinSpringJoint.connectedBody = pinAnchorRb;

        pinSpringJoint.autoConfigureConnectedAnchor = false;
        pinSpringJoint.anchor = Vector3.zero;
        pinSpringJoint.connectedAnchor = Vector3.zero;

        // Cấu hình lực lò xo ban đầu
        pinSpringJoint.spring = pinSpring;
        pinSpringJoint.damper = pinDamper;
        pinSpringJoint.minDistance = 0;
        pinSpringJoint.maxDistance = 0;
        pinSpringJoint.tolerance = 0.01f;
    }

    void SpawnGhost()
    {
        GameObject ghostInstance = Instantiate(ghostPrefab, transform.position, transform.rotation);
        ghostInstance.transform.SetParent(transform.root);
        ghostInstance.name = ghostPrefab.name + "_Ghost_Hidden";

        Animator ghostAnim = ghostInstance.GetComponent<Animator>();
        if (ghostAnim != null) ghostAnim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        // --- YÊU CẦU: Chỉ ẩn con cấp 1 (Level 1 Children) ---
        if (hideGhost)
        {
            foreach (Transform child in ghostInstance.transform)
            {
                Renderer r = child.GetComponent<Renderer>();
                if (r != null) r.enabled = false;
            }
        }
        // ----------------------------------------------------

        animatedTargetRoot = ghostInstance.transform;

        // Tìm Hips của Ghost để khớp Animation (Rotation)
        pinTargetBone = FindRecursive(ghostInstance.transform, "Hips");
        if (pinTargetBone == null) pinTargetBone = FindRecursive(ghostInstance.transform, "Pelvis");

        if (pinTargetBone == null)
        {
            var ragdollHips = GetComponentsInChildren<ConfigurableJoint>()
                .FirstOrDefault(j => j.connectedBody == null);
            if (ragdollHips != null)
                pinTargetBone = FindRecursive(ghostInstance.transform, ragdollHips.name);
        }

        if (pinTargetBone == null) pinTargetBone = ghostInstance.transform;
    }

    void SetupLimbs()
    {
        limbs.Clear();
        ConfigurableJoint[] allJoints = GetComponentsInChildren<ConfigurableJoint>();
        string[] legKeywords = { "leg", "calf", "foot", "ankle", "knee", "shin", "thigh" };

        foreach (var joint in allJoints)
        {
            if (pinSpringJoint != null && joint.gameObject == pinSpringJoint.gameObject) continue;

            Transform target = FindRecursive(animatedTargetRoot, joint.name);
            if (target == null && pinTargetBone != null)
                target = FindRecursive(pinTargetBone.root, joint.name);

            if (target != null)
            {
                joint.rotationDriveMode = RotationDriveMode.Slerp;
                joint.angularXMotion = ConfigurableJointMotion.Free;
                joint.angularYMotion = ConfigurableJointMotion.Free;
                joint.angularZMotion = ConfigurableJointMotion.Free;

                bool isHips = (joint.connectedBody == null || joint.name.Contains("Hips"));
                string lowerName = joint.name.ToLower();
                bool isLegPart = legKeywords.Any(k => lowerName.Contains(k));

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
                    drivePosition = isHips,
                    isLeg = isLegPart
                };

                float finalSpring = isLegPart ? muscleSpring * legMuscleMultiplier : muscleSpring;
                SetJointStrength(joint, finalSpring, 0f);
                limbs.Add(newLimb);
            }
        }
    }

    void FixedUpdate()
    {
        // --- YÊU CẦU: Neo đứng im, KHÔNG CẬP NHẬT VỊ TRÍ ---
        // Đã xóa dòng: pinAnchorRb.MovePosition(...) 
        // Neo sẽ nằm im tại vị trí được tạo ra ở hàm Start.

        // Chỉ xử lý lực lò xo:
        if (enablePinning && pinSpringJoint != null && !isExternalDragging)
        {
            if (returnToGhost)
            {
                // Bật lực kéo: Nhân vật bị giữ lại quanh cái Neo cố định
                pinSpringJoint.spring = pinSpring;
                pinSpringJoint.damper = pinDamper;
            }
            else
            {
                // Tắt lực kéo: Nhân vật tự do đi lại (tạm thời cắt dây)
                pinSpringJoint.spring = 0;
                pinSpringJoint.damper = 0;
            }
        }

        // 2. Cập nhật Rotation khớp xương (Để nhân vật vẫn múa may theo Animation)
        foreach (var limb in limbs)
        {
            if (limb.joint == null || limb.targetBone == null) continue;
            if (limb.isDragging) continue;

            limb.joint.targetRotation = Quaternion.Inverse(limb.targetBone.localRotation) * limb.initialRotation;
        }
    }

    public void RelaxMuscle(Rigidbody boneRb)
    {
        StopAllCoroutines();
        ConfigurableJoint j = boneRb.GetComponent<ConfigurableJoint>();
        if (j != null)
        {
            var foundLimb = limbs.Find(l => l.joint == j);
            if (foundLimb != null) foundLimb.isDragging = true;
            JointDrive zeroDrive = new JointDrive { positionSpring = 0, maximumForce = float.MaxValue };
            j.angularXDrive = zeroDrive; j.angularYZDrive = zeroDrive;
        }
    }

    public void StiffenMuscle(Rigidbody boneRb)
    {
        ConfigurableJoint j = boneRb.GetComponent<ConfigurableJoint>();
        if (j != null)
        {
            var foundLimb = limbs.Find(l => l.joint == j);
            if (foundLimb == null) return;

            foundLimb.isDragging = false;
            float targetAng = foundLimb.isLeg ? muscleSpring * legMuscleMultiplier : muscleSpring;
            StartCoroutine(SmoothReturnRoutine(j, targetAng, 0f, recoverDuration));
        }
    }

    IEnumerator SmoothReturnRoutine(ConfigurableJoint joint, float targetAngSpring, float targetLinSpring, float duration)
    {
        float timer = 0f;
        float startAng = joint.angularXDrive.positionSpring;
        while (timer < duration)
        {
            if (joint == null) yield break;
            timer += Time.fixedDeltaTime;
            float t = timer / duration;
            float smoothT = Mathf.SmoothStep(0, 1, t);
            float currentAng = Mathf.Lerp(startAng, targetAngSpring, smoothT);
            SetJointStrength(joint, currentAng, 0);
            yield return new WaitForFixedUpdate();
        }
        if (joint != null) SetJointStrength(joint, targetAngSpring, 0);
    }

    void SetJointStrength(ConfigurableJoint joint, float angularSpring, float linearSpring)
    {
        JointDrive angDrive = new JointDrive { positionSpring = angularSpring, positionDamper = muscleDamper, maximumForce = float.MaxValue };
        joint.angularXDrive = angDrive; joint.angularYZDrive = angDrive;

        JointDrive zero = new JointDrive { positionSpring = 0, maximumForce = 0 };
        joint.xDrive = zero; joint.yDrive = zero; joint.zDrive = zero;
    }

    Transform FindRecursive(Transform current, string namePart)
    {
        if (current.name.Contains(namePart)) return current;
        foreach (Transform child in current) { Transform found = FindRecursive(child, namePart); if (found != null) return found; }
        return null;
    }

    // --- HÀM GIAO TIẾP BÊN NGOÀI ---

    public void LoosenPin()
    {
        isExternalDragging = true;
        if (pinSpringJoint != null)
        {
            pinSpringJoint.spring = 0;
            pinSpringJoint.damper = 0;
        }
    }

    public void TightenPin()
    {
        isExternalDragging = false;
        // Logic sẽ tự động được FixedUpdate xử lý dựa trên biến returnToGhost
    }

    public void StopReturning()
    {
        returnToGhost = false;
        if (pinSpringJoint != null)
        {
            pinSpringJoint.spring = 0;
            pinSpringJoint.damper = 0;
        }
    }

    public void EnableReturning()
    {
        returnToGhost = true;
    }

    //ham dung de di chuyen đến vị trí offset và thay đổi tốc độ giựt
    public void MoveAnchorPosition(Vector3 newPosition, float pinSpring)
    {
        if (pinAnchorRb != null)
        {
            this.pinSpring = pinSpring;
            pinAnchorRb.transform.position += newPosition;

            // Bật lại lực kéo nếu đang bị tắt
            returnToGhost = true;
        }
    }
    void OnDrawGizmos()
    {
        if (!showDebug) return;

        if (pinSpringJoint != null && pinAnchorRb != null)
        {
            Gizmos.color = ropeColor;
            Gizmos.DrawLine(pinSpringJoint.transform.position, pinAnchorRb.position);

            // Vẽ cái Neo cố định
            Gizmos.color = Color.red;
            Gizmos.DrawCube(pinAnchorRb.position, Vector3.one * 0.2f);
        }
    }
}