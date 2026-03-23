using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Level Settings")]
    public int currentLevel = 1;
    public int obstaclesPassed = 0;
    
    // Seviye hedefleri: Level 1 (10), Level 2 (15), Level 3 (25), Level 4 (35)
    private int[] levelGoals = { 0, 10, 15, 25, 5};

    [Header("UI Panels")]
    public GameObject victoryPanel;
    public TMP_Text levelStatusText; // Victory panelindeki yazı
    public TMP_Text inGameLevelText; // Oyun içindeki üst orta yazı
    public GameObject normalLevelContent; // NormalLEvelContent nesnesini buraya sürükleyin
    public GameObject finalLevelContent;  // FinalLevelContent nesnesini buraya sürükleyin
    public Button continueButton;         // ContinueButton nesnesini buraya sürükleyin
    public Button mainMenuButton;         // MainMenuButton nesnesini buraya sürükleyin
    
    [Header("Backgrounds")]
    public GameObject[] levelBackgrounds; // 4 tane harita objesini buraya sürükleyin

    [System.Serializable]
    public class ThemeAssets
    {
        public Sprite obstacleSprite; // ObstacleBag görseli
        public Vector3 obstacleScale = Vector3.one; 
        public Sprite wallSprite;     // Wall görseli
        public Vector3 wallScale = Vector3.one;
        public float yOffset = 0f;    // Y ekseni ince ayarı
    }

    [Header("Theme Assets")]
    public ThemeAssets[] levelThemes; // Her level için sprite'ları buraya ekleyin (4 elemanlı)

    public ThemeAssets GetCurrentTheme()
    {
        int index = currentLevel - 1;
        if (levelThemes != null && index >= 0 && index < levelThemes.Length)
        {
            return levelThemes[index];
        }
        return null;
    }

    private bool pendingVictory = false;
    private PlayerMovement playerMovement;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

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

    public void MainMenu()
    {
        Time.timeScale = 1f;
        GameSpeed.Multiplier = 1f;
        if (GameOverManager.Instance != null)
        {
            GameOverManager.Instance.LoadMainMenu();
        }
    }

    public void ObstaclePassed()
    {
        obstaclesPassed++;
        CheckLevelProgress();
    }

    private void CheckLevelProgress()
    {
        if (currentLevel > 4) return;

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

        if (victoryPanel != null)
        {
            // Level bilgisini yazdır
            if (levelStatusText != null)
                levelStatusText.text = "Level " + currentLevel + " Survived!";

            if (currentLevel < 4)
            {
                // Level 1-3: Normal içerik gösterilsin
                if (normalLevelContent != null) normalLevelContent.SetActive(true);
                if (finalLevelContent != null) finalLevelContent.SetActive(false);
                
                // Play Level Win Sound
                if (AudioManager.Instance != null) AudioManager.Instance.PlayLevelWinSound();

                // Show victory panel immediately for non-final levels
                if (victoryPanel != null) victoryPanel.SetActive(true);
            }
            else
            {
                // Level 4: Final içeriği (You're Home) gösterilsin
                if (normalLevelContent != null) normalLevelContent.SetActive(false);
                if (finalLevelContent != null) finalLevelContent.SetActive(true);

                // Play Final Win Sound
                if (AudioManager.Instance != null) AudioManager.Instance.PlayFinalWinSound();

                // Game completely won: Sequence the panels
                if (LeaderboardManager.Instance != null)
                {
                    // Set the VictoryPanel to show up AFTER the leaderboard is closed
                    LeaderboardManager.Instance.nextPanelToOpen = victoryPanel; 
                    
                    // Check if player made it to top 5
                    int currentXP = (ScoreManager.Instance != null) ? ScoreManager.Instance.GetTotalXP() : 0;
                    LeaderboardManager.Instance.CheckForHighScore(currentXP);

                    // Important: We DON'T show the victory panel yet!
                    // The Leaderboard will open first.
                }
                else
                {
                    // Fallback if leaderboard is missing
                    if (victoryPanel != null) victoryPanel.SetActive(true);
                }
            }
        }
    }

    public void NextLevel()
    {
        if (currentLevel >= 4) return;

        currentLevel++;
        obstaclesPassed = 0;
        
        // Panelleri kapat ve devam et
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (normalLevelContent != null) normalLevelContent.SetActive(false);
        if (finalLevelContent != null) finalLevelContent.SetActive(false);
        
        Time.timeScale = 1f;
        GameSpeed.Multiplier = 1f;

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
