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

        // Send results to backend
        if (CatsEscape.Networking.GameDataApiClient.Instance != null)
        {
            if (GameplayStatsTracker.Instance != null)
            {
                Debug.Log("[RunState] Game ended result=failed");
                GameplayStatsTracker.Instance.hasSentGameEnd = true;
                GameplayStatsTracker.Instance.hasSentLevelResult = true;
                GameplayStatsTracker.Instance.hasActiveLevelRun = false;
            }
            CatsEscape.Networking.GameDataApiClient.Instance.SendLevelResult("failed");
            int currentLevel = LevelManager.Instance != null ? LevelManager.Instance.currentLevel : 1;
            CatsEscape.Networking.GameDataApiClient.Instance.SendActivity("game_end", currentLevel, "failed");
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
        // Try to send abandoned if not already sent (e.g. if player leaves Game Over screen)
        // But in ShowGameOver we already set hasSentResult = true, so this won't trigger unless logic changes.
        if (GameplayStatsTracker.Instance != null)
        {
            GameplayStatsTracker.Instance.SendAbandonedResult();
        }

        // Global hard reset for XP when returning to start
        // XP reset is now handled centrally in Awake via InitializeForLevel

        // Seviye sürekliliğini sıfırla
        LevelManager.ResetPersistentLevel();

        // Reset time and speed scales before returning to menu
        Time.timeScale = 1f;
        GameSpeed.Multiplier = 1f;
        
        // Load the Main Menu scene
        SceneManager.LoadScene("MainMenu");
    }
}
