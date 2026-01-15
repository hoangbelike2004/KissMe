using System.Collections;
using UnityEngine;

public class DistanceComplete : Winzone
{

    [SerializeField] private float distance;
    private Vector3 startPos;

    private Coroutine _checkCoroutinue;
    public override void OnInit()
    {
        startPos = transform.position;
        base.OnInit();
    }

    IEnumerator CheckDistance()
    {
        while (true)
        {
            if (Vector3.Distance(transform.position, startPos) > distance)
            {

                if (isGoal)
                {

                    if (VFX_Pool != PoolType.None)
                    {
                        ParticelPool particelPool = SimplePool.Spawn<ParticelPool>(VFX_Pool, startPos, Quaternion.Euler(-90, 0, 0));
                        if (particelPool != null) particelPool.PlayVFX();
                    }
                }

                // --- CASE 1: TÔI LÀ VIP & THẮNG (VIP húc Thường) ---
                if (levelprarent != null) levelprarent.RemoveHead(this);
                gameObject.tag = "Complete";
                Observer.OnStopDragProp?.Invoke();
                break;
            }
            yield return null;
        }
    }

    public void Check(bool isCheck)
    {
        if (isCheck) _checkCoroutinue = StartCoroutine(CheckDistance());
        else { if (_checkCoroutinue != null) StopCoroutine(_checkCoroutinue); }
    }
    private void OnEnable()
    {
        Observer.OnDragProp += Check;
    }
    private void OnDisable()
    {
        Observer.OnDragProp -= Check;
    }
}
