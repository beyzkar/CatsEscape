using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine.SceneManagement;
using CatsEscape.Networking;

public class LeaderboardManager : MonoBehaviour
{
    [System.Serializable]
    public class ScoreData
    {
        public string playerName;
        public int xp;
        public ScoreData(string name, int score) { playerName = name; xp = score; }
    }

    public static LeaderboardManager Instance { get; private set; }

    [Header("UI Layout")]
    public GameObject scoreboard; 
    public GameObject leaderboardPanel; 
    public UnityEngine.UI.Button saveButton;
    public TextMeshProUGUI[] entryTexts; 

    [Header("Flow Control")]
    public GameObject nextPanelToOpen; 

    [Header("Online leaderboard (SSOT)")]
    public LeaderboardApiService apiService;

    private string cachePath;
    private string pendingPath;
    private List<ScoreData> scores = new List<ScoreData>();
    private List<ScoreData> pendingScores = new List<ScoreData>();

    private void Awake()
    {
        Instance = this;

        // Paths: leaderboard.json is CACHE only. pending_scores.json is QUEUE only.
        cachePath = Path.Combine(Application.persistentDataPath, "leaderboard.json");
        pendingPath = Path.Combine(Application.persistentDataPath, "pending_scores.json");

        LoadCache();
        LoadPending();
        
        if (leaderboardPanel != null) leaderboardPanel.SetActive(false);
        if (scoreboard != null) scoreboard.SetActive(false);

        // Start Sync Cycle
        if (apiService != null)
            StartCoroutine(CoSyncCycle());
    }


    public void CheckForHighScore(int currentScore)
    {
        if (scoreboard != null) scoreboard.SetActive(true);
        if (leaderboardPanel != null) leaderboardPanel.SetActive(false);
        
        // Reset button state for new attempt
        if (saveButton != null)
        {
            saveButton.interactable = true;
            var cg = saveButton.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 1f;
        }
    }

    public void ShowLeaderboardOnly()
    {
        if (scoreboard != null) scoreboard.SetActive(false);
        if (leaderboardPanel != null) leaderboardPanel.SetActive(true);

        // Refresh from server if possible
        if (apiService != null)
            StartCoroutine(CoSyncCycle());
        else
            UpdateLeaderboardUI();
    }

    private bool _isSubmitting = false;

    public void OnSaveButtonClick()
    {
        Debug.Log("[LeaderboardManager] Save Button Clicked.");
        
        // Strictly use the username from AuthManager as it's mandatory
        string name = (CatsEscape.Auth.AuthManager.Instance != null) 
            ? CatsEscape.Auth.AuthManager.Instance.GetFormattedUserName() 
            : "Player";

        if (_isSubmitting)
        {
            Debug.LogWarning("[LeaderboardManager] Submission already in progress.");
            return;
        }

        int currentXP = (ScoreManager.Instance != null) ? ScoreManager.Instance.GetTotalXP() : 0;
        
        Debug.Log($"[LeaderboardManager] Proceeding to save with mandatory username: {name} | XP: {currentXP}");

        // Disable and fade button to prevent multiple saves
        if (saveButton != null)
        {
            saveButton.interactable = false;
            var cg = saveButton.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 0.5f;
        }

        SaveAndShowLeaderboard(name, currentXP);
    }

    public void OnSkipAndMainMenuButtonClick()
    {
        LevelManager.ResetPersistentLevel();
        ResetGameDynamics();
        SceneManager.LoadScene("MainMenu");
    }

    public void OnCloseLeaderboard()
    {
        if (leaderboardPanel != null) leaderboardPanel.SetActive(false);
        if (scoreboard != null) scoreboard.SetActive(true);
    }

    private void ResetGameDynamics()
    {
        Time.timeScale = 1f;
        GameSpeed.Multiplier = 1f;
    }

    public void SaveAndShowLeaderboard(string playerName, int currentXP)
    {
        if (scoreboard != null) scoreboard.SetActive(false);
        if (leaderboardPanel != null) leaderboardPanel.SetActive(true);

        if (apiService != null)
        {
            int currentLevel = (LevelManager.Instance != null) ? LevelManager.Instance.currentLevel : 1;
            string authType = (CatsEscape.Auth.AuthManager.Instance != null) ? CatsEscape.Auth.AuthManager.Instance.GetLoginType().ToLower() : "guest";
            int xpEarned = (GameplayStatsTracker.Instance != null) ? GameplayStatsTracker.Instance.GetFinalXPEarned() : 0;
            float timeSeconds = 0f; 

            Debug.Log($"[LeaderboardManager] Starting API Submit Coroutine for {playerName}");
            StartCoroutine(CoSubmitAndSync(playerName, currentXP, currentLevel, authType, xpEarned, timeSeconds));
        }
        else
        {
            // Fallback for offline/no API
            scores.Add(new ScoreData(playerName, currentXP));
            scores = scores.OrderByDescending(s => s.xp).Take(5).ToList();
            UpdateLeaderboardUI();
        }
    }

