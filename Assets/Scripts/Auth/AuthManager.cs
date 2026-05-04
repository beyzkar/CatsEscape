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
                        Debug.Log($"[PROGRESS] Highest XP Updated: {value} (UID: {uid})");
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
                Debug.Log($"[PROGRESS] Last Level Reached Updated: {value}");
            }
        }

        public int LastSavedXP
        {
            get => PlayerPrefs.GetInt(GetKey(LAST_SAVED_XP_KEY), 0);
            set 
            { 
                PlayerPrefs.SetInt(GetKey(LAST_SAVED_XP_KEY), value); 
                PlayerPrefs.Save(); 
                Debug.Log($"[PROGRESS] Last Saved XP Updated: {value}");
            }
        }

        public bool PendingNewGameReset
        {
            get => PlayerPrefs.GetInt(GetKey(PENDING_RESET_KEY), 0) == 1;
            set 
            { 
                PlayerPrefs.SetInt(GetKey(PENDING_RESET_KEY), value ? 1 : 0); 
                PlayerPrefs.Save(); 
                Debug.Log($"[PROGRESS] Pending New Game Reset: {value}");
            }
        }

        public void ResetRunProgressOnly()
        {
            Debug.Log("[NEW_GAME] Resetting run progress only");
            LastLevelReached = 1;
            LastSavedXP = 0;
            PendingNewGameReset = false;
            Debug.Log("[PROGRESS] Saved progress after reset: Level 1, XP 0");
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
                    InitializeProfileOnBackend();
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

            Debug.Log($"[AuthManager] Login Success | Type: {type} | UID: {UserId} | Level: {LastLevelReached} | XP Baseline: {HighestXP}");
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
                    Debug.Log("[GuestLogin] Starting offline guest session");
                    success = true; // Offline guest is always allowed
                }

                if (success)
                {
                    IsGuest = true;
                    LogLoginState(isOnline ? "Guest" : "Guest Offline");
                    IsUsernameRequiredFlowPending = true;
                    
                    // REMOVED: Forced resets. We want to preserve progress until logout.
                    // Progress will be loaded via LogLoginState -> HighestXP

                    if (isOnline)
                    {
                        InitializeProfileOnBackend();
                    }
                    else
                    {
                        Debug.Log("[GuestLogin] Backend unavailable, continuing offline");
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
                Debug.Log("[GoogleLogin] Internet required");
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
                    IsUsernameRequiredFlowPending = true;
                    InitializeProfileOnBackend();
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
                Debug.Log("[AuthManager] Skipping profile init (Offline or no API Client)");
                return;
            }

            string authType = IsGuest ? "guest" : "google";
            IsUsernameRequiredFlowPending = true;
            string displayName = IsGuest ? "Guest" : _authService.UserDisplayName;
            string email = IsGuest ? "" : _authService.UserEmail;
            string photoUrl = IsGuest ? "" : _authService.UserPhotoUrl;

            Debug.Log($"[AuthManager] Initializing profile on backend for {authType} (Name: {displayName}, Email: {email})...");
            GameDataApiClient.Instance.InitializeProfile(authType, displayName, email, photoUrl, (success) => {
                if (success)
                {
                    Debug.Log($"[AuthManager] Profile initialized successfully for {authType}.");
                    // Fetch actual progress (including userName) for both Google and Guest
                    SyncProgressWithBackend();
                }
                else
                {
                    Debug.LogWarning($"[AuthManager] Profile initialization FAILED for {authType}. Continuing anyway...");
                    SyncProgressWithBackend();
                }
            });
        }

        public void SignOut()
        {
            Debug.Log("[LOGOUT] User explicitly logged out, clearing session/progress cache if needed");
            
            // Note: This is a synchronous call. For full safety, use SignOutAndReturnToMenu or wait for abandonment manually.
            
            // Before signing out, if we are a Guest, we might want to clear their specific PlayerPrefs 
            // since Logout is the ONLY place to clear data.
            if (IsGuest)
            {
                Debug.Log("[LOGOUT] Clearing Guest-specific PlayerPrefs");
                PlayerPrefs.DeleteKey(GetKey(HIGHEST_XP_KEY));
                PlayerPrefs.DeleteKey(GetKey(LAST_LEVEL_KEY));
                PlayerPrefs.Save();
            }

            _authService.SignOut();
            IsGuest = false;
            ResetLocalSessionOnly();
            
            // For a full reset on Logout, we clear the static XP and the session level
            ScoreManager.ResetAllXP();
            LevelManager.ResetSavedLevel(); // Explicitly reset to -1
            LastSavedXP = 0;
            PendingNewGameReset = false;
            
            Debug.Log("[LOGOUT] Global SignOut complete. All local states cleared.");
            EnqueueOnMainThread(() => OnLogout?.Invoke());
        }

        public System.Collections.IEnumerator SignOutAndReturnToMenuCoroutine()
        {
            Debug.Log("[AuthManager] SignOutAndReturnToMenu starting...");
            
            if (GameplayStatsTracker.Instance != null)
            {
                Debug.Log("[AuthManager] Waiting for abandoned telemetry...");
                yield return GameplayStatsTracker.Instance.TrySendAbandonedIfActiveCoroutine("logout");
            }

            SignOut();
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
            Debug.Log("[AuthManager] SignOutAndReturnToMenu complete.");
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
                    Debug.Log("[AuthManager] Skipping progress sync (Offline).");
                    return;
                }

                Debug.Log("[AuthManager] Syncing progress with backend...");
                CatsEscape.Networking.GameDataApiClient.Instance.FetchPlayerProgress((progress) => 
                {
                    if (progress != null)
                    {
                        _cachedProgress = progress;
                        UserName = progress.userName;

                        if (string.IsNullOrEmpty(UserName))
                        {
                            Debug.Log("[AUTH] Backend needsUserName=true");
                            string defaultName = IsGuest ? "Guest" : GetFormattedUserName();
                            IsUsernameRequiredFlowPending = true;
                            Debug.Log($"[AUTH] Triggering OnUsernameRequired with defaultName={defaultName}");
                            EnqueueOnMainThread(() => OnUsernameRequired?.Invoke(defaultName));
                        }
                        else
                        {
                            Debug.Log("[AUTH] Backend needsUserName=false");
                            IsUsernameRequiredFlowPending = false;
                            EnqueueOnMainThread(() => OnUsernameFlowResolved?.Invoke());
                        }
                        
                        // Only apply XP to ScoreManager if we are in the MainMenu (baseline)
                        // OR if we are resuming a level > 1. 
                        // If we are currently playing Level 1, we MUST stay at 0 XP per new rule.
                        bool isMainMenu = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainMenu";
                        bool isResumingHigherLevel = LevelManager.Instance != null && LevelManager.Instance.currentLevel > 1;

                        if (!IsNewGameStart && (isMainMenu || isResumingHigherLevel))
                        {
                            ScoreManager.SetTotalXP(progress.highestXP);
                        }
                        
                        Debug.Log($"[AuthManager] Progress synced. Remote Level: {progress.lastLevelReached}, XP: {progress.highestXP} (Applied: {isMainMenu || isResumingHigherLevel})");
                        OnProgressSynced?.Invoke();
                    }
                    else
                    {
                        Debug.LogWarning("[AuthManager] Sync failed or no profile found. Keeping local baseline.");
                        IsUsernameRequiredFlowPending = false;
                        EnqueueOnMainThread(() => OnUsernameFlowResolved?.Invoke());
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
            IsUsernameRequiredFlowPending = false;
            Debug.Log($"[AuthManager] UserName finalized: {confirmedName}");
            EnqueueOnMainThread(() => OnUsernameFlowResolved?.Invoke());
        }
    }
}
