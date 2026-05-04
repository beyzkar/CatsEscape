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
    public TMP_Text warningText;
    [Header("Optional: Menu Visibility")]
    public GameObject mainMenuView;
    public GameObject authPanel;

    private string _defaultName;

    private void Awake()
    {
        Debug.Log("[USERNAME_UI] Controller Awake");
        if (panel != null) panel.SetActive(false);
        if (warningText != null) warningText.text = "";

        if (AuthManager.Instance != null)
        {
            AuthManager.Instance.OnUsernameRequired += ShowPanel;
            Debug.Log("[USERNAME_UI] Subscribed to OnUsernameRequired");
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmButtonClicked);
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
        Debug.Log($"[USERNAME_UI] Show panel called with defaultName={defaultName}");
        _defaultName = defaultName;
        
        if (panel != null) panel.SetActive(true);
        Debug.Log($"[USERNAME_UI] UsernamePanel active={(panel != null && panel.activeSelf)}");
        if (warningText != null) warningText.text = "";
        if (mainMenuView != null) mainMenuView.SetActive(false);
        if (authPanel != null) authPanel.SetActive(false);

        Debug.Log("[USERNAME] Opening username panel.");

        if (AuthManager.Instance.IsGuest)
        {
            // For guests, fetch the next available GuestN from backend
            GameDataApiClient.Instance.GetNextGuestUsername((nextGuestName) => {
                _defaultName = nextGuestName;
                if (usernameInput != null) 
                {
                    usernameInput.text = nextGuestName;
                    Debug.Log($"[USERNAME_UI] Input text set to={nextGuestName}");
                    Debug.Log($"[USERNAME] Guest default username: {nextGuestName}");
                    Debug.Log($"[USERNAME] Input field pre-filled with: {nextGuestName}");
                }
            });
        }
        else
        {
            if (usernameInput != null) 
            {
                usernameInput.text = defaultName;
                Debug.Log($"[USERNAME_UI] Input text set to={defaultName}");
                Debug.Log($"[USERNAME] Google default username: {defaultName}");
                Debug.Log($"[USERNAME] Input field pre-filled with: {defaultName}");
            }
        }
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
        Debug.Log($"[USERNAME] Checking availability: {chosenName}");

        GameDataApiClient.Instance.SetUsername(chosenName, (success, result) => {
            confirmButton.interactable = true;

            if (success)
            {
                Debug.Log($"[USERNAME] Username saved: {result}");
                AuthManager.Instance.FinalizeUsername(result);
                if (panel != null) panel.SetActive(false);
                if (mainMenuView != null) mainMenuView.SetActive(true);
            }
            else
            {
                if (result == "USERNAME_TAKEN")
                {
                    ShowWarning("Username is already taken.");
                    Debug.Log("[USERNAME] Username taken.");
                }
                else if (result == "USERNAME_EMPTY")
                {
                    ShowWarning("Username cannot be empty.");
                }
                else
                {
                    ShowWarning("Could not save username. Please try again.");
                }
            }
        });
    }

    private void ShowWarning(string message)
    {
        if (warningText != null)
        {
            warningText.text = message;
        }
    }
}
