using UnityEngine;

public class HomeExitTrigger : MonoBehaviour
{
    [SerializeField] private bool showDebugLogs = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (showDebugLogs)
            Debug.Log($"[HomeExitTrigger] OnTriggerEnter2D with: {other.gameObject.name}, tag: {other.tag}");

        if (!other.CompareTag("Player"))
        {
            if (showDebugLogs)
                Debug.LogWarning($"[HomeExitTrigger] Not player! Tag: {other.tag}");
            return;
        }

        if (LevelManager.Instance == null)
        {
            Debug.LogError("[HomeExitTrigger] LevelManager.Instance is null!");
            return;
        }

        LevelManager.Instance.TryCompleteLevelViaHome();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // Fallback: if OnTriggerEnter doesn't fire, try OnTriggerStay
        if (other.CompareTag("Player") && LevelManager.Instance != null)
        {
            LevelManager.Instance.TryCompleteLevelViaHome();
        }
    }
}