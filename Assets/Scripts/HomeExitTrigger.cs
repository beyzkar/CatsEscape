using UnityEngine;

// Trigger bridge for level exit object (Home) in levels 1-4.
public class HomeExitTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (LevelManager.Instance == null) return;

        LevelManager.Instance.TryCompleteLevelViaHome();
    }
}
