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
        public string androidBaseUrl = "http://192.168.1.180:5001/api/game"; 

        private string BaseUrl => Application.platform == RuntimePlatform.Android ? androidBaseUrl : editorBaseUrl;
        
        public bool IsNetworkAvailable => Application.internetReachability != NetworkReachability.NotReachable;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Debug.Log("[GameDataApiClient] Initialized and marked as DontDestroyOnLoad.");
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
            Debug.Log($"[GameDataApiClient] Started new session: {_currentSessionId}");
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
                Debug.Log("[Network] Offline detected. Skipping activity log.");
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
                Debug.LogWarning("[API] Token retrieval timed out.");
                yield break;
            }

            string idToken = tokenTask.Result;
            if (string.IsNullOrEmpty(idToken)) yield break;

            string json = $"{{\"eventType\":\"{eventType}\",\"sessionId\":\"{SessionId}\"";
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
                    Debug.Log($"[GameDataApiClient] Activity '{eventType}' logged successfully.");
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
                GameplayStatsTracker.Instance.fishSpawnCount,
                GameplayStatsTracker.Instance.potionSpawnCount,
                GameplayStatsTracker.Instance.heartsGained,
                GameplayStatsTracker.Instance.heartsLost
            );
        }

        public Coroutine SendLevelResult(int levelNumber, string levelResult, int xpEarned, int fishSpawnCount, int potionSpawnCount, int heartsGained, int heartsLost)
        {
            LevelResultDto dto = new LevelResultDto
            {
                levelNumber = levelNumber,
                levelResult = levelResult,
                xpEarned = xpEarned,
                fishSpawnCount = fishSpawnCount,
                potionSpawnCount = potionSpawnCount,
                heartsGained = heartsGained,
                heartsLost = heartsLost,
                deviceInfo = DeviceInfoProvider.GetCurrentDeviceInfo(),
                startedAt = (GameplayStatsTracker.Instance != null) ? GameplayStatsTracker.Instance.levelStartedAtISO : "",
                completedAt = (levelResult == "completed") ? System.DateTime.UtcNow.ToString("O") : "",
                abandonedAt = (levelResult == "abandoned") ? System.DateTime.UtcNow.ToString("O") : "",
                durationSeconds = (GameplayStatsTracker.Instance != null) ? GameplayStatsTracker.Instance.GetLevelDuration() : 0f
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
                Debug.Log("[Network] Offline detected. Skipping level result sync.");
                yield break;
            }

            if (!AuthManager.Instance.IsAuthenticated)
            {
                Debug.LogWarning("[GameDataApiClient] User NOT authenticated. Skipping telemetry sync.");
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
                    Debug.Log($"[GameDataApiClient] Level result synced successfully for Level {dto.levelNumber}.");
                }
                else
                {
                    Debug.LogError($"[GameDataApiClient] Sync Fail. Code: {request.responseCode}, Error: {request.error}");
                    Debug.LogError($"[GameDataApiClient] Response Body: {request.downloadHandler.text}");
                }
            }
        }

        public void InitializeProfile(string authType, string displayName, string email, string photoUrl, System.Action<bool> onComplete = null)
        {
            StartCoroutine(InitializeProfileCoroutine(authType, displayName, email, photoUrl, onComplete));
        }

        private IEnumerator InitializeProfileCoroutine(string authType, string displayName, string email, string photoUrl, System.Action<bool> onComplete)
        {
            string url = $"{BaseUrl}/profile/init";

            if (!IsNetworkAvailable)
            {
                Debug.Log("[Network] Offline detected. Skipping profile initialization.");
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
                Debug.LogWarning("[API] Token retrieval timed out for profile init.");
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
                    Debug.Log($"[GameDataApiClient] Profile initialized successfully for user {AuthManager.Instance.UserId}. Response: {request.downloadHandler.text}");
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
        string uid = (AuthManager.Instance != null) ? AuthManager.Instance.UserId : "NONE";
        string url = $"{BaseUrl}/progress?uid={uid}";
        
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.timeout = 10;
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[GameDataApiClient] Fetch Progress Error: {request.error}");
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
    }
}
