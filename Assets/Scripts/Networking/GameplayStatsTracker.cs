using UnityEngine;
using System;
using CatsEscape.Auth;

namespace CatsEscape.Networking
{
    public class GameplayStatsTracker : MonoBehaviour
    {
        public static GameplayStatsTracker Instance { get; private set; }

        [Header("Current Level Stats")]
        public int currentLevelNumber;
        public int fishSpawnCount;
        public int potionSpawnCount;
        public int heartsGained;
        public int heartsLost;
        public int xpEarnedInLevel;
        private int xpAtStart;

        [Header("State Tracking")]
        public bool hasActiveLevelRun = false;
        public bool hasSentGameEnd = false;
        public bool hasSentLevelResult = false;
        
        public string levelStartedAtISO;
        private float levelStartedAtRealTime;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                UnityEngine.SceneManagement.SceneManager.sceneUnloaded += OnSceneUnloaded;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                UnityEngine.SceneManagement.SceneManager.sceneUnloaded -= OnSceneUnloaded;
            }
        }

        private void OnSceneUnloaded(UnityEngine.SceneManagement.Scene scene)
        {
            // If the unloaded scene was the GameScene and we haven't sent a result, it's an abandonment
            if (scene.name == "GameScene")
            {
                Debug.Log("[RunState] GameScene unloaded. Checking for abandonment...");
                SendAbandonedResult();
            }
        }

        public void ResetStats(int levelNumber)
        {
            Debug.Log($"[RunState] Game started level={levelNumber} sessionId={GameDataApiClient.Instance?.SessionId}");
            currentLevelNumber = levelNumber;
            fishSpawnCount = 0;
            potionSpawnCount = 0;
            heartsGained = 0;
            heartsLost = 0;
            xpEarnedInLevel = 0;
            
            if (ScoreManager.Instance != null)
                xpAtStart = ScoreManager.Instance.GetTotalXP();

            // Reset state
            hasActiveLevelRun = true;
            hasSentGameEnd = false;
            hasSentLevelResult = false;
            
            levelStartedAtISO = DateTime.UtcNow.ToString("O");
            levelStartedAtRealTime = Time.realtimeSinceStartup;
        }

        public float GetLevelDuration()
        {
            if (!hasActiveLevelRun) return 0f;
            return Time.realtimeSinceStartup - levelStartedAtRealTime;
        }

        public int GetFinalXPEarned()
        {
            if (ScoreManager.Instance != null)
                return ScoreManager.Instance.GetTotalXP() - xpAtStart;
            return xpEarnedInLevel;
        }

        public void SendAbandonedResult()
        {
            StartCoroutine(TrySendAbandonedIfActiveCoroutine("automatic"));
        }

        public System.Collections.IEnumerator TrySendAbandonedIfActiveCoroutine(string reason)
        {
            if (hasActiveLevelRun && !hasSentGameEnd)
            {
                string uid = (AuthManager.Instance != null) ? AuthManager.Instance.UserId : "UNKNOWN";
                string sid = (GameDataApiClient.Instance != null) ? GameDataApiClient.Instance.SessionId : "UNKNOWN";
                
                Debug.Log($"[Abandoned] Active run detected. Reason: {reason}");
                Debug.Log($"[Abandoned] Sending game_end abandoned uid={uid} level={currentLevelNumber} session={sid}");
                
                hasSentGameEnd = true;
                hasSentLevelResult = true;
                hasActiveLevelRun = false;
                
                if (GameDataApiClient.Instance != null)
                {
                    Debug.Log($"[Abandoned] Sending activity and levelResult for uid={uid}");
                    var activityTask = GameDataApiClient.Instance.SendActivity("game_end", currentLevelNumber, "abandoned");
                    var resultTask = GameDataApiClient.Instance.SendLevelResult("abandoned");

                    // Wait for both requests to finish if we are in a Coroutine
                    if (activityTask != null) yield return activityTask;
                    if (resultTask != null) yield return resultTask;
                    
                    Debug.Log("[Abandoned] All telemetry sent successfully before state change.");
                }
            }
            else if (hasActiveLevelRun)
            {
                Debug.Log($"[Abandoned] Skipped: result already sent (reason: {reason})");
            }
            else
            {
                Debug.Log($"[Abandoned] Skipped: no active run (reason: {reason})");
            }
            yield break;
        }

        private void OnApplicationQuit()
        {
            SendAbandonedResult();
        }

        public void OnFishSpawned() => fishSpawnCount++;
        public void OnPotionSpawned() => potionSpawnCount++;
        public void OnHeartGained() => heartsGained++;
        public void OnHeartLost() => heartsLost++;
        public void AddXPEarned(int amount) => xpEarnedInLevel += amount;
    }
}
