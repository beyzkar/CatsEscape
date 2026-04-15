using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine.SceneManagement;

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
    public TMP_InputField nameSpace; 
    public GameObject scoreboard; 
    public GameObject leaderboardPanel; 
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

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            bool isScoreboardActive = (scoreboard != null && scoreboard.activeInHierarchy);
            bool isLeaderboardActive = (leaderboardPanel != null && leaderboardPanel.activeInHierarchy);

            if (isScoreboardActive || isLeaderboardActive)
            {
                if (nameSpace != null && nameSpace.isFocused) return;
                OnRetryButtonClick();
            }
        }
    }

    public void CheckForHighScore(int currentScore)
    {
        if (scoreboard != null) scoreboard.SetActive(true);
        if (leaderboardPanel != null) leaderboardPanel.SetActive(false);
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

    public void OnSaveButtonClick()
    {
        if (nameSpace == null) return;

        string name = !string.IsNullOrEmpty(nameSpace.text) ? nameSpace.text : "Player";
        int currentXP = (ScoreManager.Instance != null) ? ScoreManager.Instance.GetTotalXP() : 0;
        
        SaveAndShowLeaderboard(name, currentXP);
    }

    public void OnSkipAndMainMenuButtonClick()
    {
        LevelManager.ResetPersistentLevel();
        ResetGameDynamics();
        SceneManager.LoadScene("MainMenu");
    }

    public void OnRetryButtonClick()
    {
        ResetGameDynamics();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void ResetGameDynamics()
    {
        Time.timeScale = 1f;
        GameSpeed.Multiplier = 1f;
    }

    public void SaveAndShowLeaderboard(string playerName, int currentXP)
    {
        // Immediate UI feedback (add to memory, but don't save to cache yet - cache is for server sync only)
        scores.Add(new ScoreData(playerName, currentXP));
        scores = scores.OrderByDescending(s => s.xp).Take(10).ToList();
        UpdateLeaderboardUI();

        if (scoreboard != null) scoreboard.SetActive(false);
        if (leaderboardPanel != null) leaderboardPanel.SetActive(true);

        if (apiService != null)
            StartCoroutine(CoSubmitAndSync(playerName, currentXP));
    }

    private IEnumerator CoSubmitAndSync(string playerName, int currentXP)
    {
        SubmitScoreResult result = null;
        yield return apiService.SubmitScore(playerName, currentXP, r => result = r);

        if (result == null || !result.success)
        {
            Debug.LogWarning("[Leaderboard] Offline: Adding score to pending queue.");
            pendingScores.Add(new ScoreData(playerName, currentXP));
            SavePending();
        }
        
        // Always try to fetch latest (this will overwrite cache with SSOT data)
        yield return CoSyncCycle();
    }

    private IEnumerator CoSyncCycle()
    {
        if (apiService == null) yield break;

        // 1. Flush Pending Queue
        if (pendingScores.Count > 0)
        {
            Debug.Log($"[Leaderboard] Syncing {pendingScores.Count} pending scores...");
            List<ScoreData> toRemove = new List<ScoreData>();
            
            foreach (var ps in pendingScores)
            {
                SubmitScoreResult res = null;
                yield return apiService.SubmitScore(ps.playerName, ps.xp, r => res = r);
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
        yield return apiService.FetchLeaderboard((success, error, list) =>
        {
            ok = success;
            entries = list;
        });

        if (ok && entries != null)
        {
            ApplyServerEntries(entries);
            SaveCache(); // Update our read-only cache
        }
        
        UpdateLeaderboardUI();
    }

    private void ApplyServerEntries(LeaderboardApiEntry[] entries)
    {
        var merged = new List<ScoreData>();
        foreach (var e in entries)
        {
            if (e == null) continue;
            merged.Add(new ScoreData(e.playerName ?? "Player", e.score));
        }
        scores = merged.OrderByDescending(s => s.xp).Take(10).ToList();
    }

    private void UpdateLeaderboardUI()
    {
        if (entryTexts == null) return;
        for (int i = 0; i < entryTexts.Length; i++)
        {
            if (entryTexts[i] == null) continue;
            if (i < scores.Count)
                entryTexts[i].text = $"{i + 1}. {scores[i].playerName} - {scores[i].xp} XP";
            else
                entryTexts[i].text = $"{i + 1}. ---";
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
