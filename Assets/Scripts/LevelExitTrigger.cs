using UnityEngine;

public class LevelExitTrigger : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool showDebugLogs = false;

    [Header("References")]
    [SerializeField] private LevelVideoTransitionManager transitionManager;

    private bool hasTriggered = false;

    private void Start()
    {
        if (transitionManager == null)
        {
            transitionManager = LevelVideoTransitionManager.Instance;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered) return;

        if (!other.CompareTag(playerTag))
        {
            if (showDebugLogs)
                Debug.LogWarning($"[LevelExitTrigger] Not player! Tag: {other.tag}");
            return;
        }

        TriggerTransition();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (hasTriggered) return;

        if (other.CompareTag(playerTag))
        {
            TriggerTransition();
        }
    }

    private void TriggerTransition()
    {
        if (showDebugLogs)
            Debug.Log("[LevelExitTrigger] Transition triggered!");

        hasTriggered = true;

        if (transitionManager != null)
        {
            transitionManager.StartTransition();
        }
        else
        {
            Debug.LogError("[LevelExitTrigger] TransitionManager is null!");
            FallbackTransition();
        }

        DisablePlayerInput();
    }

    private void FallbackTransition()
    {
        GameSpeed.Multiplier = 0f;
        StartCoroutine(FallbackDelay());
    }

    private System.Collections.IEnumerator FallbackDelay()
    {
        yield return new WaitForSecondsRealtime(2f);
        
        LevelManager levelManager = LevelManager.Instance;
        if (levelManager != null)
        {
            levelManager.NextLevel();
        }
        
        GameSpeed.Multiplier = 1f;
    }

    private void DisablePlayerInput()
    {
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
        {
            PlayerMovement movement = player.GetComponent<PlayerMovement>();
            if (movement != null)
            {
                movement.enabled = false;
            }
        }
    }

    public void ResetTrigger()
    {
        hasTriggered = false;
    }
}