using UnityEngine;
using System;

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
            if (hasActiveLevelRun && !hasSentGameEnd)
            {
                Debug.Log("[RunState] Sending abandoned result");
                hasSentGameEnd = true;
                hasSentLevelResult = true;
                hasActiveLevelRun = false;
                
                if (GameDataApiClient.Instance != null)
                {
                    Debug.Log("[Activity] game_end abandoned sent");
                    GameDataApiClient.Instance.SendActivity("game_end", currentLevelNumber, "abandoned");
                    
                    Debug.Log("[LevelResult] abandoned sent");
                    GameDataApiClient.Instance.SendLevelResult("abandoned");
                }
            }
            else if (hasActiveLevelRun)
            {
                Debug.Log("[RunState] Skipped abandoned because result already sent");
            }
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
