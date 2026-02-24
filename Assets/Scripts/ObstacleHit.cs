using UnityEngine;

public class ObstacleHit : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (GameOverManager.Instance != null)
                GameOverManager.Instance.ShowGameOver();
        }
    }
}