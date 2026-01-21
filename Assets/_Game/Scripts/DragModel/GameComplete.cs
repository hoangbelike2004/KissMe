using UnityEngine;

public class GameComplete : Winzone
{
    public override void OnInit()
    {
        base.OnInit();
    }
    public override void CheckCollision(Collision collision)
    {
        if (collision.gameObject.CompareTag(targetTag))
        {
            ContactPoint contact = collision.GetContact(0);

            Winzone otherHead = collision.gameObject.GetComponent<Winzone>();
            if (otherHead == null) return;
            if (!isGoal && !otherHead.isGoal) return;
            if (isGoal)
            {
                if (WinzoneType.Booling == winzoneType)
                {
                    collision.gameObject.SetActive(false);
                }
                if (VFX_Pool != PoolType.None)
                {
                    ParticelPool particelPool = SimplePool.Spawn<ParticelPool>(VFX_Pool, contact.point + levelprarent.offSetEffect, Quaternion.identity);
                    particelPool.PlayVFX();
                }
                otherHead.gameObject.tag = "Complete";
                gameObject.tag = "Complete";
            }
            if (this.isSpecial != otherHead.isSpecial)
            {
                if (this is GameComplete && otherHead is GameComplete)
                {
                    Observer.OnStopDragProp?.Invoke();
                }
                else
                {
                    if (this.isSpecial)
                    {
                        // Ra lệnh cho Mouse Joint buông ra và nảy về cổ
                        // 1. QUAN TRỌNG: Cắt dây chuột ngay lập tức, KHÔNG nảy về
                        if (dragManager != null)
                        {
                            dragManager.ForceStopImmediate();
                        }
                        // 2. Dính vào đối phương và đóng băng tại chỗ
                        LockHeadToTarget(collision.rigidbody);
                    }
                    else
                    {
                        transform.SetParent(otherHead.transform);
                    }
                }
            }
            // --- CASE 2: HUỀ (Cùng loại va nhau) ---
            else
            {
                if (this is GameComplete && otherHead is GameComplete)
                {
                    Observer.OnStopDragProp?.Invoke();
                }
                else
                {
                    // 1. QUAN TRỌNG: Cắt dây chuột ngay lập tức, KHÔNG nảy về
                    if (dragManager != null)
                    {
                        dragManager.ForceStopImmediate();
                    }
                    // 2. Dính vào đối phương và đóng băng tại chỗ
                    LockHeadToTarget(collision.rigidbody);
                }
            }
            // --- CASE 1: TÔI LÀ VIP & THẮNG (VIP húc Thường) ---
            levelprarent.RemoveHead(this);
            levelprarent.RemoveHead(otherHead);
        }
    }
}
