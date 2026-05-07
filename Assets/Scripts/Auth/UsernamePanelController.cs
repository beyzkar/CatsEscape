using UnityEngine;
using TMPro;
using UnityEngine.UI;
using CatsEscape.Auth;
using CatsEscape.Networking;

public class UsernamePanelController : MonoBehaviour
{
    [Header("UI References")]
    public GameObject panel;
    public TMP_InputField usernameInput;
    public Button confirmButton;
    public Button exitButton;
    public TMP_Text warningText;
    [Header("Optional: Menu Visibility")]
    public GameObject mainMenuView;
    public GameObject authPanel;

    private string _defaultName;

    private void Awake()
    {
        if (panel != null) panel.SetActive(false);
        if (warningText != null) warningText.text = "";

        if (AuthManager.Instance != null)
        {
            AuthManager.Instance.OnUsernameRequired += ShowPanel;
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        }

        if (exitButton != null)
        {
            exitButton.onClick.AddListener(OnExitButtonClicked);
        }
    }

    private void OnDestroy()
    {
        if (AuthManager.Instance != null)
        {
            AuthManager.Instance.OnUsernameRequired -= ShowPanel;
        }
    }

    public void ShowPanel(string defaultName)
    {
        _defaultName = defaultName;
        
        if (panel != null) panel.SetActive(true);
        if (warningText != null) warningText.text = "";
        if (mainMenuView != null) mainMenuView.SetActive(false);
        if (authPanel != null) authPanel.SetActive(false);


        if (AuthManager.Instance.IsGuest)
        {
            // For guests, fetch the next available GuestN from backend
            GameDataApiClient.Instance.GetNextGuestUsername((nextGuestName) => {
                _defaultName = nextGuestName;
                if (usernameInput != null) 
                {
                    usernameInput.text = nextGuestName;
                }
            });
        }
        else
        {
            if (usernameInput != null) 
            {
                usernameInput.text = defaultName;
            }
        }
    }

    public void OnExitButtonClicked()
    {
        // Cancel the current session/login if user exits before picking a name
        if (AuthManager.Instance != null)
        {
            AuthManager.Instance.SignOut();
        }

        if (panel != null) panel.SetActive(false);
        if (authPanel != null) authPanel.SetActive(true);
    }

    public void OnConfirmButtonClicked()
    {
        // Get name from input field. If empty, use the pre-filled default
        string chosenName = usernameInput.text.Trim();
        
        if (string.IsNullOrEmpty(chosenName))
        {
            chosenName = _defaultName;
        }

        if (string.IsNullOrEmpty(chosenName))
        {
            ShowWarning("Username cannot be empty.");
            return;
        }

        confirmButton.interactable = false;
        ShowWarning("Checking availability...");

        GameDataApiClient.Instance.SetUsername(chosenName, (success, result) => {
            confirmButton.interactable = true;

            if (success)
            {
                AuthManager.Instance.FinalizeUsername(result);
                if (panel != null) panel.SetActive(false);
                if (mainMenuView != null) mainMenuView.SetActive(true);
            }
            else
            {
                if (result == "USERNAME_TAKEN")
                {
                    ShowWarning("Username is already taken.");
                }
                else if (result == "USERNAME_EMPTY")
                {
                    ShowWarning("Username cannot be empty.");
                }
                else if (result == "NETWORK_ERROR")
                {
                    Debug.LogWarning("[USERNAME] Server unreachable. Finalizing locally.");
                    ShowWarning("Sunucuya bağlanılamadı. Yerel olarak devam ediliyor...");
                    
                    // Allow proceeding anyway - Requirement 9
                    AuthManager.Instance.FinalizeUsername(chosenName);
                    
                    // Small delay to let user see the message before closing
                    Invoke(nameof(HidePanelAndShowMenu), 1.5f);
                }
                else
                {
                    ShowWarning("Could not save username. Please try again.");
                }
            }
        });
    }

    private void HidePanelAndShowMenu()
    {
        if (panel != null) panel.SetActive(false);
        if (mainMenuView != null) mainMenuView.SetActive(true);
    }

    private void ShowWarning(string message)
    {
        if (warningText != null)
        {
            warningText.text = message;
        }
    }
}
