using UnityEngine;
using UnityEngine.SceneManagement;
using CatsEscape.Auth;

public class MainMenuManager : MonoBehaviour
{
    [Header("Settings")]
    public string gameSceneName = "GameScene";

    [Header("UI Panels")]
    public GameObject characterSelectPanel; 
    public GameObject mainMenuView;         
    public GameObject authPanel;            
    public UnityEngine.UI.Button continueButton;

    private void Start()
    {
        if (characterSelectPanel != null) characterSelectPanel.SetActive(false);
        
        UpdateAuthUI();

        if (AuthManager.Instance != null)
        {
            AuthManager.Instance.OnLoginSuccess += UpdateAuthUI;
            AuthManager.Instance.OnGuestLogin += UpdateAuthUI;
            AuthManager.Instance.OnLogout += UpdateAuthUI;
            AuthManager.Instance.OnProgressSynced += UpdateContinueButtonState;
            AuthManager.Instance.OnUsernameRequired += HandleUsernameRequired;
            AuthManager.Instance.OnUsernameFlowResolved += UpdateAuthUI;
        }
    }

    private void OnDestroy()
    {
        if (AuthManager.Instance != null)
        {
            AuthManager.Instance.OnLoginSuccess -= UpdateAuthUI;
            AuthManager.Instance.OnGuestLogin -= UpdateAuthUI;
            AuthManager.Instance.OnLogout -= UpdateAuthUI;
            AuthManager.Instance.OnProgressSynced -= UpdateContinueButtonState;
            AuthManager.Instance.OnUsernameRequired -= HandleUsernameRequired;
            AuthManager.Instance.OnUsernameFlowResolved -= UpdateAuthUI;
        }
    }

    private void UpdateAuthUI()
    {
        if (AuthManager.Instance == null) return;

        bool isAuthenticated = AuthManager.Instance.IsAuthenticated;
        bool blockForUsername = AuthManager.Instance.IsUsernameRequiredFlowPending;
        bool showMainMenu = isAuthenticated && !blockForUsername;
        
        if (mainMenuView != null) mainMenuView.SetActive(showMainMenu);
        if (authPanel != null) authPanel.SetActive(!isAuthenticated && !blockForUsername);
        
        if (showMainMenu) 
        {
            UpdateContinueButtonState();
        }
        else 
        {
            if (characterSelectPanel != null) characterSelectPanel.SetActive(false);
        }
    }

    private void HandleUsernameRequired(string _)
    {
        UpdateAuthUI();
    }

    private void UpdateContinueButtonState()
    {
        if (continueButton != null && AuthManager.Instance != null)
        {
            bool hasProgress = AuthManager.Instance.LastLevelReached > 1 || AuthManager.Instance.LastSavedXP > 0;
            continueButton.interactable = hasProgress;
        }
    }

    public void ContinueGame()
    {
        Debug.Log("[MAIN_MENU] Continue clicked.");
        
        AuthManager.IsNewGameStart = false;
        Debug.Log("[CONTINUE] IsNewGameStart=false");

        var progress = ProgressManager.LoadProgress();
        Debug.Log($"[CONTINUE] Loading saved progress: level={progress.currentLevel}, xp={progress.xp}");

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

    public void NewStartGame()
    {
        Debug.Log("[NEW_GAME] Starting fresh run");

        AuthManager.IsNewGameStart = true;
        Debug.Log("[NEW_GAME] IsNewGameStart=true");

        // Reset ONLY run progress
        ProgressManager.CurrentLevel = 1;
        ProgressManager.XP = 0;
        
        // Ensure backend sync flags are also reset if needed
        ProgressManager.ResetRunProgressOnly(); 
        ProgressManager.Save();

        Debug.Log("[NEW_GAME] Progress reset: level=1, xp=0");

        // Start the game flow (which might show character selection)
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
        SceneManager.LoadScene(gameSceneName);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
