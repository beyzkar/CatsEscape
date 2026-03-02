using UnityEngine;
using UnityEngine.SceneManagement;

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

    public void ShowGameOver()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
        
        Time.timeScale = 0f;
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
        // Reset time and speed scales before returning to menu
        Time.timeScale = 1f;
        GameSpeed.Multiplier = 1f;
        
        // Load the Main Menu scene
        SceneManager.LoadScene("MainMenu");
    }
}
