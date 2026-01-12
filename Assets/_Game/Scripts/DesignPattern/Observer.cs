using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public static class Observer
{
    public static UnityAction OnStopDragProp;// dung keo vat

    public static UnityAction<List<DragType>> OnSetDragType;

    public static UnityAction<bool> OnDragProp;
}
