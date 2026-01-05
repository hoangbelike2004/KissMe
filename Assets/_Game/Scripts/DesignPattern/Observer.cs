using UnityEngine;
using UnityEngine.Events;

public static class Observer
{
    public static UnityAction OnStopDragProp;// dung keo vat

    public static UnityAction<DragType> OnSetDragType;
}
