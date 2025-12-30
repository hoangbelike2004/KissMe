using System.Collections.Generic;
using UnityEngine;

public class Level : MonoBehaviour
{
    private List<HeadGameplay> heads = new List<HeadGameplay>();

    public void AddHead(HeadGameplay head)
    {
        if (heads.Contains(head)) return;
        heads.Add(head);
    }

    public void RemoveHead(HeadGameplay head)
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
