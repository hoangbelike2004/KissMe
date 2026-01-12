using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
// Định nghĩa Enum ngay đây
public enum DragType
{
    None,
    Limb,       // Loại 1: Tay/Chân (Chỉ xoay)
    Body,       // Loại 2: Thân/Hips (Kéo, Khóa X, Hồi phục dáng)
    Prop        // Loại 3: Đồ vật (Kéo tự do)
}
public class Level : MonoBehaviour
{
    [SerializeField] List<DragType> dragTypes;

    public Vector3 offSetEffect;

    public float timeDelaywin;
    public List<DragType> DragTypes => dragTypes;

    public float distanceCam;

    [Header("Dùng để xác định vị trí của các đối tượng/mục đích(tutorial)/áp dụng(Level 1)")]
    public Transform target1;

    public Transform target2;

    public GameObject interactableObject;

    public GameObject interactableObject2;

    private List<Winzone> heads = new List<Winzone>();

    private WinzoneType winzoneType;

    public void AddHead(Winzone head)
    {
        if (heads.Contains(head)) return;
        heads.Add(head);
    }

    public void RemoveHead(Winzone head)
    {
        if (heads.Contains(head))
        {
            heads.Remove(head);
            var objectCompletes = heads.Where(x => x.isWinningObject).ToList();
            if (objectCompletes.Count == 0)
            {
                ChangeStateWinZone();
            }
        }
    }

    public void SetWinzoneType(WinzoneType type)
    {
        winzoneType = type;
    }

    public void ChangeStateWinZone()
    {
        switch (winzoneType)
        {
            case WinzoneType.Cake:
                interactableObject.SetActive(true);
                interactableObject2.SetActive(false);
                break;
            case WinzoneType.Frog:
                interactableObject.gameObject.SetActive(true);
                interactableObject2.SetActive(false);
                break;
            case WinzoneType.Pin:
                RagdollPuppetMaster ragdollPuppetMaster = interactableObject.GetComponent<RagdollPuppetMaster>();
                ragdollPuppetMaster.enabled = true;
                interactableObject2.SetActive(false);
                break;
            case WinzoneType.Mask:
                interactableObject.SetActive(false);
                HeadGameplay headGameplay = interactableObject2.GetComponent<HeadGameplay>();
                headGameplay.enabled = true;
                break;
            case WinzoneType.Tablet:
                interactableObject.SetActive(true);
                interactableObject2.SetActive(false);
                break;
            case WinzoneType.Key:
                interactableObject.transform
                    .DORotate(new Vector3(0, 360, 0), 2.5f, RotateMode.FastBeyond360)
                    .SetLoops(-1, LoopType.Restart) // -1 là lặp vô hạn, Restart để bắt đầu vòng mới
                    .SetEase(Ease.Linear)           // QUAN TRỌNG: Xoay đều, không bị khựng lại mỗi khi hết vòng
                    .SetLink(interactableObject.gameObject);
                interactableObject2.GetComponent<Rigidbody>().isKinematic = true;
                break;
            case WinzoneType.BucketWater:
                Transform tf = interactableObject2.transform.GetChild(0);
                if (tf)
                {
                    tf.GetComponent<Rigidbody>().isKinematic = true;
                    tf.DORotate(new Vector3(0, 0, -60), 0.4f).OnComplete(() =>
                    {
                        ParticleSystem par = tf.GetChild(0).GetComponent<ParticleSystem>();
                        par.Play();
                        Invoke(nameof(LevelBucketWater), par.main.duration);
                    });
                }
                break;
            case WinzoneType.Lid:
                interactableObject.SetActive(true);
                break;
        }
        Invoke(nameof(GameComplete), timeDelaywin);
    }

    public void LevelBucketWater()
    {
        ParticelPool particelPool = SimplePool.Spawn<ParticelPool>(PoolType.VFX_Green, interactableObject.transform.position + offSetEffect, Quaternion.Euler(-90, 0, 0));
        if (particelPool != null) particelPool.PlayVFX();
        interactableObject.SetActive(true);
        interactableObject2.SetActive(false);
    }

    public void GameComplete()
    {
        GameController.Instance.GameComplete();
    }
}
