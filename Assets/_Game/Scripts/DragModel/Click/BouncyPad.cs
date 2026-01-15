using UnityEngine;
using System.Collections;

public class BouncyPad : MonoBehaviour
{
    [Header("--- CẤU HÌNH NẢY ---")]
    public float bounceForce = 15f;    // Độ mạnh cú nảy
    public float floatDuration = 1.0f; // Thời gian bay tự do (trước khi bị dây neo kéo lại)

    void OnCollisionEnter(Collision collision)
    {
        // 1. Tìm xem cái va chạm có phải là Ragdoll không
        // (Dùng GetComponentInParent vì collider thường nằm ở chân, còn script nằm ở Root)
        RagdollPuppetMaster puppet = collision.gameObject.GetComponentInParent<RagdollPuppetMaster>();

        if (puppet != null)
        {
            // Tìm Rigidbody của bộ phận va chạm (ví dụ: Chân) để đẩy
            Rigidbody hitBody = collision.rigidbody;

            if (hitBody != null)
            {
                // BẮT ĐẦU QUY TRÌNH NẢY
                StartCoroutine(PerformBounce(puppet, hitBody));
            }
        }
    }

    IEnumerator PerformBounce(RagdollPuppetMaster puppet, Rigidbody body)
    {
        // BƯỚC 1: "Cởi dây" - Tạm thời tắt lực lò xo để nhân vật không bị kéo xuống đất
        puppet.LoosenPin();

        // BƯỚC 2: Reset vận tốc cũ (để nảy dứt khoát, không bị triệt tiêu lực)
        // Lưu ý: Unity 6 dùng body.linearVelocity, Unity cũ dùng body.velocity
        // Nếu báo lỗi dòng dưới, hãy đổi thành: body.velocity = ...
        body.linearVelocity = new Vector3(body.linearVelocity.x, 0, body.linearVelocity.z);

        // BƯỚC 3: Đẩy nhân vật lên trời
        // ForceMode.Impulse tạo lực nổ tức thì
        body.AddForce(Vector3.up * bounceForce, ForceMode.Impulse);

        // (Tùy chọn) Đẩy cả Hips lên nữa cho chắc ăn (tránh trường hợp chỉ chân bay lên mà người ở lại)
        // puppet.GetComponentInChildren<Rigidbody>().AddForce(Vector3.up * bounceForce * 0.5f, ForceMode.Impulse);

        // BƯỚC 4: Chờ nhân vật bay lên xong
        yield return new WaitForSeconds(floatDuration);

        // BƯỚC 5: "Trói lại" - Đồng bộ vị trí Neo lên trời theo nhân vật
        // Nếu không làm bước này, khi TightenPin lại, nhân vật sẽ bị giật ngược về mặt đất cũ

        // Lấy vị trí hiện tại của nhân vật trên không trung
        Vector3 currentAirPos = body.position;

        // Cập nhật cái Neo (Anchor) bay lên đó luôn
        puppet.SetAnchorPosition(currentAirPos);

        // Kích hoạt lại lò xo để tiếp tục điều khiển
        puppet.TightenPin();
    }
}