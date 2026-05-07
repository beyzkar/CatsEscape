using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using CatsEscape.Auth;

namespace CatsEscape.Networking
{
    public class GameDataApiClient : MonoBehaviour
    {
        public static GameDataApiClient Instance { get; private set; }

        [Header("Backend Settings")]
        [Tooltip("Base URL for the API in Editor (usually localhost)")]
        public string editorBaseUrl = "http://localhost:5001/api/game";
        
        [Tooltip("Base URL for the API on Android (PHYSICAL DEVICE: Using your Computer's Local IP)")]
        public string androidBaseUrl = "http://192.168.1.181:5001/api/game"; 

        private string BaseUrl => Application.platform == RuntimePlatform.Android ? androidBaseUrl : editorBaseUrl;
        
        public bool IsNetworkAvailable => Application.internetReachability != NetworkReachability.NotReachable;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        // --- NEW: Activity Tracking ---
        private string _currentSessionId;
        public string SessionId 
        {
            get 
            {
                if (string.IsNullOrEmpty(_currentSessionId))
                {
                    _currentSessionId = System.Guid.NewGuid().ToString();
                }
                return _currentSessionId;
            }
        }

        public void StartNewSession()
        {
            _currentSessionId = System.Guid.NewGuid().ToString();
            SendActivity("session_start");
        }

        public Coroutine SendActivity(string eventType, int? levelNumber = null, string result = null)
        {
            // NEW: Ensure profile is initialized at least once during game start or session start
            if ((eventType == "game_start" || eventType == "session_start") && AuthManager.Instance != null && AuthManager.Instance.IsAuthenticated)
            {
                AuthManager.Instance.InitializeProfileOnBackend();
            }

            return StartCoroutine(PostActivityCoroutine(eventType, levelNumber, result));
        }

