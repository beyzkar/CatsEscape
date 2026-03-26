using UnityEngine;

public class ObstacleHit : MonoBehaviour
{
    [Tooltip("If true, kills player instantly. If false, player loses only 1 heart.")]
    public bool instantDeath = true;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (instantDeath)
            {
                if (GameOverManager.Instance != null)
                    GameOverManager.Instance.ShowGameOver();
            }
            else
            {
                PlayerObstacleRules rules = other.GetComponent<PlayerObstacleRules>();
                if (rules != null)
                {
                    rules.LoseHeart();
                }
            }
        }
    }
}