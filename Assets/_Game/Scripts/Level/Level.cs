using System.Collections.Generic;
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
    [SerializeField] DragType dragType;

    public DragType DragType => dragType;

    private List<Winzone> heads = new List<Winzone>();

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
            if (heads.Count == 0)
            {
                // Debug.Log("🎉 LEVEL COMPLETE! Tất cả đầu đã rơi xuống.");
            }
        }
    }
}
