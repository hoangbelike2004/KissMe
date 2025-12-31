using System.Collections.Generic;
using UnityEngine;

public class Level : MonoBehaviour
{
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
