using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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
                GameController.Instance.GameComplete();
                // Debug.Log("🎉 LEVEL COMPLETE! Tất cả đầu đã rơi xuống.");
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
        }
    }
}
