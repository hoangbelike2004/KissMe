using UnityEngine;

public class FireController : MonoBehaviour
{
    [Header("--- Cấu hình Lửa ---")]
    public float currentHealth = 100f;

    // Biến nội bộ, tự lấy, không cần public
    private ParticleSystem myFire;
    private Collider myCollider;

    private float currentHealthMax => currentHealth;

    void Start()
    {
        // 1. Tự lấy Particle System nằm ngay trên GameObject này
        myFire = GetComponent<ParticleSystem>();

        // 2. Lấy Collider để lát nữa tắt nó đi khi lửa tắt
        myCollider = GetComponent<Collider>();
    }

    public void Douse(float waterDamage)
    {
        currentHealth -= waterDamage;

        // --- HIỆU ỨNG LỬA NHỎ DẦN ---
        if (myFire != null)
        {
            var main = myFire.main;
            // Giảm kích thước hạt lửa theo % máu
            // Ví dụ: Máu còn 50% -> Lửa nhỏ đi một nửa
            main.startSizeMultiplier = Mathf.Max(0, currentHealth / currentHealthMax);
        }

        // --- KIỂM TRA TẮT LỬA ---
        if (currentHealth <= 0)
        {
            Extinguish();
        }
    }

    void Extinguish()
    {
        // 1. Dừng phát sinh lửa (những hạt đang cháy sẽ tắt từ từ tự nhiên)
        if (myFire != null)
        {
            myFire.Stop();
        }

        // 2. Tắt Collider ngay lập tức
        // Để nước không còn bắn trúng "khoảng không" nơi lửa từng cháy
        if (myCollider != null)
        {
            myCollider.enabled = false;
        }

        GameController.Instance.GameComplete();
    }
}