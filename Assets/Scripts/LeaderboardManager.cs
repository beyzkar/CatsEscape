using UnityEngine;
using TMPro;
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

    private string savePath;
    private List<ScoreData> scores = new List<ScoreData>();

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
            return;
        }

        savePath = Path.Combine(Application.dataPath, "leaderboard.json");
        LoadScores();
        
        if (leaderboardPanel != null) leaderboardPanel.SetActive(false);
        if (scoreboard != null) scoreboard.SetActive(false);
    }

    public void CheckForHighScore(int currentScore)
    {
        // Open Save Score panel
        if (scoreboard != null) scoreboard.SetActive(true);
        if (leaderboardPanel != null) leaderboardPanel.SetActive(false);
    }

    public void ShowLeaderboardOnly()
    {
        if (scoreboard != null) scoreboard.SetActive(false);
        if (leaderboardPanel != null) leaderboardPanel.SetActive(true);
        UpdateLeaderboardUI();
    }

    public void OnSaveButtonClick()
    {
        if (nameSpace == null)
        {
            Debug.LogError("LeaderboardManager: 'Name Space' (InputField) reference is missing!");
            return;
        }

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
        scores.Add(new ScoreData(playerName, currentXP));
        scores = scores.OrderByDescending(s => s.xp).Take(5).ToList();
        
        SaveScores();
        UpdateLeaderboardUI();

        if (scoreboard != null) scoreboard.SetActive(false);
        if (leaderboardPanel != null) leaderboardPanel.SetActive(true);
    }

    public void OnContinueButtonClick()
    {
        if (leaderboardPanel != null) leaderboardPanel.SetActive(false);
        if (nextPanelToOpen != null) nextPanelToOpen.SetActive(true);
    }

    private void UpdateLeaderboardUI()
    {
        if (entryTexts == null || entryTexts.Length == 0) return;

        for (int i = 0; i < entryTexts.Length; i++)
        {
            if (entryTexts[i] == null) continue;

            if (i < scores.Count)
                entryTexts[i].text = $"{i + 1}. {scores[i].playerName} - {scores[i].xp} XP";
            else
                entryTexts[i].text = $"{i + 1}. ---";
        }
    }

    private void SaveScores()
    {
        string json = JsonUtility.ToJson(new LeaderboardWrapper { allScores = scores }, true);
        File.WriteAllText(savePath, json);
    }

    private void LoadScores()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            LeaderboardWrapper wrapper = JsonUtility.FromJson<LeaderboardWrapper>(json);
            scores = wrapper.allScores ?? new List<ScoreData>();
            scores = scores.OrderByDescending(s => s.xp).Take(5).ToList();
        }
    }

    [System.Serializable]
    private class LeaderboardWrapper { public List<ScoreData> allScores; }
}
