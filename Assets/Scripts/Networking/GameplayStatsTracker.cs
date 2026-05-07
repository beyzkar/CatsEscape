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
        public bool hasLevelEnded = false;
        public bool resultAlreadySent = false;
        
        // Aliases for compatibility
        public bool hasSentGameEnd => resultAlreadySent;
        public bool hasSentLevelResult => resultAlreadySent;
        
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
                SendAbandonedResult();
            }
        }

        public void ResetStats(int levelNumber)
        {
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
            hasLevelEnded = false;
            resultAlreadySent = false;
            
            levelStartedAtISO = DateTime.UtcNow.ToString("O");
            levelStartedAtRealTime = Time.realtimeSinceStartup;
            if (GameDataApiClient.Instance != null)
            {
                GameDataApiClient.Instance.SendActivity("game_start", currentLevelNumber, null);
            }
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

        public void TrackLevelResult(string result)
        {
            if (resultAlreadySent)
            {
                return;
            }

            resultAlreadySent = true;
            hasLevelEnded = true;
            hasActiveLevelRun = false;

            if (GameDataApiClient.Instance != null)
            {
                // Send to both activity and level-result collections
                GameDataApiClient.Instance.SendActivity("level_result", currentLevelNumber, result);
                GameDataApiClient.Instance.SendLevelResult(result);
            }
        }

        public void SendAbandonedResult()
        {
            if (hasActiveLevelRun && !hasLevelEnded && !resultAlreadySent)
            {
                TrackLevelResult("abandoned");
            }
        }

        public System.Collections.IEnumerator TrySendAbandonedIfActiveCoroutine(string reason)
        {
            if (hasActiveLevelRun && !hasLevelEnded && !resultAlreadySent)
            {
                string uid = (AuthManager.Instance != null) ? AuthManager.Instance.UserId : "UNKNOWN";
                string sid = (GameDataApiClient.Instance != null) ? GameDataApiClient.Instance.SessionId : "UNKNOWN";
                float duration = GetLevelDuration();
                
                TrackLevelResult("abandoned");
                yield return null; // Small wait for coroutine consistency
            }
            yield break;
        }

        private void OnApplicationQuit()
        {
            SendAbandonedResult();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            // On mobile, backgrounding the app often means abandonment if not resumed
            if (pauseStatus)
            {
                SendAbandonedResult();
            }
        }

        public void OnFishSpawned() => fishSpawnCount++;
        public void OnPotionSpawned() => potionSpawnCount++;
        public void OnHeartGained() => heartsGained++;
        public void OnHeartLost() => heartsLost++;
        public void AddXPEarned(int amount) => xpEarnedInLevel += amount;
    }
}
