using System.Collections;
using UnityEngine;

public class ProximityChecker : Winzone
{
    [SerializeField] private float distance;
    [SerializeField] private Transform tf2;
    [SerializeField] RagdollPuppetMaster puppet;
    [SerializeField] RagdollPathDrawer pathDrawer;
    public override void OnInit()
    {
        base.OnInit();
        StartCoroutine(CheckDistance());
    }

    IEnumerator CheckDistance()
    {
        while (true)
        {
            if (Vector3.Distance(transform.position, tf2.position) < distance)
            {
                if (isGoal && VFX_Pool != PoolType.None)
                {
                    ParticelPool particelPool = SimplePool.Spawn<ParticelPool>(VFX_Pool, transform.position, Quaternion.Euler(-90, 0, 0));
                    if (particelPool != null) particelPool.PlayVFX();
                }
                Observer.OnDrawToTaget?.Invoke();
                levelprarent.RemoveHead(this);
                break;
            }
            yield return null;
        }
    }
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Obstacle"))
        {
            ResetToStart();
        }
    }

    void ResetToStart()
    {
        // 1. Reset Vật lý + Vị trí nhân vật
        if (puppet != null)
        {
            puppet.ResetToStart();
        }

        // 2. Reset Hệ thống vẽ (để nó không tiếp tục kéo nhân vật đi)
        if (pathDrawer != null)
        {
            pathDrawer.ResetDrawing();
        }
    }
}
