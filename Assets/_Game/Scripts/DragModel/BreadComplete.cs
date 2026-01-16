using System.Collections;
using UnityEngine;

public class BreadComplete : Winzone
{
    [SerializeField] BreadComplete target;

    private BreadComplete currentTarget;
    public override void OnInit()
    {
        base.OnInit();
    }
    public override void CheckCollision(Collision collision)
    {
        if (collision.gameObject.CompareTag(targetTag))
        {
            BreadComplete breadComplete = collision.collider.GetComponent<BreadComplete>();
            ContactPoint contact = collision.GetContact(0);
            if (breadComplete != null && target != null && breadComplete == target)
            {
                currentTarget = breadComplete;
                StartCoroutine(CheckWin(contact.point));
            }
        }
    }
    private void OnCollisionExit(Collision other)
    {
        if (currentTarget != null)
        {
            currentTarget = null;
        }
    }

    IEnumerator CheckWin(Vector3 contact)
    {
        while (currentTarget != null)
        {
            if (currentTarget.transform.position.y > transform.position.y && Mathf.Abs(transform.position.x - target.transform.position.x) < 0.4f)
            {
                currentTarget.transform.SetParent(transform);
                levelprarent.RemoveHead(this);
                target.transform.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezePosition | RigidbodyConstraints.FreezeRotation;
                target = null;
                if (isGoal)
                {
                    if (VFX_Pool != PoolType.None)
                    {
                        ParticelPool particelPool = SimplePool.Spawn<ParticelPool>(VFX_Pool, contact + levelprarent.offSetEffect, Quaternion.identity);
                        particelPool.PlayVFX();
                    }
                }
                break;
            }
            yield return null;
        }
    }
}
