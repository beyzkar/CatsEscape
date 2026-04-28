using System;
using System.Threading.Tasks;
using UnityEngine;

#if USE_FIREBASE_AUTH
using Firebase;
using Firebase.Auth;
#endif

#if USE_FIREBASE_AUTH && (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
using Google;
#endif

namespace CatsEscape.Auth
{
    public class FirebaseAuthService : IAuthService
    {
        private string _webClientId;
        private bool _isInitialized = false;
        private string _errorMessage = "";

#if USE_FIREBASE_AUTH
        private FirebaseApp _app;
        private FirebaseAuth _auth;
        private FirebaseUser _user;
#endif

        public bool IsLoggedIn => 
#if USE_FIREBASE_AUTH
            _user != null;
#else
            false;
#endif

        public bool IsInitialized => _isInitialized;
        public string ErrorMessage => _errorMessage;

        public string UserDisplayName => 
#if USE_FIREBASE_AUTH
            _user?.DisplayName ?? "User";
#else
            "Guest";
#endif

        public string UserEmail => 
#if USE_FIREBASE_AUTH
            _user?.Email ?? "";
#else
            "";
#endif

        public string UserId => 
#if USE_FIREBASE_AUTH
            _user?.UserId ?? "";
#else
            "";
#endif

        public string UserPhotoUrl => 
#if USE_FIREBASE_AUTH
            _user?.PhotoUrl?.ToString() ?? "";
#else
            "";
#endif

        public async Task InitAsync(string webClientId)
        {
            _webClientId = webClientId;
            Debug.Log($"[AUTH_FLOW] InitAsync started. WebClientId: {webClientId}");

#if USE_FIREBASE_AUTH
            try
            {
                // Security Check: Verify if configuration file exists
                if (!System.IO.File.Exists(Application.streamingAssetsPath + "/google-services-desktop.json") && 
                    !System.IO.File.Exists(Application.dataPath + "/google-services.json"))
                {
                    Debug.LogError("[SECURITY] Firebase configuration file (google-services.json) is missing! Firebase initialization will likely fail.");
                }

                var checkTask = FirebaseApp.CheckAndFixDependenciesAsync();
                await checkTask;

                if (checkTask.Result == DependencyStatus.Available)
                {
                    _app = FirebaseApp.DefaultInstance;
                    _auth = FirebaseAuth.GetAuth(_app);
                    _auth.StateChanged += AuthStateChanged;
                    _isInitialized = true;
                    Debug.Log("[AUTH_FLOW] Firebase Initialized Success.");

#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
                    // Configure Google Sign-In ONCE here during initialization
                    if (!string.IsNullOrEmpty(_webClientId) && _webClientId != "YOUR_WEB_CLIENT_ID_HERE")
                    {
                        GoogleSignIn.Configuration = new GoogleSignInConfiguration
                        {
                            WebClientId = _webClientId,
                            RequestIdToken = true,
                            UseGameSignIn = false
                        };
                        Debug.Log("[AUTH_FLOW] GoogleSignIn Configuration applied successfully during Init.");
                    }
                    else
                    {
                        Debug.LogWarning("[AUTH_FLOW] GoogleSignIn Configuration SKIP: WebClientId is empty or default.");
                    }
#endif
                }
                else
                {
                    _errorMessage = $"Firebase dependencies error: {checkTask.Result}";
                    Debug.LogError($"[AUTH_FLOW] Could not resolve Firebase dependencies: {checkTask.Result}");
                    _isInitialized = false; // Fix: Stay false if dependencies fail
                }
            }
            catch (Exception ex)
            {
                _errorMessage = $"Init Exception: {ex.Message}";
                Debug.LogError($"[AUTH_FLOW] InitAsync Exception: {ex.Message}");
                _isInitialized = false; // Fix: Stay false if exception occurs
            }
#else
            Debug.Log("[AUTH_FLOW] InitAsync: Firebase Auth is disabled (USE_FIREBASE_AUTH not defined).");
            _errorMessage = "Scripting Define Symbol (USE_FIREBASE_AUTH) is missing.";
            _isInitialized = true;
            await Task.Yield();
#endif
        }

#if USE_FIREBASE_AUTH
        private void AuthStateChanged(object sender, EventArgs eventArgs)
        {
            if (_auth != null && _auth.CurrentUser != _user)
            {
                bool signedIn = _user != _auth.CurrentUser && _auth.CurrentUser != null;
                _user = _auth.CurrentUser;
                if (signedIn)
                {
                    Debug.Log($"[AUTH_FLOW] User Logged In: {_user.DisplayName}");
                }
            }
        }
#endif

