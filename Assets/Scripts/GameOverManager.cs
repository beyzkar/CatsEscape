using UnityEngine;
using UnityEngine.SceneManagement;
using CatsEscape.Networking;
using CatsEscape.Auth;

public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance { get; private set; }

    [Header("UI Panels")]
    public GameObject gameOverPanel;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        // Check for Space key to retry
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (gameOverPanel != null && gameOverPanel.activeInHierarchy)
            {
                Replay();
            }
        }
    }

    public void ShowGameOver()
    {
        // Set time and speed to 0 immediately
        Time.timeScale = 0f;
        GameSpeed.Multiplier = 0f;

        // JUST show the GameOver panel (No scoreboard/leaderboard on death as requested)
        if (gameOverPanel != null) 
        {
            gameOverPanel.SetActive(true);
        }

        // Send results to backend via centralized tracker
        if (GameplayStatsTracker.Instance != null)
        {
            string uid = (AuthManager.Instance != null) ? AuthManager.Instance.UserId : "UNKNOWN";
            float duration = GameplayStatsTracker.Instance.GetLevelDuration();
            int currentLevel = LevelManager.Instance != null ? LevelManager.Instance.currentLevel : 1;
            int xp = (ScoreManager.Instance != null ? ScoreManager.Instance.GetTotalXP() : 0);

            Debug.Log($"[LEVEL] Details -> UID: {uid}, Level: {currentLevel}, Duration: {duration:F2}s, XP: {xp}");
            
            GameplayStatsTracker.Instance.TrackLevelResult("failed");
        }
    }

    public void Replay()
    {
        // Reset time and speed scales
        Time.timeScale = 1f;
        GameSpeed.Multiplier = 1f;
        
        // Reload the current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void LoadMainMenu()
    {
        Debug.Log("[MAIN_MENU] Button clicked from GameOver");
        
        // Try to send abandoned if not already sent (e.g. if player leaves Game Over screen)
        // But in ShowGameOver we already set hasSentResult = true, so this won't trigger unless logic changes.
        if (GameplayStatsTracker.Instance != null)
        {
            GameplayStatsTracker.Instance.SendAbandonedResult();
        }

        if (CatsEscape.Auth.AuthManager.Instance != null)
        {
            if (CatsEscape.Auth.AuthManager.Instance.PendingNewGameReset)
            {
                Debug.Log("[MAIN_MENU] Final completed, resetting run progress.");
            }
            else
            {
                Debug.Log("[MAIN_MENU] Not final completed, preserving progress.");
            }
            
            Debug.Log($"[PROGRESS] Saved currentLevel={CatsEscape.Auth.AuthManager.Instance.LastLevelReached}, xp={CatsEscape.Auth.AuthManager.Instance.LastSavedXP}, pendingNewGameReset={CatsEscape.Auth.AuthManager.Instance.PendingNewGameReset}");
        }

        // REMOVED: LevelManager.ResetPersistentLevel() - We want to preserve progress when returning to menu.
        Debug.Log("[MAIN_MENU] Returning to Main Menu scene. Progress handling complete.");

        // Reset time and speed scales before returning to menu
        Time.timeScale = 1f;
        GameSpeed.Multiplier = 1f;
        
        // Load the Main Menu scene
        SceneManager.LoadScene("MainMenu");
    }
}
