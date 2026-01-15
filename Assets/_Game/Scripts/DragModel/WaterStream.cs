using UnityEngine;
using System.Collections.Generic;

public class WaterStream : MonoBehaviour
{
    [Header("--- Sức mạnh vòi nước ---")]
    public float damagePerParticle = 5f; // Mỗi hạt nước trừ bao nhiêu máu lửa

    private ParticleSystem part;
    private List<ParticleCollisionEvent> collisionEvents;

    void Start()
    {
        part = GetComponent<ParticleSystem>();
        collisionEvents = new List<ParticleCollisionEvent>();
    }

    void OnParticleCollision(GameObject other)
    {
        // 1. Kiểm tra xem có bắn trúng vật có Tag "Fire" không
        if (other.CompareTag("Fire"))
        {
            // 2. Lấy script quản lý lửa ra
            FireController fire = other.GetComponent<FireController>();

            if (fire != null)
            {
                // 3. Tính toán số lượng hạt va chạm trong khung hình này
                // (Vì 1 frame có thể có 10 hạt nước cùng chạm vào lửa)
                int numCollisionEvents = part.GetCollisionEvents(other, collisionEvents);

                // Trừ máu: (Sát thương 1 hạt) * (Số hạt va chạm)
                fire.Douse(damagePerParticle * numCollisionEvents);
            }
        }
    }
}