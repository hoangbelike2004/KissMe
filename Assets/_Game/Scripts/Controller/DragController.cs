using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DragController : MonoBehaviour
{
    private List<DragType> currentTypes = new List<DragType>();

    private Dictionary<DragType, IDrag> dragHandlers = new Dictionary<DragType, IDrag>();

    void Awake()
    {
        // 1. Lấy tất cả component trên GameObject này có kế thừa IDrag
        IDrag[] drags = GetComponents<IDrag>();
        drags = drags.Distinct().ToArray();
        for (int i = 0; i < drags.Length; i++)
        {
            drags[i].OnDeactive();
            if (!dragHandlers.ContainsKey(drags[i].DragType))
            {
                dragHandlers.Add(drags[i].DragType, drags[i]);
            }
        }
    }
    public void SetState(List<DragType> newTypes)
    {
        //dong nhung state cu
        for (int i = 0; i < currentTypes.Count; i++)
        {
            if (dragHandlers.ContainsKey(currentTypes[i]))
            {
                dragHandlers[currentTypes[i]].OnDeactive();
            }
        }

        //Mo state moi
        currentTypes = newTypes;
        for (int i = 0; i < currentTypes.Count; i++)
        {
            dragHandlers[currentTypes[i]].OnActive();
        }
    }
    void OnEnable()
    {
        Observer.OnSetDragType += SetState;
    }

    void OnDisable()
    {
        Observer.OnSetDragType -= SetState;
    }
}
