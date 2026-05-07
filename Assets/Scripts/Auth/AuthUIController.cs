using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Text;

namespace CatsEscape.Auth
{
    public class AuthUIController : MonoBehaviour
    {
        [Header("Panels")]
        public GameObject loginPanel;
        public GameObject statusPanel;

        [Header("Buttons")]
        public Button googleLoginButton;
        public Button guestButton;

        [Header("Isolation Mode (Surgical Fix 3.0)")]
        [Tooltip("If false, the Google Login button will be disabled for testing.")]
        public bool allowGoogleLogin = true;
        [Tooltip("If false, the Guest button will be disabled for testing.")]
        public bool allowGuestLogin = true;

        private void Start()
        {
            
            // Handle Isolation Mode Toggles
            if (googleLoginButton != null)
            {
                googleLoginButton.gameObject.SetActive(allowGoogleLogin);
                if (!allowGoogleLogin) Debug.LogWarning("[AUTH_FLOW] Google Login button DISABLED via script settings.");
            }
            
            if (guestButton != null)
            {
                guestButton.gameObject.SetActive(allowGuestLogin);
                if (!allowGuestLogin) Debug.LogWarning("[AUTH_FLOW] Guest button DISABLED via script settings.");
            }

            if (AuthManager.Instance != null)
            {
                AuthManager.Instance.OnLoginSuccess += OnLoginSuccess;
                AuthManager.Instance.OnLogout += OnLogout;
                AuthManager.Instance.OnGuestLogin += HandleGuestLogin;
                AuthManager.Instance.OnLoginFailed += ShowErrorMessage;
            }

            UpdateUI();
        }

        private void OnDestroy()
        {
            if (AuthManager.Instance != null)
            {
                AuthManager.Instance.OnLoginSuccess -= OnLoginSuccess;
                AuthManager.Instance.OnLogout -= OnLogout;
                AuthManager.Instance.OnGuestLogin -= HandleGuestLogin;
                AuthManager.Instance.OnLoginFailed -= ShowErrorMessage;
            }
        }

        private void OnLoginSuccess() { HandleGuestLogin(); }
        private void OnLogout() { UpdateUI(); }

        private void HandleGuestLogin()
        {
            if (AuthManager.Instance != null)
            {
                AuthManager.Instance.StartCoroutine(DeferredDeactivationRoutine());
            }
            else
            {
                DirectDeactivation();
            }
        }

        private System.Collections.IEnumerator DeferredDeactivationRoutine()
        {
            yield return new WaitForEndOfFrame();
            DirectDeactivation();
        }

        private void DirectDeactivation()
        {
            try
            {
                this.gameObject.SetActive(false);
                
                // Note: We removed CompleteGuestFlowAndStartGame from here because
                // in the new MainScene flow, we want to stay in the menu after auth.
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AUTH_FLOW] Error in DirectDeactivation: {ex.Message}");
            }
        }

        public async void OnGoogleLoginClicked()
        {
            if (!allowGoogleLogin)
            {
                Debug.LogWarning("[AUTH_FLOW] Click REJECTED: Google Login is disabled in AuthUIController settings.");
                return;
            }

            SetButtonsInteractable(false);
            
            try
            {
                if (AuthManager.Instance != null)
                {
                    await AuthManager.Instance.SignInWithGoogleAsync();
                }
                else
                {
                    Debug.LogError("[AUTH_FLOW] CRITICAL ERROR: AuthManager.Instance is NULL! Cannot proceed with sign-in.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AUTH_FLOW] EXCEPTION in AuthUIController.OnGoogleLoginClicked: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                SetButtonsInteractable(true);
            }
        }

        public void ContinueAsGuest()
        {
            if (!allowGuestLogin) return;

            if (AuthManager.Instance != null)
            {
                AuthManager.Instance.ContinueAsGuest();
            }
            else
            {
                HandleGuestLogin();
            }
        }


        private void UpdateUI()
        {
            try
            {
                if (AuthManager.Instance == null) return;

                bool loggedIn = AuthManager.Instance.IsUserLoggedIn();
                if (loginPanel != null) loginPanel.SetActive(!loggedIn);
                if (statusPanel != null) statusPanel.SetActive(loggedIn);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Auth-Diagnostic][UI] Error in UpdateUI: {ex.Message}");
            }
        }

        private void ShowErrorMessage(string message)
        {
            Debug.Log($"[Auth-Diagnostic][UI] Auth Error Displayed: {message}");
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (googleLoginButton != null) googleLoginButton.interactable = interactable;
            if (guestButton != null) guestButton.interactable = interactable;
        }
    }
}
