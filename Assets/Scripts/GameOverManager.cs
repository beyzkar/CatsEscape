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

            
            GameplayStatsTracker.Instance.TrackLevelResult("failed");
        }
    }

    public void Replay()
    {
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.RestartCurrentLevel();
        }
        else
        {
            // Fallback if LevelManager is missing
            Time.timeScale = 1f;
            GameSpeed.Multiplier = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    public void LoadMainMenu()
    {
        
        // Try to send abandoned if not already sent (e.g. if player leaves Game Over screen)
        // But in ShowGameOver we already set hasSentResult = true, so this won't trigger unless logic changes.
        if (GameplayStatsTracker.Instance != null)
        {
            GameplayStatsTracker.Instance.SendAbandonedResult();
        }

        if (CatsEscape.Auth.AuthManager.Instance != null)
        {
        }

        // REMOVED: LevelManager.ResetPersistentLevel() - We want to preserve progress when returning to menu.

        // Reset time and speed scales before returning to menu
        Time.timeScale = 1f;
        GameSpeed.Multiplier = 1f;
        
        // Load the Main Menu scene
        SceneManager.LoadScene("MainMenu");
    }
}
