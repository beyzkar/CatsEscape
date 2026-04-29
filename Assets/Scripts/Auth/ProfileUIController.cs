using UnityEngine;
using TMPro;
using CatsEscape.Auth;

namespace CatsEscape.UI
{
    public class ProfileUIController : MonoBehaviour
    {
        [Header("Profile Popup Panel")]
        [Tooltip("The main container GameObject for the Profile UI.")]
        public GameObject profilePopup;

        [Header("UI Text Fields (TMP)")]
        public TMP_Text playerNameText;
        public TMP_Text lastLevelText;
        public TMP_Text highestXPText;
        public TMP_Text totalCompletionsText;
        public TMP_Text emailText;

        /// <summary>
        /// Opens the profile popup and populates it with the latest data from AuthManager.
        /// </summary>
        public void OpenProfile()
        {
            if (profilePopup == null) 
            {
                Debug.LogError("[ProfileUI] ProfilePopup reference is missing in the Inspector!");
                return;
            }

            if (AuthManager.Instance == null)
            {
                Debug.LogError("[ProfileUI] AuthManager instance not found!");
                return;
            }

            PopulateProfileData();
            profilePopup.SetActive(true);
        }

        /// <summary>
        /// Populates the UI fields with Google or Guest specific information.
        /// </summary>
        private void PopulateProfileData()
        {
            AuthManager auth = AuthManager.Instance;
            bool isGoogle = auth.IsUserLoggedIn();

            // 1. Name Handling (Fallback to "Player" for Google, "Guest" for Guest)
            if (playerNameText != null)
                playerNameText.text = auth.GetFormattedUserName();

            // 3. Email Handling (Only show for Google)
            if (emailText != null)
            {
                string email = auth.UserEmail;
                bool showEmail = isGoogle && !string.IsNullOrEmpty(email);
                emailText.text = showEmail ? email : "-";
                // Optionally keep visible but with "-" or hide it:
                emailText.gameObject.SetActive(true); 
            }

            // 4. Stats Handling (From persistent PlayerPrefs via AuthManager)
            if (lastLevelText != null)
                lastLevelText.text = "Last Level: " + auth.LastLevelReached;

            if (highestXPText != null)
                highestXPText.text = "Highest XP: " + auth.HighestXP;

            if (totalCompletionsText != null)
                totalCompletionsText.text = "Total Completed: " + auth.TotalCompletions;

            Debug.Log($"[ProfileUI] Data populated. Type: {auth.GetLoginType()}, User: {auth.GetFormattedUserName()}");
        }

        /// <summary>
        /// Closes the profile popup without affecting session data.
        /// </summary>
        public void CloseProfile()
        {
            if (profilePopup != null)
            {
                profilePopup.SetActive(false);
            }
        }

        /// <summary>
        /// Signs the user out (clearing Firebase and Guest state) and returns to MainMenu.
        /// </summary>
        public void SignOutAndQuit()
        {
            if (AuthManager.Instance != null)
            {
                // Close popup first for clean UI state
                CloseProfile();
                
                // Triggers full sign out and scene load
                AuthManager.Instance.SignOutAndReturnToMenu();
            }
            else
            {
                Debug.LogError("[ProfileUI] Cannot sign out: AuthManager missing.");
                // Fallback to reload if everything fails
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
            }
        }

        /// <summary>
        /// Returns to the Main Menu without signing out. 
        /// Used for simple navigation from GameScene back to the baseline.
        /// </summary>
        public void OnMainMenuClicked()
        {
            Debug.Log("[ProfileUI] Main Menu button clicked. Preparing for transition...");

            // 1. Send Abandoned Result if there is an active run
            if (CatsEscape.Networking.GameplayStatsTracker.Instance != null)
            {
                // This ensures game_end/abandoned events are sent to backend
                CatsEscape.Networking.GameplayStatsTracker.Instance.SendAbandonedResult();
            }

            // 2. Safe State Resets
            Time.timeScale = 1f;
            GameSpeed.Multiplier = 1f;

            // 3. UI Cleanup
            CloseProfile();

            // 4. Scene Transition
            // Note: Using "MainMenu" as identified in the project's scene files.
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }
}
