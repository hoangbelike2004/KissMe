using UnityEngine;

public class ClickObject : MonoBehaviour
{
    // Hàm này tự động chạy khi bạn click chuột trái vào Collider của vật này
    private void OnMouseDown()
    {
        if (gameObject.CompareTag("Prop"))
        {
            gameObject.SetActive(false);
            ParticelPool particelPool = SimplePool.Spawn<ParticelPool>(PoolType.VFX_Complete, gameObject.transform.position + Vector3.up * 1.5f, Quaternion.Euler(-90, 0, 0));
            if (particelPool != null) particelPool.PlayVFX();
        }
    }

    void HandleLogic()
    {
        // Ví dụ: Đổi màu
        GetComponent<Renderer>().material.color = Color.red;

        // Ví dụ: Bay lên
        // Rigidbody rb = GetComponent<Rigidbody>();
        // if (rb) rb.AddForce(Vector3.up * 500f);
    }
}