using UnityEngine;

public class RazorZone : Winzone
{
    public override void OnInit()
    {
        base.OnInit();
    }

    public override void CheckCollision(Collision collision)
    {
        Debug.Log(1);
        if (collision.collider.CompareTag(targetTag))
        {
            levelprarent.RemoveHead(this);
            levelprarent.RemoveHead(collision.gameObject.GetComponent<Winzone>());

            collision.gameObject.SetActive(false);
        }
    }
}
