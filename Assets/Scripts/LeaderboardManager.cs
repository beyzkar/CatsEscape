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

    [Header("UI Yerlesimi")]
    public TMP_InputField nameSpace; 
    public GameObject scoreboard; 
    public GameObject leaderboardPanel; 
    public TextMeshProUGUI[] entryTexts; 

    [Header("Akis Kontrolu")]
    public GameObject nextPanelToOpen; 

    private string savePath;
    private List<ScoreData> scores = new List<ScoreData>();

    private void Awake()
    {
        if (Instance == null) 
        {
            Instance = this;
            Debug.Log($"LeaderboardManager: [{gameObject.name}] üzerinde Instance atandı.");
        }
        else 
        {
            Debug.LogWarning($"LeaderboardManager: [{gameObject.name}] üzerinde kopya script bulundu! Bu objeyi siliyorum.");
            Destroy(gameObject);
            return;
        }

        // Kayıt yerini Assets klasörü (proje içi) olarak değiştiriyoruz
        savePath = Path.Combine(Application.dataPath, "leaderboard.json");
        LoadScores();
        
        if (leaderboardPanel != null) leaderboardPanel.SetActive(false);
        if (scoreboard != null) scoreboard.SetActive(false);
        
        Debug.Log("LeaderboardManager: Sistem hazır ve çalışıyor!");
    }

    public void CheckForHighScore(int currentScore)
    {
        Debug.Log($"LeaderboardManager: Save Score ekranı açılıyor (Skor: {currentScore})");
        
        // Artık yüksek skor olup olmadığına bakmıyoruz, her zaman giriş panelini açıyoruz.
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
        Debug.Log("LeaderboardManager: Save butonuna basıldı!");
        
        if (nameSpace == null)
        {
            Debug.LogError("LeaderboardManager: 'Name Space' (InputField) referansı boş! Lütfen Inspector'dan sürükle.");
            return;
        }

        string name = !string.IsNullOrEmpty(nameSpace.text) ? nameSpace.text : "Player";
        int currentXP = (ScoreManager.Instance != null) ? ScoreManager.Instance.GetTotalXP() : 0;
        
        Debug.Log($"LeaderboardManager: Kaydediliyor -> İsim: {name}, Skor: {currentXP}");
        SaveAndShowLeaderboard(name, currentXP);
    }

    public void OnSkipAndMainMenuButtonClick()
    {
        Debug.Log("LeaderboardManager: Skip ve Ana Menü butonuna basıldı. Seviye sıfırlanıyor.");
        
        // Seviye sürekliliğini sıfırla
        LevelManager.ResetPersistentLevel();

        // Zaman ölçeğini ve oyun hızını sıfırla (GameOver ekranından buralar donmuş olabilir)
        Time.timeScale = 1f;
        GameSpeed.Multiplier = 1f;
        
        SceneManager.LoadScene("MainMenu");
    }

    public void OnRetryButtonClick()
    {
        Debug.Log("LeaderboardManager: Retry butonuna basıldı.");
        
        // Reset time and speed scales before reloading
        Time.timeScale = 1f;
        GameSpeed.Multiplier = 1f;
        
        // Reload the current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void SaveAndShowLeaderboard(string playerName, int currentXP)
    {
        scores.Add(new ScoreData(playerName, currentXP));
        scores = scores.OrderByDescending(s => s.xp).Take(5).ToList();
        
        SaveScores();
        UpdateLeaderboardUI();

        Debug.Log("LeaderboardManager: Kayıt tamamlandı. Liste paneli açılıyor.");
        if (scoreboard != null) scoreboard.SetActive(false);
        if (leaderboardPanel != null) 
        {
            leaderboardPanel.SetActive(true);
            Debug.Log("LeaderboardManager: leaderboardPanel aktif edildi.");
        }
        else
        {
            Debug.LogError("LeaderboardManager: 'leaderboardPanel' referansı boş!");
        }
    }

    public void OnContinueButtonClick()
    {
        Debug.Log("LeaderboardManager: Devam et butonuna basıldı.");
        if (leaderboardPanel != null) leaderboardPanel.SetActive(false);
        
        if (nextPanelToOpen != null)
        {
            nextPanelToOpen.SetActive(true);
            Debug.Log($"LeaderboardManager: {nextPanelToOpen.name} paneli açıldı.");
        }
    }

    private void UpdateLeaderboardUI()
    {
        if (entryTexts == null || entryTexts.Length == 0)
        {
            Debug.LogWarning("LeaderboardManager: 'Entry Texts' listesi boş!");
            return;
        }

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
        Debug.Log($"LeaderboardManager: Skorlar kaydedildi -> {savePath}");
    }

    private void LoadScores()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            LeaderboardWrapper wrapper = JsonUtility.FromJson<LeaderboardWrapper>(json);
            scores = wrapper.allScores ?? new List<ScoreData>();
            scores = scores.OrderByDescending(s => s.xp).Take(5).ToList();
            Debug.Log("LeaderboardManager: Mevcut skorlar yüklendi.");
        }
    }

    [System.Serializable]
    private class LeaderboardWrapper { public List<ScoreData> allScores; }
}
