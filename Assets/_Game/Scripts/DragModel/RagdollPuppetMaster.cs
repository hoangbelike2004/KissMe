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
    [Tooltip("TRUE: Bắt chước Animation. FALSE: Ngất xỉu/Thả lỏng hoàn toàn.")]
    public bool mimicAnimation = true;

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
    private bool _lastMimicState;

    [Header("--- DEBUG VISUALS ---")]
    public bool showDebug = true;
    public Color ropeColor = Color.cyan;

    [Header("--- RECOVERY ---")]
    public float recoverDuration = 0.5f;

    // --- [PHẦN MỚI THÊM 1: Biến lưu vị trí cũ] ---
    private Vector3 _startPosition;
    private Quaternion _startRotation;
    private Rigidbody _hipsRb;
    // ---------------------------------------------

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
        // --- [PHẦN MỚI THÊM 2: Lưu lại vị trí khi bắt đầu] ---
        _hipsRb = GetHipsRigidbody();
        if (_hipsRb != null)
        {
            _startPosition = _hipsRb.position;
            _startRotation = _hipsRb.rotation;
        }
        else
        {
            _startPosition = transform.position;
            _startRotation = transform.rotation;
        }
        // -----------------------------------------------------

        // 1. Setup Ghost và Limbs trước
        if (animatedTargetRoot == null && ghostPrefab != null) SpawnGhost();
        if (animatedTargetRoot != null) SetupLimbs();

        // 2. Setup Pinning (Quan trọng: Phải lấy Hips của nhân vật thật)
        if (enablePinning) SetupPinning();

        Rigidbody hipsRb = GetHipsRigidbody(); // (Biến cục bộ này của bạn vẫn giữ nguyên)
        if (hipsRb != null) hipsRb.centerOfMass = new Vector3(0, -0.5f, 0);

        SetupRigidbodySettings();

        // Khởi tạo trạng thái ngược lại để Force update lần đầu tiên trong FixedUpdate
        _lastMimicState = !mimicAnimation;
    }

    // ... (GIỮ NGUYÊN TẤT CẢ CÁC HÀM CŨ CỦA BẠN Ở ĐÂY: SetupRigidbodySettings, GetHipsRigidbody, v.v...) ...

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
        Rigidbody hipsRb = GetHipsRigidbody();
        if (hipsRb == null)
        {
            Debug.LogError("Vẫn không tìm thấy Rigidbody của nhân vật!");
            return;
        }

        GameObject anchorObj = new GameObject("Virtual_Pin_Anchor_FIXED");
        anchorObj.transform.position = hipsRb.position;
        anchorObj.transform.rotation = hipsRb.rotation;
        anchorObj.transform.SetParent(transform.root);

        pinAnchorRb = anchorObj.AddComponent<Rigidbody>();
        pinAnchorRb.isKinematic = true;
        pinAnchorRb.useGravity = false;

        pinSpringJoint = hipsRb.gameObject.AddComponent<SpringJoint>();
        pinSpringJoint.connectedBody = pinAnchorRb;

        pinSpringJoint.autoConfigureConnectedAnchor = false;
        pinSpringJoint.anchor = Vector3.zero;
        pinSpringJoint.connectedAnchor = Vector3.zero;

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

        if (hideGhost)
        {
            foreach (Transform child in ghostInstance.transform)
            {
                Renderer r = child.GetComponent<Renderer>();
                if (r != null) r.enabled = false;
            }
        }

        animatedTargetRoot = ghostInstance.transform;
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
        // 1. KIỂM TRA THAY ĐỔI TRẠNG THÁI MIMIC
        if (mimicAnimation != _lastMimicState)
        {
            if (mimicAnimation)
            {
                foreach (var limb in limbs)
                {
                    float strength = limb.isLeg ? muscleSpring * legMuscleMultiplier : muscleSpring;
                    SetJointStrength(limb.joint, strength, 0f);
                }
            }
            else
            {
                foreach (var limb in limbs)
                {
                    SetJointStrength(limb.joint, 0f, 0f);
                }
            }
            _lastMimicState = mimicAnimation;
        }

        // 2. XỬ LÝ PINNING (NEO)
        if (enablePinning && pinSpringJoint != null && !isExternalDragging)
        {
            if (returnToGhost)
            {
                pinSpringJoint.spring = pinSpring;
                pinSpringJoint.damper = pinDamper;
            }
            else
            {
                pinSpringJoint.spring = 0;
                pinSpringJoint.damper = 0;
            }
        }

        // 3. CẬP NHẬT GÓC XOAY (CHỈ KHI ĐANG MIMIC)
        if (mimicAnimation)
        {
            foreach (var limb in limbs)
            {
                if (limb.joint == null || limb.targetBone == null) continue;
                if (limb.isDragging) continue;

                limb.joint.targetRotation = Quaternion.Inverse(limb.targetBone.localRotation) * limb.initialRotation;
            }
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
            if (mimicAnimation)
            {
                float targetAng = foundLimb.isLeg ? muscleSpring * legMuscleMultiplier : muscleSpring;
                StartCoroutine(SmoothReturnRoutine(j, targetAng, 0f, recoverDuration));
            }
        }
    }

    IEnumerator SmoothReturnRoutine(ConfigurableJoint joint, float targetAngSpring, float targetLinSpring, float duration)
    {
        float timer = 0f;
        float startAng = joint.angularXDrive.positionSpring;
        while (timer < duration)
        {
            if (joint == null) yield break;
            if (!mimicAnimation) yield break;

            timer += Time.fixedDeltaTime;
            float t = timer / duration;
            float smoothT = Mathf.SmoothStep(0, 1, t);
            float currentAng = Mathf.Lerp(startAng, targetAngSpring, smoothT);
            SetJointStrength(joint, currentAng, 0);
            yield return new WaitForFixedUpdate();
        }
        if (joint != null && mimicAnimation) SetJointStrength(joint, targetAngSpring, 0);
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

    public void MoveAnchorPosition(Vector3 newPosition, float pinSpring)
    {
        if (pinAnchorRb != null)
        {
            this.pinSpring = pinSpring;
            pinAnchorRb.transform.position += newPosition;
            returnToGhost = true;
        }
    }

    public void SetAnchorPosition(Vector3 targetPosition)
    {
        if (pinAnchorRb != null)
        {
            pinAnchorRb.MovePosition(targetPosition);
            returnToGhost = true;
            pinSpringJoint.spring = pinSpring;
            pinSpringJoint.damper = pinDamper;
        }
    }

    public Vector3 GetAnchorPosition()
    {
        if (pinAnchorRb != null)
        {
            return pinAnchorRb.position;
        }
        return transform.position;
    }

    // --- [PHẦN MỚI THÊM 3: Hàm Reset] ---
    public void ResetToStart()
    {
        // 1. Reset vị trí cái Neo (nếu có)
        if (pinAnchorRb != null)
        {
            pinAnchorRb.transform.position = _startPosition;
            pinAnchorRb.transform.rotation = _startRotation;
            pinAnchorRb.linearVelocity = Vector3.zero; // Cắt quán tính

            // Bật lại lực kéo
            returnToGhost = true;
            if (pinSpringJoint != null) pinSpringJoint.spring = pinSpring;
        }

        // 2. Reset nhân vật vật lý
        if (_hipsRb != null)
        {
            _hipsRb.position = _startPosition;
            _hipsRb.rotation = _startRotation;
            _hipsRb.linearVelocity = Vector3.zero; // Cắt quán tính (dùng .linearVelocity nếu là Unity 6)
            _hipsRb.angularVelocity = Vector3.zero;
        }

        // 3. Reset Ghost Animation (Để nó không chạy lệch pha)
        if (animatedTargetRoot != null)
        {
            animatedTargetRoot.position = _startPosition;
            animatedTargetRoot.rotation = _startRotation;
        }
    }
    // -------------------------------------

    void OnDrawGizmos()
    {
        if (!showDebug) return;

        if (pinSpringJoint != null && pinAnchorRb != null)
        {
            Gizmos.color = ropeColor;
            Gizmos.DrawLine(pinSpringJoint.transform.position, pinAnchorRb.position);
            Gizmos.color = Color.red;
            Gizmos.DrawCube(pinAnchorRb.position, Vector3.one * 0.2f);
        }
    }
}