    private IEnumerator CoSubmitAndSync(string playerName, int currentXP, int levelNumber, string authType, int xpEarned, float timeSeconds)
    {
        if (_isSubmitting) yield break;
        _isSubmitting = true;

        SubmitScoreResult result = null;
        yield return apiService.SubmitScore(playerName, currentXP, levelNumber, authType, xpEarned, timeSeconds, r => result = r);

        if (result == null || !result.success)
        {
            Debug.LogWarning("[Leaderboard] Failed to save online. Adding to pending queue.");
            pendingScores.Add(new ScoreData(playerName, currentXP));
            SavePending();
        }
        
        // Always try to fetch latest 
        yield return CoSyncCycle();

        _isSubmitting = false;
    }

    private IEnumerator CoSyncCycle()
    {
        if (apiService == null) yield break;

        // 1. Flush Pending Queue
        if (pendingScores.Count > 0)
        {
            List<ScoreData> toRemove = new List<ScoreData>();
            
            foreach (var ps in pendingScores)
            {
                SubmitScoreResult res = null;
                int level = (LevelManager.Instance != null) ? LevelManager.Instance.currentLevel : 1;
                string auth = (CatsEscape.Auth.AuthManager.Instance != null) ? CatsEscape.Auth.AuthManager.Instance.GetLoginType().ToLower() : "guest";
                
                yield return apiService.SubmitScore(ps.playerName, ps.xp, level, auth, 0, 0, r => res = r);
                if (res != null && res.success)
                    toRemove.Add(ps);
                else
                    break; // Stop syncing if internet is still spotty
            }

            foreach (var r in toRemove) pendingScores.Remove(r);
            if (toRemove.Count > 0) SavePending();
        }

        // 2. Fetch Latest from SSOT (Server)
        bool ok = false;
        LeaderboardApiEntry[] entries = null;
        int? currentLevel = (LevelManager.Instance != null) ? (int?)LevelManager.Instance.currentLevel : null;
        string fetchError = "";

        yield return apiService.FetchLeaderboard(currentLevel, (success, error, list) =>
        {
            ok = success;
            fetchError = error;
            entries = list;
        });

        if (ok && entries != null)
        {
            ApplyServerEntries(entries);
            SaveCache(); // Update our read-only cache
        }
        else if (!ok)
        {
            Debug.LogError($"[LEADERBOARD] Sync failed: {fetchError}");
        }
        
        UpdateLeaderboardUI();
    }

    private void ApplyServerEntries(LeaderboardApiEntry[] entries)
    {
        var merged = new List<ScoreData>();
        foreach (var e in entries)
        {
            if (e == null) continue;
            string nameToUse = !string.IsNullOrEmpty(e.userName) ? e.userName : (e.displayName ?? "Player");
            merged.Add(new ScoreData(nameToUse, e.score));
        }
        // Take TOP 5 only
        scores = merged.OrderByDescending(s => s.xp).Take(5).ToList();
    }

    private void UpdateLeaderboardUI()
    {
        if (entryTexts == null) return;
        
        // Ensure we always fill 5 rows (as per UI layout)
        int rowCount = Mathf.Min(entryTexts.Length, 5);
        
        for (int i = 0; i < rowCount; i++)
        {
            if (entryTexts[i] == null) continue;
            
            if (i < scores.Count)
            {
                entryTexts[i].text = $"{i + 1}. {scores[i].playerName} - {scores[i].xp} XP";
            }
            else
            {
                entryTexts[i].text = $"{i + 1}. ---";
                // Only log empty rows once or if needed, but keeping it simple
            }
        }
    }

    private void SaveCache()
    {
        try
        {
            string json = JsonUtility.ToJson(new LeaderboardWrapper { allScores = scores }, true);
            File.WriteAllText(cachePath, json);
        }
        catch (System.Exception e) { Debug.LogError($"[Leaderboard] Cache save error: {e.Message}"); }
    }

    private void LoadCache()
    {
        try
        {
            if (File.Exists(cachePath))
            {
                string json = File.ReadAllText(cachePath);
                var wrapper = JsonUtility.FromJson<LeaderboardWrapper>(json);
                scores = wrapper?.allScores ?? new List<ScoreData>();
            }
        }
        catch (System.Exception e) 
        { 
            Debug.LogWarning($"[Leaderboard] Cache load error (resetting): {e.Message}");
            scores = new List<ScoreData>();
        }
    }

    private void SavePending()
    {
        try
        {
            string json = JsonUtility.ToJson(new LeaderboardWrapper { allScores = pendingScores }, true);
            File.WriteAllText(pendingPath, json);
        }
        catch (System.Exception e) { Debug.LogError($"[Leaderboard] Pending queue save error: {e.Message}"); }
    }

    private void LoadPending()
    {
        try
        {
            if (File.Exists(pendingPath))
            {
                string json = File.ReadAllText(pendingPath);
                var wrapper = JsonUtility.FromJson<LeaderboardWrapper>(json);
                pendingScores = wrapper?.allScores ?? new List<ScoreData>();
            }
        }
        catch (System.Exception e) 
        { 
            Debug.LogWarning($"[Leaderboard] Pending load error: {e.Message}");
            pendingScores = new List<ScoreData>();
        }
    }

    [System.Serializable]
    private class LeaderboardWrapper { public List<ScoreData> allScores; }
}
