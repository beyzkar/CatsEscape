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

        // Leaderboard check and panel sequencing
        if (LeaderboardManager.Instance != null && ScoreManager.Instance != null)
        {
            // The GameOver panel should open AFTER the leaderboard is closed
            LeaderboardManager.Instance.nextPanelToOpen = gameOverPanel;
            
            // This will open Save Score panel or Leaderboard panel
            LeaderboardManager.Instance.CheckForHighScore(ScoreManager.Instance.GetTotalXP());
            
            // Ensure GameOver panel is actually OFF for now to prevent overlapping
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
        }
        else
        {
            // Fallback: If no leaderboard system, just show GameOver panel
            if (gameOverPanel != null) gameOverPanel.SetActive(true);
        }

        // Send results to backend
        if (CatsEscape.Networking.GameDataApiClient.Instance != null)
            CatsEscape.Networking.GameDataApiClient.Instance.SendLevelResult("failed");
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
