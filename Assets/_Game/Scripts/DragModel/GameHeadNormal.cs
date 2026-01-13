using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameHeadNormal : Winzone
{
    [Header("Cấu hình Treo")]
    public float liftHeight = 2.5f;   // Kéo lên cao bao nhiêu mét?
    public float springPower = 200f;  // Lực kéo (càng to kéo càng dứt khoát)
    public float damper = 10f;        // Độ êm (để khỏi nảy tưng tưng)

    public List<GameObject> ragdollPuppetMasters = new List<GameObject>();
    public override void OnInit()
    {
        base.OnInit();
    }

    public override void CheckCollision(Collision collision)
    {
        if (collision.gameObject.CompareTag(targetTag))
        {

            // --- TÌM RAGDOLL PUPPET MASTER ---
            // GetComponentInParent sẽ tìm component này trên chính object đó
            // hoặc tìm ngược lên các object cha (Head -> Spine -> Hips -> Root...)
            RagdollPuppetMaster puppetMaster = collision.gameObject.GetComponentInParent<RagdollPuppetMaster>();

            if (puppetMaster != null)
            {
                puppetMaster.StopReturning();
            }
            // --- CÁC XỬ LÝ CŨ CỦA BẠN ---
            ContactPoint contact = collision.GetContact(0);

            if (!isGoal) return;
            collision.gameObject.tag = "Complete";

            Winzone windZone = collision.gameObject.GetComponent<Winzone>();
            cameraFollow.RemoveWinzone(windZone);
            levelprarent.RemoveHead(windZone);
            StartCoroutine(DestroyGameObject(puppetMaster.gameObject));
            // XỬ LÝ KÉO ĐẦU
            //AttachSkyHook(collision.gameObject, contact.point, puppetMaster.transform.gameObject);
        }
    }

    // Hàm riêng để xử lý logic vật lý
    private void AttachSkyHook(GameObject victimHead, Vector3 hitPoint, GameObject parent)
    {
        // 1. Kiểm tra: Nếu nó đang bị treo rồi thì thôi (tránh gắn 2-3 dây cùng lúc)
        if (victimHead.GetComponent<SpringJoint>() != null) return;

        // 2. Tạo cái Móc (Anchor) ở trên trời
        // Vị trí móc = Vị trí va chạm + Chiều cao muốn kéo
        GameObject anchor = new GameObject("SkyHook_Anchor");
        anchor.transform.position = hitPoint + Vector3.up * liftHeight;

        // 3. Setup Rigidbody cho Móc (Để nó đứng im trên không trung)
        Rigidbody anchorRb = anchor.AddComponent<Rigidbody>();
        anchorRb.isKinematic = true;

        // 4. Gắn dây (SpringJoint) vào đầu nạn nhân
        SpringJoint joint = victimHead.AddComponent<SpringJoint>();
        joint.connectedBody = anchorRb;

        // 5. Cấu hình dây để nó hoạt động như "cần cẩu"
        joint.autoConfigureConnectedAnchor = false;

        // Điểm neo trên móc: Tâm của móc
        joint.connectedAnchor = Vector3.zero;
        // Điểm neo trên đầu: Tâm của đầu (hoặc vị trí va chạm nếu muốn)
        joint.anchor = Vector3.zero;

        // QUAN TRỌNG NHẤT: Set khoảng cách dây bằng 0
        // Lò xo sẽ co lại hết cỡ => Kéo nhân vật bay vút lên cái móc
        joint.maxDistance = 0f;
        joint.minDistance = 0f;

        // Lực kéo và độ hãm
        joint.spring = springPower;
        joint.damper = damper;

        // [TÙY CHỌN] Nếu muốn xóa cái móc khi nhân vật chết hoặc sau 10 giây
        StartCoroutine(DestroyGameObject(parent));
    }

    IEnumerator DestroyGameObject(GameObject ojb)
    {
        if (ragdollPuppetMasters.Count > 0) ragdollPuppetMasters.RemoveAt(0);
        ParticelPool par = SimplePool.Spawn<ParticelPool>(VFX_Pool, ojb.transform.position + levelprarent.offSetEffect, Quaternion.identity);
        par.PlayVFX();
        Destroy(ojb);
        yield return new WaitForSeconds(0.4f);
        if (ragdollPuppetMasters.Count > 0) ragdollPuppetMasters[0].SetActive(true);
        else levelprarent.RemoveHead(this);
    }
}