using UnityEngine;

public interface IDrag
{
    public DragType DragType { get; }
    void OnActive();

    void OnDrag();

    void OnDeactive();

    //void OnExit();
}
