using UnityEngine;
using UnityEngine.SceneManagement;
using CatsEscape.Auth; // Namespace eklendi

public class MainMenuManager : MonoBehaviour
{
    public string gameSceneName = "GameScene";
    public GameObject characterSelectPanel; 
    public GameObject mainMenuView;         
    public GameObject authPanel;            

    private void Start()
    {
        // Başlangıçta panelleri hazırlayalım
        if (characterSelectPanel != null) characterSelectPanel.SetActive(false);
        
        // Eğer zaten giriş yapılmışsa (Google veya Guest) direkt menüyü göster
        CheckAuthState();

        // Auth olaylarını dinle (Geç kalmış silent login'ler veya yeni girişler için)
        if (AuthManager.Instance != null)
        {
            AuthManager.Instance.OnLoginSuccess += HandleAuthSuccess;
            AuthManager.Instance.OnGuestLogin += HandleAuthSuccess;
            AuthManager.Instance.OnLogout += HandleLogout;
        }
    }

    private void OnDestroy()
    {
        if (AuthManager.Instance != null)
        {
            AuthManager.Instance.OnLoginSuccess -= HandleAuthSuccess;
            AuthManager.Instance.OnGuestLogin -= HandleAuthSuccess;
            AuthManager.Instance.OnLogout -= HandleLogout;
        }
    }

    private void CheckAuthState()
    {
        if (AuthManager.Instance == null) return;

        bool isAuthenticated = AuthManager.Instance.IsAuthenticated;
        Debug.Log($"[MainMenu] Initial Auth State: {isAuthenticated}");

        if (mainMenuView != null) mainMenuView.SetActive(isAuthenticated);
        if (authPanel != null) authPanel.SetActive(!isAuthenticated);
    }

    private void HandleAuthSuccess()
    {
        Debug.Log("[MainMenu] Auth Success received. Showing Menu.");
        if (mainMenuView != null) mainMenuView.SetActive(true);
        if (authPanel != null) authPanel.SetActive(false);
    }

    private void HandleLogout()
    {
        Debug.Log("[MainMenu] Logout received. Showing Auth Panel.");
        if (mainMenuView != null) mainMenuView.SetActive(false);
        if (authPanel != null) authPanel.SetActive(true);
        if (characterSelectPanel != null) characterSelectPanel.SetActive(false);
    }

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
            PerformStartGame();
        }
    }

    public void PerformStartGame()
    {
        Time.timeScale = 1f;
        if (GameSpeed.Multiplier <= 0f) GameSpeed.Multiplier = 1f;

        // Reset level progression if we are a guest starting fresh
        if (AuthManager.Instance != null && AuthManager.Instance.IsGuest)
        {
            LevelManager.ResetSavedLevel();
        }
        
        SceneManager.LoadScene(gameSceneName);
    }

    public void QuitGame()
    {
        Debug.Log("Game Quitting...");
        Application.Quit();
    }
}
