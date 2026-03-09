using UnityEngine;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Level Settings")]
    public int currentLevel = 1;
    public int obstaclesPassed = 0;
    
    // Seviye hedefleri (Engel sayıları): Level 1 (10), Level 2 (20), Level 3 (20), Level 4 (30)
    private int[] levelGoals = { 0, 5, 5, 30, 30 };

    [Header("UI Panels")]
    public GameObject victoryPanel;
    public GameObject normalLevelContent; // NormalLEvelContent nesnesini buraya sürükleyin
    public GameObject finalLevelContent;  // FinalLevelContent nesnesini buraya sürükleyin
    public Button continueButton;         // ContinueButton nesnesini buraya sürükleyin
    public Button mainMenuButton;         // MainMenuButton nesnesini buraya sürükleyin
    
    [Header("Backgrounds")]
    public GameObject[] levelBackgrounds; // 4 tane harita objesini buraya sürükleyin

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // İlk seviye arka planını aktif et, diğerlerini kapat
        UpdateBackgroundVisibility();
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

        if (obstaclesPassed >= levelGoals[currentLevel])
        {
            ShowVictory();
        }
    }

    private void ShowVictory()
    {
        Time.timeScale = 0f;
        GameSpeed.Multiplier = 0f;

        // Arka plan videosunu durdur
        UnityEngine.Video.VideoPlayer bgv = Object.FindFirstObjectByType<UnityEngine.Video.VideoPlayer>();
        if (bgv != null) bgv.Pause();

        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);

            if (currentLevel < 4)
            {
                // Level 1-3: Normal içerik gösterilsin
                if (normalLevelContent != null) normalLevelContent.SetActive(true);
                if (finalLevelContent != null) finalLevelContent.SetActive(false);
                
                // Play Level Win Sound
                if (AudioManager.Instance != null) AudioManager.Instance.PlayLevelWinSound();
            }
            else
            {
                // Level 4: Final içeriği (You're Home) gösterilsin
                if (normalLevelContent != null) normalLevelContent.SetActive(false);
                if (finalLevelContent != null) finalLevelContent.SetActive(true);

                // Play Final Win Sound
                if (AudioManager.Instance != null) AudioManager.Instance.PlayFinalWinSound();
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

        // Arka plan videosunu devam ettir
        UnityEngine.Video.VideoPlayer bgv = Object.FindFirstObjectByType<UnityEngine.Video.VideoPlayer>();
        if (bgv != null) bgv.Play();

        // Seviye arka planını güncelle
        UpdateBackgroundVisibility();

        Debug.Log("Starting Level " + currentLevel);
    }
}
