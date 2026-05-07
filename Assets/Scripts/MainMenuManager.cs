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
    public UnityEngine.UI.Button newGameButton;

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
        

        // Logic for which root panel to show
        bool showPlayMenu = isAuthenticated && !blockForUsername;
        bool showAuthOverlay = !isAuthenticated && !blockForUsername;

        if (showPlayMenu)
        {
            ShowPlayMenuImmediate();
        }
        else
        {
            if (mainMenuView != null) mainMenuView.SetActive(false);
            if (authPanel != null) authPanel.SetActive(showAuthOverlay);
            if (characterSelectPanel != null) characterSelectPanel.SetActive(false);
        }
    }

    private void ShowPlayMenuImmediate()
    {
        
        if (mainMenuView != null) mainMenuView.SetActive(true);
        if (authPanel != null) authPanel.SetActive(false);
        if (characterSelectPanel != null) characterSelectPanel.SetActive(false);

        // Ensure New Game and Continue are VISIBLE
        if (newGameButton != null) 
        {
            newGameButton.gameObject.SetActive(true);
            newGameButton.interactable = true;
        }

        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(true);
            // Default to non-interactable until we ARE SURE there is progress
            // (either from local PlayerPrefs baseline or background sync)
            UpdateContinueButtonState();
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
            // Note: LastLevelReached and LastSavedXP look at local PlayerPrefs if _cachedProgress is null
            bool hasProgress = AuthManager.Instance.LastLevelReached > 1 || AuthManager.Instance.LastSavedXP > 0;
            
            if (continueButton.interactable != hasProgress)
            {
                continueButton.interactable = hasProgress;
            }
        }
    }

    public void ContinueGame()
    {
        AuthManager.IsNewGameStart = false;

        var progress = ProgressManager.LoadProgress();

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
        AuthManager.IsNewGameStart = true;

        // Reset ONLY run progress
        ProgressManager.CurrentLevel = 1;
        ProgressManager.XP = 0;
        
        // Ensure backend sync flags are also reset if needed
        ProgressManager.ResetRunProgressOnly(); 
        ProgressManager.Save();

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
