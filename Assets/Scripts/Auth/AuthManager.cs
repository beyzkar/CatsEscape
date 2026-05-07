using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using CatsEscape.Networking;

namespace CatsEscape.Auth
{
    public class AuthManager : MonoBehaviour
    {
        public static AuthManager Instance { get; private set; }
        public static bool IsNewGameStart = false; // Flag to force Level 1 XP 0

        [Header("Settings")]
        public string webClientId;
        public bool autoSilentLogin = true;

        [Header("Testing Modes")]
        [Tooltip("If false, Firebase initialization will be skipped entirely to test Guest-only isolation.")]
        public bool enableFirebaseInit = true;
        [Tooltip("If false, silent login attempts will be skipped.")]
        public bool enableSilentLogin = true;

        private IAuthService _authService;
        public IAuthService Service => _authService;

        public event Action OnLoginSuccess;
        public event Action OnLogout;
        public event Action OnGuestLogin;
        public event Action<string> OnLoginFailed;
        public event Action OnProgressSynced;
        public event Action<string> OnUsernameRequired;
        public event Action OnUsernameFlowResolved;

        public string UserName { get; private set; }
        public bool IsUsernameRequiredFlowPending { get; private set; }

        private PlayerProgressDto _cachedProgress;

        // --- NEW: Persistent Auth & Stats (UID Based) ---
        private string GetKey(string baseKey) 
        {
            string uid = UserId;
            if (string.IsNullOrEmpty(uid) || uid == "NONE") 
            {
                if (IsGuest) uid = "GUEST_SESSION";
                else return $"TEMP_{baseKey}"; // Fallback to prevent overwriting real data
            }
            return $"{uid}_{baseKey}";
        }

        private const string GUEST_PREF_KEY = "Auth_IsGuest";
        private const string HIGHEST_XP_KEY = "Stats_HighestXP";
        private const string TOTAL_COMPLETIONS_KEY = "Stats_TotalCompletions";
        private const string LAST_LEVEL_KEY = "Stats_LastLevelReached";
        private const string LAST_SAVED_XP_KEY = "Stats_LastSavedXP";
        private const string PENDING_RESET_KEY = "Stats_PendingReset";
        private const string GUEST_USERNAME_KEY = "Auth_GuestUserName";
        private const string GOOGLE_USERNAME_KEY = "Auth_GoogleUserName";

