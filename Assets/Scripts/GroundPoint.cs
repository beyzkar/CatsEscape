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
        // Sadece Level 5'te ve daha önce puan verilmemişse çalışır
        if (!hasGivenPoint && LevelManager.Instance != null && LevelManager.Instance.currentLevel == 5)
        {
            hasGivenPoint = true;
            LevelManager.Instance.ObstaclePassed();
            
            // Opsiyonel: Sadece önemli ilerlemeleri logla
            if (LevelManager.Instance.obstaclesPassed % 2 == 0)
            {
                Debug.Log($"GroundPoint: Level 5 Progress: {LevelManager.Instance.obstaclesPassed}/{LevelManager.Instance.LevelTargetObstacleCount}");
            }
        }
    }
}
