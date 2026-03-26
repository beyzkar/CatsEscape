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
        // Debug: Çarpışma algılandı mı?
        if (collision.collider.CompareTag("Player"))
        {
            Debug.Log("GroundPoint: Collision algılandı! Obje: " + gameObject.name + " | Level: " + (LevelManager.Instance != null ? LevelManager.Instance.currentLevel : 0));
            
            if (!hasGivenPoint && LevelManager.Instance != null && LevelManager.Instance.currentLevel == 5)
            {
                hasGivenPoint = true;
                LevelManager.Instance.ObstaclePassed();
                Debug.Log("GroundPoint: Level 5 Puani Verildi! Toplam: " + LevelManager.Instance.obstaclesPassed);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Debug: Trigger algılandı mı?
        if (collision.CompareTag("Player"))
        {
            Debug.Log("GroundPoint: Trigger algılandı! Obje: " + gameObject.name + " | Level: " + (LevelManager.Instance != null ? LevelManager.Instance.currentLevel : 0));

            if (!hasGivenPoint && LevelManager.Instance != null && LevelManager.Instance.currentLevel == 5)
            {
                hasGivenPoint = true;
                LevelManager.Instance.ObstaclePassed();
                Debug.Log("GroundPoint (Trigger): Level 5 Puani Verildi! Toplam: " + LevelManager.Instance.obstaclesPassed);
            }
        }
    }
}
