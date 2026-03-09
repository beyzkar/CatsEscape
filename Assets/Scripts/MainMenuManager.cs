using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public string gameSceneName = "GameScene";

    public void StartGame()
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
