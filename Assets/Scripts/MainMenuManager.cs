using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public string gameSceneName = "GameScene";
    public GameObject characterSelectPanel; // Unity'den seçim panelini buraya sürükleyin
    public GameObject mainMenuView;         // Ana menü yazılarını içeren obje
    public GameObject authPanel;            // Authentication paneli (opsiyonel)

    public void StartGame()
    {
        // Direkt başlamak yerine seçim panelini aç
        if (characterSelectPanel != null)
        {
            characterSelectPanel.SetActive(true);
            if (mainMenuView != null) mainMenuView.SetActive(false);
        }
        else
        {
            // Eğer panel yoksa (unuttuysan) güvenli hata ayıklama için direkt başlat
            PerformStartGame();
        }
    }

    public void PerformStartGame()
    {
        // Reset time and speed scales before starting
        Time.timeScale = 1f;
        if (GameSpeed.Multiplier <= 0f) GameSpeed.Multiplier = 1f;
        
        SceneManager.LoadScene(gameSceneName);
    }

    public void QuitGame()
    {
        Debug.Log("Game Quitting...");
        Application.Quit();
    }
}