        public bool IsGuest
        {
            get => PlayerPrefs.GetInt(GUEST_PREF_KEY, 0) == 1;
            set { PlayerPrefs.SetInt(GUEST_PREF_KEY, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public int HighestXP
        {
            get 
            {
                if (_cachedProgress != null) return _cachedProgress.highestXP;
                return PlayerPrefs.GetInt(GetKey(HIGHEST_XP_KEY), 0);
            }
            set 
            { 
                if (value > HighestXP) 
                { 
                    if (_cachedProgress != null) _cachedProgress.highestXP = value;
                    
                    // Safety: Don't persist to PlayerPrefs if we are in a transition/none state
                    string uid = UserId;
                    if (!string.IsNullOrEmpty(uid) && uid != "NONE")
                    {
                    PlayerPrefs.SetInt(GetKey(HIGHEST_XP_KEY), value); 
                    PlayerPrefs.Save(); 
                }
                } 
            }
        }

        public int TotalCompletions
        {
            get 
            {
                if (_cachedProgress != null) return _cachedProgress.totalCompletions;
                return PlayerPrefs.GetInt(GetKey(TOTAL_COMPLETIONS_KEY), 0);
            }
            set 
            { 
                if (_cachedProgress != null) _cachedProgress.totalCompletions = value;
                PlayerPrefs.SetInt(GetKey(TOTAL_COMPLETIONS_KEY), value); 
                PlayerPrefs.Save(); 
            }
        }

        public int LastLevelReached
        {
            get 
            {
                // REMOVED: if (IsGuest) return 1; - Guests now preserve level progress
                if (_cachedProgress != null) return _cachedProgress.lastLevelReached;
                return PlayerPrefs.GetInt(GetKey(LAST_LEVEL_KEY), 1);
            }
            set 
            { 
                // REMOVED: if (IsGuest) return; - Guests now preserve level progress
                if (_cachedProgress != null) _cachedProgress.lastLevelReached = value;
                PlayerPrefs.SetInt(GetKey(LAST_LEVEL_KEY), value); 
                PlayerPrefs.Save(); 
            }
        }

        public int LastSavedXP
        {
            get => PlayerPrefs.GetInt(GetKey(LAST_SAVED_XP_KEY), 0);
            set 
            { 
                PlayerPrefs.SetInt(GetKey(LAST_SAVED_XP_KEY), value); 
                PlayerPrefs.Save(); 
            }
        }

        public bool PendingNewGameReset
        {
            get => PlayerPrefs.GetInt(GetKey(PENDING_RESET_KEY), 0) == 1;
            set 
            { 
                PlayerPrefs.SetInt(GetKey(PENDING_RESET_KEY), value ? 1 : 0); 
                PlayerPrefs.Save(); 
            }
        }

        public void ResetRunProgressOnly()
        {
            LastLevelReached = 1;
            LastSavedXP = 0;
            PendingNewGameReset = false;
        }

        public bool IsAuthenticated => IsUserLoggedIn() || IsGuest;
        public string UserEmail => _authService?.UserEmail ?? "";
        public string UserId => _authService?.UserId ?? "NONE";

        private readonly ConcurrentQueue<Action> _mainThreadQueue = new ConcurrentQueue<Action>();
        private int _mainThreadId;

        private void Awake()
        {
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _authService = new FirebaseAuthService();
        }

        private void Update()
        {
            while (_mainThreadQueue.TryDequeue(out var action))
            {
                action?.Invoke();
            }
        }

        private async void Start()
        {
            if (enableFirebaseInit)
            {
                try
                {
                    await _authService.InitAsync(webClientId);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[AuthManager] Firebase Init Error: {ex.Message}");
                }
            }
            
            if (enableSilentLogin && _authService.IsInitialized)
            {
                bool success = await _authService.TrySilentLoginAsync();
                if (success)
                {
                    LogLoginState("Silent Google");
                    
                    // Try to restore cached username immediately (UID-based)
                    string cachedName = PlayerPrefs.GetString(GetKey(GOOGLE_USERNAME_KEY), "");
                    if (!string.IsNullOrEmpty(cachedName))
                    {
                        UserName = cachedName;
                        IsUsernameRequiredFlowPending = false;
                        EnqueueOnMainThread(() => OnUsernameFlowResolved?.Invoke());
                    }

                    InitializeProfileOnBackend();

                    // OPTIMISTIC: If we still don't have a UserName, use Google Display Name as a temporary fallback
                    // to show the menu INSTANTLY without waiting for backend sync.
                    if (string.IsNullOrEmpty(UserName))
                    {
                        string fallbackName = GetFormattedUserName();
                        if (!string.IsNullOrEmpty(fallbackName) && fallbackName != "Player" && fallbackName != "Guest Explorer")
                        {
                            UserName = fallbackName;
                            IsUsernameRequiredFlowPending = false;
                            EnqueueOnMainThread(() => OnUsernameFlowResolved?.Invoke());
                        }
                    }

                    EnqueueOnMainThread(() => OnLoginSuccess?.Invoke());
                }
            }
        }

        private void LogLoginState(string type)
        {
            // Reset only active session data, NOT the long-term progress keys
            ResetLocalSessionOnly();

            if (!IsGuest) 
            {
                // Load local baseline immediately so the UI isn't empty while waiting for sync
                ScoreManager.SetTotalXP(HighestXP);
            }
            else 
            {
                // Guests now preserve progress until logout per request
                ScoreManager.SetTotalXP(HighestXP);
            }
            if (CatsEscape.Networking.GameDataApiClient.Instance != null)
            {
                CatsEscape.Networking.GameDataApiClient.Instance.StartNewSession();
            }
        }

        private void ResetLocalSessionOnly()
        {
            // Clear things that shouldn't survive a user switch, but keep persistent PlayerPrefs intact
            _cachedProgress = null;
            LevelManager.ResetSavedLevel();
            // We don't call ScoreManager.ResetAllXP() here if we want to preserve the static value for a moment, 
            // but we usually want a clean slate before loading new data.
        }

        public async void ContinueAsGuest()
        {
            // NEW: Handle abandonment of current run BEFORE switching to Guest
            if (GameplayStatsTracker.Instance != null && GameDataApiClient.Instance != null && GameDataApiClient.Instance.IsNetworkAvailable)
            {
                await WaitForCoroutine(GameplayStatsTracker.Instance.TrySendAbandonedIfActiveCoroutine("switch_to_guest"));
            }

            try 
            {
                bool isOnline = (GameDataApiClient.Instance != null) && GameDataApiClient.Instance.IsNetworkAvailable;
                bool success = false;

                if (isOnline)
                {
                    success = await _authService.SignInAnonymouslyAsync();
                }
                else
                {
                    success = true; // Offline guest is always allowed
                }

                if (success)
                {
                    IsGuest = true;
                    LogLoginState(isOnline ? "Guest" : "Guest Offline");
                    
                    // Check for cached guest username
                    string cachedName = PlayerPrefs.GetString(GUEST_USERNAME_KEY, "");
                    if (!string.IsNullOrEmpty(cachedName))
                    {
                        UserName = cachedName;
                        IsUsernameRequiredFlowPending = false;
                        EnqueueOnMainThread(() => OnUsernameFlowResolved?.Invoke());
                    }
                    else
                    {
                        IsUsernameRequiredFlowPending = true;
                        // Trigger immediately for Guest per Requirement 9
                        string defaultName = "GuestPlayer"; 
                        EnqueueOnMainThread(() => OnUsernameRequired?.Invoke(defaultName));
                    }

                    if (isOnline)
                    {
                        InitializeProfileOnBackend();
                    }
                    else
                    {
                    }
                    
                    EnqueueOnMainThread(() => OnGuestLogin?.Invoke());
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AuthManager] Guest Login Failed: {ex.Message}");
                EnqueueOnMainThread(() => OnLoginFailed?.Invoke(ex.Message));
            }
        }

        private async Task WaitForCoroutine(System.Collections.IEnumerator routine)
        {
            if (routine == null) return;
            var tcs = new TaskCompletionSource<bool>();
            StartCoroutine(WaitRoutine(routine, tcs));
            await tcs.Task;
        }

        private System.Collections.IEnumerator WaitRoutine(System.Collections.IEnumerator routine, TaskCompletionSource<bool> tcs)
        {
            yield return routine;
            tcs.SetResult(true);
        }

        public async Task SignInWithGoogleAsync()
        {
            if (GameDataApiClient.Instance != null && !GameDataApiClient.Instance.IsNetworkAvailable)
            {
                EnqueueOnMainThread(() => OnLoginFailed?.Invoke("Google ile giriş için internet bağlantısı gerekli."));
                return;
            }

            // NEW: Handle abandonment of current run BEFORE switching to Google
            if (GameplayStatsTracker.Instance != null)
            {
                await WaitForCoroutine(GameplayStatsTracker.Instance.TrySendAbandonedIfActiveCoroutine("switch_to_google"));
            }

            try
            {
                bool success = await _authService.SignInWithGoogleAsync();
                if (success)
                {
                    IsGuest = false;
                    LogLoginState("Google");

                    // Try to restore cached username immediately (UID-based)
                    string cachedName = PlayerPrefs.GetString(GetKey(GOOGLE_USERNAME_KEY), "");
                    if (!string.IsNullOrEmpty(cachedName))
                    {
                        UserName = cachedName;
                        IsUsernameRequiredFlowPending = false;
                        EnqueueOnMainThread(() => OnUsernameFlowResolved?.Invoke());
                    }
                    else
                    {
                        IsUsernameRequiredFlowPending = true;
                    }

                    InitializeProfileOnBackend();

                    // OPTIMISTIC: If we still don't have a UserName, use Google Display Name as a temporary fallback
                    // to show the menu INSTANTLY without waiting for backend sync.
                    if (string.IsNullOrEmpty(UserName))
                    {
                        string fallbackName = GetFormattedUserName();
                        
                        // We ONLY skip the panel for Google users who have a real name.
                        // Guests ("Guest Explorer") MUST always go through the panel.
                        if (!IsGuest && !string.IsNullOrEmpty(fallbackName) && fallbackName != "Player" && fallbackName != "Guest Explorer")
                        {
                            UserName = fallbackName;
                            IsUsernameRequiredFlowPending = false;
                            EnqueueOnMainThread(() => OnUsernameFlowResolved?.Invoke());
                        }
                        else
                        {
                        }
                    }

                    EnqueueOnMainThread(() => OnLoginSuccess?.Invoke());
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AuthManager] Google Login Failed: {ex.Message}");
                EnqueueOnMainThread(() => OnLoginFailed?.Invoke(ex.Message));
            }
        }

        public void InitializeProfileOnBackend()
        {
            if (GameDataApiClient.Instance == null || !GameDataApiClient.Instance.IsNetworkAvailable) 
            {
                return;
            }

            string authType = IsGuest ? "guest" : "google";
            
            // Only block UI if we don't have ANY username yet (cached or otherwise)
            if (string.IsNullOrEmpty(UserName))
            {
                IsUsernameRequiredFlowPending = true;
            }
            else
            {
                IsUsernameRequiredFlowPending = false;
                EnqueueOnMainThread(() => OnUsernameFlowResolved?.Invoke());
            }
            string displayName = IsGuest ? "Guest" : _authService.UserDisplayName;
            string email = IsGuest ? "" : _authService.UserEmail;
            string photoUrl = IsGuest ? "" : _authService.UserPhotoUrl;

            GameDataApiClient.Instance.InitializeProfile(authType, displayName, email, photoUrl, (success) => {
                if (success)
                {
                    SyncProgressWithBackend();
                }
                else
                {
                    SyncProgressWithBackend();
                }
            });
        }

        public void SignOut()
        {
            
            // Note: This is a synchronous call. For full safety, use SignOutAndReturnToMenu or wait for abandonment manually.
            
            // Before signing out, if we are a Guest, we might want to clear their specific PlayerPrefs 
            // since Logout is the ONLY place to clear data.
            if (IsGuest)
            {
                PlayerPrefs.DeleteKey(GetKey(HIGHEST_XP_KEY));
                PlayerPrefs.DeleteKey(GetKey(LAST_LEVEL_KEY));
                PlayerPrefs.DeleteKey(GUEST_USERNAME_KEY);
                PlayerPrefs.Save();
            }

            _authService.SignOut();
            IsGuest = false;
            UserName = null;
            IsUsernameRequiredFlowPending = false;
            ResetLocalSessionOnly();
            
            // For a full reset on Logout, we clear the static XP and the session level
            ScoreManager.ResetAllXP();
            LevelManager.ResetSavedLevel(); // Explicitly reset to -1
            LastSavedXP = 0;
            PendingNewGameReset = false;
            
            EnqueueOnMainThread(() => OnLogout?.Invoke());
        }

        public System.Collections.IEnumerator SignOutAndReturnToMenuCoroutine()
        {
            
            if (GameplayStatsTracker.Instance != null)
            {
                yield return GameplayStatsTracker.Instance.TrySendAbandonedIfActiveCoroutine("logout");
            }

            SignOut();
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }

        public void EnqueueOnMainThread(Action action)
        {
            if (action == null) return;
            _mainThreadQueue.Enqueue(action);
        }

        public void SyncProgressWithBackend()
        {
            if (CatsEscape.Networking.GameDataApiClient.Instance != null && IsAuthenticated)
            {
                if (!CatsEscape.Networking.GameDataApiClient.Instance.IsNetworkAvailable)
                {
                    return;
                }

                CatsEscape.Networking.GameDataApiClient.Instance.FetchPlayerProgress((progress) => 
                {
                    if (progress != null)
                    {
                        _cachedProgress = progress;
                        UserName = progress.userName;

                        if (IsGuest && !string.IsNullOrEmpty(UserName))
                        {
                            PlayerPrefs.SetString(GUEST_USERNAME_KEY, UserName);
                            PlayerPrefs.Save();
                        }

                        if (string.IsNullOrEmpty(UserName))
                        {
                            string defaultName = IsGuest ? "Guest" : GetFormattedUserName();
                            IsUsernameRequiredFlowPending = true;
                            EnqueueOnMainThread(() => OnUsernameRequired?.Invoke(defaultName));
                        }
                        else
                        {
                            // Cache the confirmed username for future sessions (UID-based)
                            PlayerPrefs.SetString(GetKey(IsGuest ? GUEST_USERNAME_KEY : GOOGLE_USERNAME_KEY), UserName);
                            PlayerPrefs.Save();

                            IsUsernameRequiredFlowPending = false;
                            EnqueueOnMainThread(() => OnUsernameFlowResolved?.Invoke());
                        }
                        
                        // Only apply XP to ScoreManager if we are in the MainMenu (baseline)
                        // This prevents mid-game "jumps" if backend has stale or polluted data.
                        bool isMainMenu = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainMenu";

                        if (isMainMenu)
                        {
                            ScoreManager.SetTotalXP(progress.highestXP);
                        }
                        else
                        {
                        }
                        OnProgressSynced?.Invoke();
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(UserName))
                        {
                            string defaultName = IsGuest ? "GuestPlayer" : GetFormattedUserName();
                            IsUsernameRequiredFlowPending = true;
                            EnqueueOnMainThread(() => OnUsernameRequired?.Invoke(defaultName));
                        }
                        else
                        {
                            IsUsernameRequiredFlowPending = false;
                            EnqueueOnMainThread(() => OnUsernameFlowResolved?.Invoke());
                        }
                    }
                });
            }
        }

        public void CompleteGuestFlowAndStartGame()
        {
            LevelManager.ResetSavedLevel();
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.currentLevel = 1;
                ScoreManager.ResetAllXP();
                if (PlayerMovement.Instance != null) PlayerMovement.Instance.PrepareForLevelStart();
            }
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
        }

        public bool IsUserLoggedIn() => _authService != null && _authService.IsLoggedIn;
        


        public void SignOutAndReturnToMenu()
        {
            StartCoroutine(SignOutAndReturnToMenuCoroutine());
        }
        public string GetLoginType()
        {
            if (IsGuest) return "GUEST";
            if (_authService != null && _authService.IsLoggedIn) return "GOOGLE";
            return "NONE";
        }

        public string GetFormattedUserName()
        {
            if (!string.IsNullOrEmpty(UserName)) return UserName;
            if (IsGuest) return "Guest Explorer";
            
            // Priority 1: Google Display Name
            if (_authService != null && !string.IsNullOrEmpty(_authService.UserDisplayName))
            {
                return _authService.UserDisplayName;
            }

            // Priority 2: Email Prefix
            if (_authService != null && !string.IsNullOrEmpty(_authService.UserEmail))
            {
                string email = _authService.UserEmail;
                if (email.Contains("@")) return email.Split('@')[0];
                return email;
            }

            // Priority 3: Fallback
            return "Player";
        }

        public void FinalizeUsername(string confirmedName)
        {
            UserName = confirmedName;
            PlayerPrefs.SetString(GetKey(IsGuest ? GUEST_USERNAME_KEY : GOOGLE_USERNAME_KEY), confirmedName);
            PlayerPrefs.Save();
            
            IsUsernameRequiredFlowPending = false;
            EnqueueOnMainThread(() => OnUsernameFlowResolved?.Invoke());
        }
    }
}
