using UnityEngine;

public class ObstacleBreak : MonoBehaviour
{
    [Header("Break when player lands on top")]
    public float topNormalThreshold = 0.5f; // 0.6 yerine biraz daha toleranslı

    private void OnCollisionEnter2D(Collision2D col)
    {
        if (!col.collider.CompareTag("Player")) return;

        // tüm temas noktalarını kontrol et (en sağlamı)
        for (int i = 0; i < col.contactCount; i++)
        {
            Vector2 n = col.GetContact(i).normal;

            // Player kutunun üstüne iniş yaptıysa normal yukarı bakar
            if (n.y > topNormalThreshold)
            {
                Destroy(gameObject);
                return;
            }
        }
    }
}