        public async Task<bool> SignInWithGoogleAsync()
        {
            Debug.Log("[AUTH_FLOW] SignInWithGoogleAsync entry.");
            if (!_isInitialized || _auth == null)
            {
                Debug.LogError("[AUTH_FLOW] Service not initialized or _auth is null.");
                return false;
            }

#if USE_FIREBASE_AUTH
            try
            {
                if (string.IsNullOrEmpty(_webClientId) || _webClientId == "YOUR_WEB_CLIENT_ID_HERE")
                {
                    Debug.LogError("[AUTH_FLOW] _webClientId is empty or invalid.");
                    return false;
                }

#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
                Debug.Log("[AUTH_ANDROID] Google sign-in started");
                var googleUser = await GoogleSignIn.DefaultInstance.SignIn();

                if (googleUser == null)
                {
                    Debug.LogError("[AUTH_ANDROID] Google sign-in failed: GoogleUser is null");
                    return false;
                }

                Debug.Log("[AUTH_ANDROID] Firebase credential sign-in started");
                Credential credential = GoogleAuthProvider.GetCredential(googleUser.IdToken, null);
                _user = await _auth.SignInWithCredentialAsync(credential);
                
                if (_user != null)
                {
                    Debug.Log($"[AUTH_ANDROID] Firebase credential sign-in success UID: {_user.UserId}");
                    return true;
                }
                else
                {
                    Debug.LogError("[AUTH_ANDROID] Firebase credential sign-in failed: FirebaseUser is null");
                    return false;
                }
#else
                Debug.LogWarning("[AUTH_FLOW] STEP 4.2 INFO: Google Sign-In is only available on Android/iOS devices. Editor bypass triggered.");
                await Task.Yield();
                return false;
#endif
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AUTH_FLOW] STEP 4.3 EXCEPTION: Critical failure in SignInWithGoogleAsync: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
#else
            Debug.LogError("[AUTH_FLOW] STEP 4 FAIL: USE_FIREBASE_AUTH symbol is NOT defined.");
            await Task.Yield();
            return false;
#endif
        }
        
        public async Task<bool> SignInAnonymouslyAsync()
        {
            Debug.Log("[AUTH_FLOW] SignInAnonymouslyAsync entry.");
            if (!_isInitialized || _auth == null)
            {
                Debug.LogError("[AUTH_FLOW] Service not initialized or _auth is null.");
                return false;
            }

#if USE_FIREBASE_AUTH
            try
            {
                Debug.Log("[AUTH_ANDROID] Calling Firebase anonymous sign-in");
                var authResult = await _auth.SignInAnonymouslyAsync();
                _user = authResult?.User;
                
                if (_user != null)
                {
                    Debug.Log($"[AUTH_ANDROID] Firebase anonymous sign-in success UID: {_user.UserId}");
                    return true;
                }
                else
                {
                    Debug.LogError("[AUTH_ANDROID] Firebase anonymous sign-in failed: User is null");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AUTH_ANDROID] Firebase anonymous sign-in failed: {ex.Message}");
                return false;
            }
#else
            await Task.Yield();
            return false;
#endif
        }

        public async Task<bool> TrySilentLoginAsync()
        {
            Debug.Log("[AUTH_FLOW] Silent login check starting...");
#if USE_FIREBASE_AUTH
            if (!_isInitialized || _auth == null) return false;
            try
            {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
                Debug.Log("[AUTH_FLOW] Calling Googe SignInSilently...");
                var googleUser = await GoogleSignIn.DefaultInstance.SignInSilently();
                if (googleUser != null)
                {
                    Debug.Log("[AUTH_FLOW] Silent SUCCESS. Binding to Firebase...");
                    Credential credential = GoogleAuthProvider.GetCredential(googleUser.IdToken, null);
                    _user = await _auth.SignInWithCredentialAsync(credential);
                    Debug.Log("[AUTH_FLOW] Silent Bind SUCCESS.");
                    return _user != null;
                }
                Debug.Log("[AUTH_FLOW] No silent user detected.");
#else
                // Satisfy async requirement in Editor
                await Task.Yield();
#endif
            }
            catch (Exception ex)
            {
                Debug.Log($"[AUTH_FLOW] Silent check info: {ex.Message}");
            }
#endif
            return false;
        }

        public void SignOut()
        {
            Debug.Log("[Auth] Sign-out requested.");
#if USE_FIREBASE_AUTH
            try 
            {
                _auth?.SignOut();
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
                GoogleSignIn.DefaultInstance?.SignOut();
#endif
                _user = null;
                Debug.Log("[Auth] Sign-out complete.");
            }
            catch (Exception ex)
            {
                 Debug.LogError($"[Auth] Sign-out Error: {ex.Message}");
            }
#endif
        }
        public async Task<string> GetIdTokenAsync(bool forceRefresh = false)
        {
#if USE_FIREBASE_AUTH
            if (_user != null)
            {
                try 
                {
                    string token = await _user.TokenAsync(forceRefresh);
                    if (!string.IsNullOrEmpty(token))
                    {
                        Debug.Log("[AUTH_ANDROID] Firebase ID token retrieved successfully");
                    }
                    return token;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Auth] Error getting ID Token: {ex.Message}");
                    return null;
                }
            }
#endif
            await Task.Yield();
            return null;
        }
    }
}
