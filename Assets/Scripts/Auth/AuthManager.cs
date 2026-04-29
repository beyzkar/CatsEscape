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
                if (IsGuest) return 1; 
                if (_cachedProgress != null) return _cachedProgress.lastLevelReached;
                return PlayerPrefs.GetInt(GetKey(LAST_LEVEL_KEY), 1);
            }
            set 
            { 
                if (IsGuest) return; 
                if (_cachedProgress != null) _cachedProgress.lastLevelReached = value;
                PlayerPrefs.SetInt(GetKey(LAST_LEVEL_KEY), value); 
                PlayerPrefs.Save(); 
            }
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
                // Guests always start fresh
                ScoreManager.ResetAllXP();
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
            if (GameplayStatsTracker.Instance != null)
            {
                await WaitForCoroutine(GameplayStatsTracker.Instance.TrySendAbandonedIfActiveCoroutine("switch_to_guest"));
            }

            try 
            {
                bool success = await _authService.SignInAnonymouslyAsync();
                if (success)
                {
                    IsGuest = true;
                    LogLoginState("Guest");
                    
                    if (LevelManager.Instance != null) LevelManager.Instance.currentLevel = 1;
                    LevelManager.ResetSavedLevel();
                    ScoreManager.ResetAllXP();

                    InitializeProfileOnBackend();
                    
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
            if (GameDataApiClient.Instance == null) return;

            string authType = IsGuest ? "guest" : "google";
            string displayName = IsGuest ? "Guest" : _authService.UserDisplayName;
            string email = IsGuest ? "" : _authService.UserEmail;
            string photoUrl = IsGuest ? "" : _authService.UserPhotoUrl;

            Debug.Log($"[AuthManager] Initializing profile on backend for {authType} (Name: {displayName}, Email: {email})...");
            GameDataApiClient.Instance.InitializeProfile(authType, displayName, email, photoUrl, (success) => {
                if (success)
                {
                    Debug.Log($"[AuthManager] Profile initialized successfully for {authType}.");
                    // For Google users, now fetch their actual progress
                    if (!IsGuest)
                    {
                        SyncProgressWithBackend();
                    }
                }
                else
                {
                    Debug.LogWarning($"[AuthManager] Profile initialization FAILED for {authType}. Continuing anyway...");
                    // Still try to sync progress if Google, it might just be the init endpoint that failed
                    if (!IsGuest)
                    {
                        SyncProgressWithBackend();
                    }
                }
            });
        }

        public void SignOut()
        {
            // Note: This is a synchronous call. For full safety, use SignOutAndReturnToMenu or wait for abandonment manually.
            _authService.SignOut();
            IsGuest = false;
            ResetLocalSessionOnly();
            ScoreManager.ResetAllXP();
            LevelManager.ResetSavedLevel(); // Explicitly reset to -1
            
            Debug.Log("[AuthManager] Global SignOut complete. All local states cleared.");
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
            if (CatsEscape.Networking.GameDataApiClient.Instance != null && IsUserLoggedIn())
            {
                Debug.Log("[AuthManager] Syncing progress with backend...");
                CatsEscape.Networking.GameDataApiClient.Instance.FetchPlayerProgress((progress) => {
                    if (progress != null)
                    {
                        _cachedProgress = progress;
                        
                        // Only apply XP to ScoreManager if we are in the MainMenu (baseline)
                        // OR if we are resuming a level > 1. 
                        // If we are currently playing Level 1, we MUST stay at 0 XP per new rule.
                        bool isMainMenu = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainMenu";
                        bool isResumingHigherLevel = LevelManager.Instance != null && LevelManager.Instance.currentLevel > 1;

                        if (isMainMenu || isResumingHigherLevel)
                        {
                            ScoreManager.SetTotalXP(progress.highestXP);
                        }
                        
                        Debug.Log($"[AuthManager] Progress synced. Remote Level: {progress.lastLevelReached}, XP: {progress.highestXP} (Applied: {isMainMenu || isResumingHigherLevel})");
                        OnProgressSynced?.Invoke();
                    }
                    else
                    {
                        Debug.LogWarning("[AuthManager] Sync failed or no profile found. Keeping local baseline.");
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
            if (IsGuest) return "Guest Explorer";
            if (_authService != null && !string.IsNullOrEmpty(_authService.UserEmail))
            {
                string email = _authService.UserEmail;
                if (email.Contains("@")) return email.Split('@')[0];
                return email;
            }
            return "Unknown User";
        }
    }
}
