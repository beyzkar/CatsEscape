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
        public bool isLevelActive = false;
        public bool hasSentResult = false;
        public string levelStartedAtISO;
        private float levelStartedAtRealTime;

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

        public void ResetStats(int levelNumber)
        {
            Debug.Log($"[StatsTracker] Resetting stats for Level {levelNumber}");
            currentLevelNumber = levelNumber;
            fishSpawnCount = 0;
            potionSpawnCount = 0;
            heartsGained = 0;
            heartsLost = 0;
            xpEarnedInLevel = 0;
            
            if (ScoreManager.Instance != null)
                xpAtStart = ScoreManager.Instance.GetTotalXP();

            // Reset state
            isLevelActive = true;
            hasSentResult = false;
            levelStartedAtISO = DateTime.UtcNow.ToString("O");
            levelStartedAtRealTime = Time.realtimeSinceStartup;
        }

        public float GetLevelDuration()
        {
            if (!isLevelActive) return 0f;
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
            if (isLevelActive && !hasSentResult)
            {
                hasSentResult = true;
                isLevelActive = false;
                Debug.Log($"[StatsTracker] Level {currentLevelNumber} ABANDONED. Sending result...");
                
                if (GameDataApiClient.Instance != null)
                {
                    GameDataApiClient.Instance.SendLevelResult("abandoned");
                    GameDataApiClient.Instance.SendActivity("game_end", currentLevelNumber, "abandoned");
                }
            }
        }

        private void OnApplicationQuit()
        {
            SendAbandonedResult();
        }

        private void OnDestroy()
        {
            // If this object is destroyed while a level is active, it might be a scene change to Main Menu
            // However, since it's DontDestroyOnLoad, it only destroys on app quit or manual destroy.
        }

        public void OnFishSpawned() => fishSpawnCount++;
        public void OnPotionSpawned() => potionSpawnCount++;
        public void OnHeartGained() => heartsGained++;
        public void OnHeartLost() => heartsLost++;
        public void AddXPEarned(int amount) => xpEarnedInLevel += amount;
    }
}
