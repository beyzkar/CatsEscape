using UnityEngine;

// Breaks the object (obstacle) if the player lands on top of it
public class ObstacleBreak : MonoBehaviour
{
    [Header("Settings")]
    public float topNormalThreshold = 0.5f; // Threshold for determining if the hit was from above

    private void OnCollisionEnter2D(Collision2D col)
    {
        if (!col.collider.CompareTag("Player")) return;

        // Iterate through contact points to check hit direction
        for (int i = 0; i < col.contactCount; i++)
        {
            Vector2 normal = col.GetContact(i).normal;

            // If player lands on top of the object
            if (normal.y > topNormalThreshold)
            {
                Destroy(gameObject);
                return;
            }
        }
    }
}