using DG.Tweening;
using UnityEngine;

public class MagnetObject : Winzone
{
    // Layer của đồng tiền để check cho nhanh
    public string coinTag = "Head";

    public override void OnInit()
    {
        base.OnInit();
    }
    public override void CheckCollision(Collision collision)
    {
        if (collision.gameObject.CompareTag(coinTag))
        {
            ContactPoint contact = collision.GetContact(0);
            if (VFX_Pool != PoolType.None)
            {
                ParticelPool particelPool = SimplePool.Spawn<ParticelPool>(VFX_Pool, contact.point + levelprarent.offSetEffect, Quaternion.identity);
                particelPool.PlayVFX();
            }
            Winzone coin = collision.transform.GetComponent<Winzone>();
            levelprarent.RemoveHead(coin);
            levelprarent.RemoveHead(this);
            cameraFollow.RemoveWinzone(coin);
            Destroy(collision.gameObject);
        }
    }
    // 1. Vùng ngoài (Trigger): Hút tiền
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(coinTag))
        {
            Transform tf = other.transform;
            tf.GetComponent<Rigidbody>().isKinematic = true;
            tf.SetParent(transform);
            tf.DOLocalMove(Vector3.zero, 1f);
        }
    }
}