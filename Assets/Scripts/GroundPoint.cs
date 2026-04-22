using UnityEngine;

/// <summary>
/// GroundPoint: Oyuncunun yeni bir zemin parçasına bastığını algılar.
/// Her Ground prefabına eklenmelidir.
/// </summary>
public class GroundPoint : MonoBehaviour
{
    private bool hasGivenPoint = false;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            HandlePlayerContact();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            HandlePlayerContact();
        }
    }

    private void HandlePlayerContact()
    {
        // AUTHORITY: Only the first landing on a NEW ground segment counts for Level 5 progress.
        if (!hasGivenPoint && LevelManager.Instance != null && LevelManager.Instance.currentLevel == 5)
        {
            hasGivenPoint = true;
            LevelManager.Instance.ObstaclePassed();
            
            // Clean production log
            Debug.Log($"[GroundPoint] Valid landing detected on {gameObject.name}. Total Progress: {LevelManager.Instance.obstaclesPassed}/{LevelManager.Instance.LevelTargetObstacleCount}");
        }
    }
}
