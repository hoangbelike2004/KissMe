using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DragController : MonoBehaviour
{
    private DragType currentType;

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
        SetState(DragType.Prop);
    }
    public void SetState(DragType newType)
    {
        if (currentType == newType) return;

        ChangeState(newType);
    }

    public void ChangeState(DragType newType)
    {
        dragHandlers[newType].OnActive();
        if (dragHandlers.ContainsKey(currentType)) dragHandlers[currentType].OnDeactive();
        currentType = newType;
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
