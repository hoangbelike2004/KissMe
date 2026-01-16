using DG.Tweening;
using UnityEngine;

public class DoorComplete : MonoBehaviour
{
    public bool isClose = true;
    void OnCollisionEnter(Collision collision)
    {
        if (isClose)
        {
            if (collision.collider.CompareTag("Complete"))
            {
                isClose = false;
                Debug.Log(1);
                transform.parent.DOLocalRotate(Vector3.up * 60, 0.5f);
                collision.collider.tag = "Untagged";
            }
        }
    }
}
