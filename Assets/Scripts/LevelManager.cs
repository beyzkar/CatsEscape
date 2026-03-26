using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Serialization;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Level Settings")]
    public int currentLevel = 1;
    public int obstaclesPassed = 0;
    
    // Seviye sürekliliği için static değişken (-1: Henüz atanmadı demek)
    private static int savedLevel = -1;
    
    // Seviye hedefleri: Level 1 (10), Level 2 (15), Level 3 (25), Level 4 (35), Level 5 (50)
    private int[] levelGoals = { 0, 10, 15, 25, 10, 10 };

    [Header("Speed Settings")]
    public float[] levelSpeeds = { 1.0f, 1.35f, 1.7f, 2.1f, 2.5f };

    [Header("UI Panels")]
    public GameObject victoryPanel;
    public TMP_Text levelStatusText; // Victory panelindeki yazı
    public TMP_Text inGameLevelText; // Oyun içindeki üst orta yazı
    public GameObject normalLevelContent; // NormalLEvelContent nesnesini buraya sürükleyin
    public GameObject finalLevelContent;  // FinalLevelContent nesnesini buraya sürükleyin
    public Button continueButton;         // ContinueButton nesnesini buraya sürükleyin
    public Button mainMenuButton;         // MainMenuButton nesnesini buraya sürükleyin
    
    [Header("Backgrounds")]
    public GameObject[] levelBackgrounds; // 5 tane harita objesini buraya sürükleyin

    [System.Serializable]
    public class ThemeAssets
    {
        public Sprite obstacleSprite; // ObstacleBag görseli
        public Vector3 obstacleScale = Vector3.one; 
        public Sprite wallSprite;     // Wall görseli
        public Vector3 wallScale = Vector3.one;
        
        [FormerlySerializedAs("yOffset")]
        public float obstacleYOffset = 0f; // ObstacleBag için Y ayarı
        public float wallYOffset = 0f;     // Duvarlar için Y ayarı
    }

    [Header("Theme Assets")]
    public ThemeAssets[] levelThemes; // Her level için sprite'ları buraya ekleyin (5 elemanlı)

    public ThemeAssets GetCurrentTheme()
    {
        int index = currentLevel - 1;
        if (levelThemes != null && index >= 0 && index < levelThemes.Length)
        {
            return levelThemes[index];
        }
        return null;
    }

    public float GetCurrentBaseSpeed()
    {
        int index = currentLevel - 1;
        if (levelSpeeds != null && index >= 0 && index < levelSpeeds.Length)
        {
            return levelSpeeds[index];
        }
        return 1f;
    }

    private bool pendingVictory = false;
    private PlayerMovement playerMovement;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Seviye sürekliliğini sağla
        if (savedLevel == -1)
        {
            // İlk açılışta Inspector'daki değeri baz al (Test kolaylığı için)
            savedLevel = currentLevel;
        }
        else
        {
            // Sonraki yüklemelerde (Retry vb.) kaydedilen seviyeyi kullan
            currentLevel = savedLevel;
        }

        // PlayerMovement referansını al
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerMovement = p.GetComponent<PlayerMovement>();

        // İlk seviye arka planını aktif et, diğerlerini kapat
        UpdateBackgroundVisibility();
        UpdateInGameLevelText();
    }

    private void UpdateInGameLevelText()
    {
        if (inGameLevelText != null)
        {
            // Sadece LEVEL X yazar (PITS sayacı kaldırıldı)
            inGameLevelText.text = "LEVEL " + currentLevel;
        }
    }

    private void Update()
    {
        // Eğer seviye hedefi tamamlandıysa ve kedi yere bastıysa zaferi göster
        if (pendingVictory && playerMovement != null && playerMovement.IsGrounded)
        {
            pendingVictory = false; // Flag'i sıfırla
            ShowVictory();
        }
    }

    private void OnValidate()
    {
        // Editör modunda Current Level'ı değiştirince hemen görsün
        if (!Application.isPlaying)
        {
            UpdateBackgroundVisibility();
        }
    }

    private void UpdateBackgroundVisibility()
    {
        // Search backgrounds only
        if (levelBackgrounds == null || levelBackgrounds.Length == 0) return;

        for (int i = 0; i < levelBackgrounds.Length; i++)
        {
            if (levelBackgrounds[i] != null)
            {
                // currentLevel 1-based, array index 0-based
                // Sadece mevcut seviyenin arka planı aktif olsun
                levelBackgrounds[i].SetActive(i == (currentLevel - 1));
            }
        }
    }

    public static void ResetPersistentLevel()
    {
        savedLevel = 1;
    }

    public void MainMenu()
    {
        ResetPersistentLevel(); // Seviyeyi sıfırla
        Time.timeScale = 1f;
        GameSpeed.Multiplier = 1f; // Main Menu is always 1.0f
        if (GameOverManager.Instance != null)
        {
            GameOverManager.Instance.LoadMainMenu();
        }
    }

    public void ObstaclePassed()
    {
        obstaclesPassed++;
        
        // Konsolda Level 5 ilerlemesini göster
        if (currentLevel == 5)
        {
            Debug.Log("Level 5 - Zemin/Cukur gecildi! Sayac: " + obstaclesPassed + "/10");
        }

        CheckLevelProgress();
        UpdateInGameLevelText(); // Sayacı güncelle
    }

    public void ResetProgress()
    {
        if (currentLevel == 5)
        {
            obstaclesPassed = 0;
            UpdateInGameLevelText();
            Debug.Log("Level 5 - Engel vuruldu! Sayac SIFIRLANDI.");
        }
    }

    private void CheckLevelProgress()
    {
        if (currentLevel > 5) return;

        // Hedefe ulaşıldıysa ShowVictory yerine pendingVictory işaretle
        if (obstaclesPassed >= levelGoals[currentLevel])
        {
            pendingVictory = true; 
        }
    }

    private void ShowVictory()
    {
        Time.timeScale = 0f;
        GameSpeed.Multiplier = 0f;

        // Stop BGM while victory sound plays
        if (AudioManager.Instance != null) AudioManager.Instance.StopBackgroundMusic();

        // Arka plan videosunu durdur
        UnityEngine.Video.VideoPlayer bgv = Object.FindFirstObjectByType<UnityEngine.Video.VideoPlayer>();
        if (bgv != null) bgv.Pause();

        // Level 5'te sayacı son kez güncelle
        UpdateInGameLevelText();

        if (victoryPanel != null)
        {
            // Level bilgisini yazdır
            if (levelStatusText != null)
                levelStatusText.text = "Level " + currentLevel + " Survived!";

            if (currentLevel < 5)
            {
                // Level 1-4: Normal içerik gösterilsin
                if (normalLevelContent != null) normalLevelContent.SetActive(true);
                if (finalLevelContent != null) finalLevelContent.SetActive(false);
                
                // Play Level Win Sound
                if (AudioManager.Instance != null) AudioManager.Instance.PlayLevelWinSound();

                // Show victory panel immediately for non-final levels
                if (victoryPanel != null) victoryPanel.SetActive(true);
            }
            else
            {
                // Level 5: Önce Scoreboard sonra Leaderboard açılması isteniyor (Gemini Promptu gereği)
                if (normalLevelContent != null) normalLevelContent.SetActive(false);
                if (finalLevelContent != null) finalLevelContent.SetActive(true);

                // Play Final Win Sound
                if (AudioManager.Instance != null) AudioManager.Instance.PlayFinalWinSound();

                if (LeaderboardManager.Instance != null)
                {
                    // Sıralama (Sequence): Scoreboard -> Leaderboard -> VictoryPanel
                    LeaderboardManager.Instance.nextPanelToOpen = victoryPanel; 

                    int currentXP = (ScoreManager.Instance != null) ? ScoreManager.Instance.GetTotalXP() : 0;
                    LeaderboardManager.Instance.CheckForHighScore(currentXP);
                    
                    Debug.Log("LevelManager: Level 5 bitirildi. Scoreboard açılıyor...");
                }
                else
                {
                    // Fallback
                    if (victoryPanel != null) victoryPanel.SetActive(true);
                }
            }
        }
    }


    public void NextLevel()
    {
        if (currentLevel >= 5) return;

        currentLevel++;
        savedLevel = currentLevel; // Kaydet
        obstaclesPassed = 0;
        
        // Panelleri kapat ve devam et
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (normalLevelContent != null) normalLevelContent.SetActive(false);
        if (finalLevelContent != null) finalLevelContent.SetActive(false);
        
        Time.timeScale = 1f;
        
        // Update player speed for the new level
        if (playerMovement != null)
        {
            PlayerObstacleRules rules = playerMovement.GetComponent<PlayerObstacleRules>();
            if (rules != null) rules.UpdateGameSpeed();
        }
        else
        {
            GameSpeed.Multiplier = GetCurrentBaseSpeed();
        }

        // Restart BGM for the next level
        if (AudioManager.Instance != null) AudioManager.Instance.PlayBackgroundMusic();

        // Arka plan videosunu devam ettir
        UnityEngine.Video.VideoPlayer bgv = Object.FindFirstObjectByType<UnityEngine.Video.VideoPlayer>();
        if (bgv != null) bgv.Play();

        // Seviye arka planını ve yazısını güncelle
        UpdateBackgroundVisibility();
        UpdateInGameLevelText();

        Debug.Log("Starting Level " + currentLevel);
    }
}