        private IEnumerator PostActivityCoroutine(string eventType, int? levelNumber, string result)
        {
            if (!IsNetworkAvailable)
            {
                yield break;
            }

            if (AuthManager.Instance == null || !AuthManager.Instance.IsAuthenticated)
            {
                yield break; 
            }

            var tokenTask = AuthManager.Instance.Service.GetIdTokenAsync();
            float timer = 0f;
            while (!tokenTask.IsCompleted && timer < 5f) 
            {
                timer += Time.deltaTime;
                yield return null;
            }
            
            if (!tokenTask.IsCompleted)
            {
                yield break;
            }

            string idToken = tokenTask.Result;
            if (string.IsNullOrEmpty(idToken)) yield break;

            string userName = (AuthManager.Instance != null) ? AuthManager.Instance.UserName : "";
            string json = $"{{\"eventType\":\"{eventType}\",\"sessionId\":\"{SessionId}\",\"userName\":\"{userName}\"";
            if (levelNumber.HasValue) json += $",\"levelNumber\":{levelNumber.Value}";
            if (!string.IsNullOrEmpty(result)) json += $",\"result\":\"{result}\"";
            json += "}";

            string url = $"{BaseUrl}/activity";
            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", "Bearer " + idToken);
                request.timeout = 10;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                }
                else
                {
                    Debug.LogError($"[GameDataApiClient] Activity Sync Fail. Code: {request.responseCode}, Error: {request.error}");
                }
            }
        }

        public Coroutine SendLevelResult(string result)
        {
            if (GameplayStatsTracker.Instance == null)
            {
                Debug.LogError("[GameDataApiClient] GameplayStatsTracker.Instance is NULL!");
                return null;
            }

            return SendLevelResult(
                GameplayStatsTracker.Instance.currentLevelNumber,
                result,
                GameplayStatsTracker.Instance.GetFinalXPEarned(),
                ScoreManager.Instance.GetTotalXP(),
                GameplayStatsTracker.Instance.fishSpawnCount,
                GameplayStatsTracker.Instance.potionSpawnCount,
                GameplayStatsTracker.Instance.heartsGained,
                GameplayStatsTracker.Instance.heartsLost
            );
        }

        public Coroutine SendLevelResult(int levelNumber, string levelResult, int xpEarned, int totalXP, int fishSpawnCount, int potionSpawnCount, int heartsGained, int heartsLost)
        {
            LevelResultDto dto = new LevelResultDto
            {
                levelNumber = levelNumber,
                levelResult = levelResult,
                xpEarned = xpEarned,
                totalXP = totalXP,
                fishSpawnCount = fishSpawnCount,
                potionSpawnCount = potionSpawnCount,
                heartsGained = heartsGained,
                heartsLost = heartsLost,
                deviceInfo = DeviceInfoProvider.GetCurrentDeviceInfo(),
                startedAt = (GameplayStatsTracker.Instance != null) ? GameplayStatsTracker.Instance.levelStartedAtISO : "",
                completedAt = (levelResult == "completed") ? System.DateTime.UtcNow.ToString("O") : "",
                abandonedAt = (levelResult == "abandoned") ? System.DateTime.UtcNow.ToString("O") : "",
                durationSeconds = (GameplayStatsTracker.Instance != null) ? GameplayStatsTracker.Instance.GetLevelDuration() : 0f,
                userName = (AuthManager.Instance != null) ? AuthManager.Instance.UserName : ""
            };

            return StartCoroutine(PostLevelResultCoroutine(dto));
        }

        private IEnumerator PostLevelResultCoroutine(LevelResultDto dto)
        {
            string url = $"{BaseUrl}/level-result";

            if (AuthManager.Instance == null)
            {
                Debug.LogError("[GameDataApiClient] AuthManager.Instance is NULL!");
                yield break;
            }

            if (!IsNetworkAvailable)
            {
                yield break;
            }

            if (!AuthManager.Instance.IsAuthenticated)
            {
                yield break;
            }

            var tokenTask = AuthManager.Instance.Service.GetIdTokenAsync();
            float timer = 0f;
            while (!tokenTask.IsCompleted && timer < 5f)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            if (!tokenTask.IsCompleted)
            {
                Debug.LogError("[API] Token retrieval timed out for level result.");
                yield break;
            }

            string idToken = tokenTask.Result;
            if (string.IsNullOrEmpty(idToken))
            {
                Debug.LogError("[GameDataApiClient] Firebase token is EMPTY!");
                yield break;
            }
            
            string json = JsonUtility.ToJson(dto);

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", "Bearer " + idToken);
                request.timeout = 10;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                }
                else
                {
                    Debug.LogError($"[GameDataApiClient] Sync Fail. Code: {request.responseCode}, Error: {request.error}");
                    Debug.LogError($"[GameDataApiClient] Response Body: {request.downloadHandler.text}");
                }
            }
        }

        public void GetNextGuestUsername(System.Action<string> callback)
        {
            StartCoroutine(GetNextGuestUsernameCoroutine(callback));
        }

        private IEnumerator GetNextGuestUsernameCoroutine(System.Action<string> callback)
        {
            string url = $"{BaseUrl}/user/next-guest-username";
            
            if (AuthManager.Instance == null || !AuthManager.Instance.IsAuthenticated)
            {
                callback?.Invoke("GuestPlayer");
                yield break;
            }

            var tokenTask = AuthManager.Instance.Service.GetIdTokenAsync();
            yield return new WaitUntil(() => tokenTask.IsCompleted);

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("Authorization", "Bearer " + tokenTask.Result);
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var response = JsonUtility.FromJson<NextGuestResponse>(request.downloadHandler.text);
                    callback?.Invoke(response.nextGuestName);
                }
                else
                {
                    callback?.Invoke("GuestPlayer");
                }
            }
        }

        [System.Serializable] class NextGuestResponse { public string nextGuestName; }

        public void SetUsername(string userName, System.Action<bool, string> onComplete)
        {
            StartCoroutine(SetUsernameCoroutine(userName, onComplete));
        }

        private IEnumerator SetUsernameCoroutine(string userName, System.Action<bool, string> onComplete)
        {
            string url = $"{BaseUrl}/user/set-username";
            
            if (AuthManager.Instance == null || !AuthManager.Instance.IsAuthenticated)
            {
                onComplete?.Invoke(false, "AUTH_REQUIRED");
                yield break;
            }

            var tokenTask = AuthManager.Instance.Service.GetIdTokenAsync();
            yield return new WaitUntil(() => tokenTask.IsCompleted);

            string safeUserName = (userName ?? string.Empty).Trim();
            string uid = AuthManager.Instance.UserId;
            string json = JsonUtility.ToJson(new SetUsernameRequest { userName = safeUserName, uid = uid });

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", "Bearer " + tokenTask.Result);
                request.timeout = 10;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var response = JsonUtility.FromJson<SetUsernameResponse>(request.downloadHandler.text);
                    onComplete?.Invoke(true, response.userName);
                }
                else
                {
                    string errorMsg = "SERVER_ERROR";
                    bool isNetworkError = request.responseCode == 0 || 
                                         request.result == UnityWebRequest.Result.ConnectionError ||
                                         request.error.ToLower().Contains("timeout") ||
                                         request.error.ToLower().Contains("connect");

                    if (isNetworkError)
                    {
                        errorMsg = "NETWORK_ERROR";
                        Debug.LogError($"[USERNAME_API] Network Error Detected! Error: {request.error}, Code: {request.responseCode}, URL: {url}");
                    }
                    try {
                        if (!string.IsNullOrEmpty(request.downloadHandler.text))
                        {
                            var errorRes = JsonUtility.FromJson<SetUsernameResponse>(request.downloadHandler.text);
                            if (errorRes != null && !string.IsNullOrEmpty(errorRes.message)) errorMsg = errorRes.message;
                        }
                    } catch {}
                    onComplete?.Invoke(false, errorMsg);
                }
            }
        }

        [System.Serializable] class SetUsernameRequest { public string userName; public string uid; }
        [System.Serializable] class SetUsernameResponse { public bool success; public string userName; public string message; }

        public void InitializeProfile(string authType, string displayName, string email, string photoUrl, System.Action<bool> onComplete = null)
        {
            StartCoroutine(InitializeProfileCoroutine(authType, displayName, email, photoUrl, onComplete));
        }

        private IEnumerator InitializeProfileCoroutine(string authType, string displayName, string email, string photoUrl, System.Action<bool> onComplete)
        {
            string url = $"{BaseUrl}/profile/init";

            if (!IsNetworkAvailable)
            {
                onComplete?.Invoke(false);
                yield break;
            }

            if (AuthManager.Instance == null || !AuthManager.Instance.IsAuthenticated)
            {
                onComplete?.Invoke(false);
                yield break;
            }

            var tokenTask = AuthManager.Instance.Service.GetIdTokenAsync();
            float timer = 0f;
            while (!tokenTask.IsCompleted && timer < 5f)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            if (!tokenTask.IsCompleted)
            {
                onComplete?.Invoke(false);
                yield break;
            }

            string idToken = tokenTask.Result;
            if (string.IsNullOrEmpty(idToken))
            {
                onComplete?.Invoke(false);
                yield break;
            }

            // Construct JSON
            string json = $"{{\"authType\":\"{authType}\"";
            if (!string.IsNullOrEmpty(displayName)) json += $",\"displayName\":\"{displayName}\"";
            if (!string.IsNullOrEmpty(email)) json += $",\"email\":\"{email}\"";
            if (!string.IsNullOrEmpty(photoUrl)) json += $",\"photoUrl\":\"{photoUrl}\"";
            json += "}";

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", "Bearer " + idToken);
                request.timeout = 10;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    onComplete?.Invoke(true);
                }
                else
                {
                    Debug.LogError($"[GameDataApiClient] Profile Init Fail. Code: {request.responseCode}, Error: {request.error}, Body: {request.downloadHandler.text}");
                    onComplete?.Invoke(false);
                }
            }
        }

        public void FetchPlayerProgress(System.Action<PlayerProgressDto> callback)
    {
        StartCoroutine(GetProgressRoutine(callback));
    }

    private IEnumerator GetProgressRoutine(System.Action<PlayerProgressDto> callback)
    {
        if (AuthManager.Instance == null || !AuthManager.Instance.IsAuthenticated)
        {
            callback?.Invoke(null);
            yield break;
        }

        // Get ID Token
        var tokenTask = AuthManager.Instance.Service.GetIdTokenAsync();
        yield return new WaitUntil(() => tokenTask.IsCompleted);
        string idToken = tokenTask.Result;

        string uid = AuthManager.Instance.UserId;
        string url = $"{BaseUrl}/progress?uid={uid}";
        
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("Authorization", "Bearer " + idToken);
            request.timeout = 10;
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[GameDataApiClient] Fetch Progress Error: {request.error} (Code: {request.responseCode})");
                callback?.Invoke(null);
            }
            else
            {
                try
                {
                    PlayerProgressDto progress = JsonUtility.FromJson<PlayerProgressDto>(request.downloadHandler.text);
                    callback?.Invoke(progress);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[GameDataApiClient] JSON Parsing Error: {ex.Message}");
                    callback?.Invoke(null);
                }
            }
        }
    }
    }

    [System.Serializable]
    public class PlayerProgressDto
    {
        public string uid;
        public int highestXP;
        public int lastLevelReached;
        public int highestLevelReached;
        public int totalCompletions;
        public int totalFailures;
        public int totalAbandoned;
        public string userName;
    }
}
