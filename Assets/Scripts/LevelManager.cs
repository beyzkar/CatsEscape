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
    
    // Level persistence (static variable, -1: Not yet assigned)
    private static int savedLevel = -1;
    
    private int[] levelGoals = { 0, 10, 20, 30, 40, 20 };

    [Header("Speed Settings")]
    public float[] levelSpeeds = { 1.5f, 2.02f, 2.55f, 3.15f, 3.75f }; // Each level speed increased 1.5x
    public float[] enemySpeeds = { 0f, 0f, 1.5f, 3.0f, 4.5f, 6.0f }; // Level-specific walking speeds for enemies

    [Header("UI Panels")]
    public GameObject victoryPanel;
    public TMP_Text levelStatusText; // Text in the victory panel
    public TMP_Text inGameLevelText; // On-screen level display
    public GameObject normalLevelContent;
    public GameObject finalLevelContent;
    
    [Header("Backgrounds")]
    public GameObject[] levelBackgrounds; // List of 5 background objects

    [System.Serializable]
    public class ThemeAssets
    {
        public Sprite obstacleSprite; // ObstacleBag sprite
        public Vector3 obstacleScale = Vector3.one; 
        public Sprite wallSprite;     // Wall sprite
        public Vector3 wallScale = Vector3.one;
        
        [FormerlySerializedAs("yOffset")]
        public float obstacleYOffset = 0f; // Y-offset for ObstacleBag
        public float wallYOffset = 0f;     // Y-offset for Walls

        [Header("Physics Overrides (BoxCollider2D)")]
        public Vector2 wallColliderSize = Vector2.zero;   // Set to zero to use auto-bounds
        public Vector2 wallColliderOffset = Vector2.zero; // Manual offset override
        public PhysicsMaterial2D wallPhysicsMaterial;     // Level-specific friction/bounciness
    }

    [Header("Theme Assets")]
    public ThemeAssets[] levelThemes; // Theme assets for 5 levels

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

        // Ensure level persistence
        if (savedLevel == -1)
        {
            savedLevel = currentLevel;
        }
        else
        {
            currentLevel = savedLevel;
        }

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerMovement = p.GetComponent<PlayerMovement>();

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
        // If the goal is reached, show victory only when the cat is grounded
        if (pendingVictory && playerMovement != null && playerMovement.IsGrounded)
        {
            pendingVictory = false;
            ShowVictory();
        }
    }

    private void OnValidate()
    {
        // Allow editor to preview backgrounds when changing currentLevel
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
                // Deactivate all but the current level's background
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
        ResetPersistentLevel();
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
        
        if (currentLevel == 5)
        {
            Debug.Log("Level 5 Progress: " + obstaclesPassed + "/10");
        }

        CheckLevelProgress();
        UpdateInGameLevelText();
    }

    public void ResetProgress()
    {
        if (currentLevel == 5)
        {
            obstaclesPassed = 0;
            UpdateInGameLevelText();
            Debug.Log("Level 5: Obstacle hit! Progress RESET.");
        }
    }

    private void CheckLevelProgress()
    {
        if (currentLevel > 5) return;

        if (obstaclesPassed >= levelGoals[currentLevel])
        {
            pendingVictory = true; 
        }
    }

    private void ShowVictory()
    {
        Time.timeScale = 0f;
        GameSpeed.Multiplier = 0f;

        if (AudioManager.Instance != null) AudioManager.Instance.StopBackgroundMusic();

        // Pause background video
        UnityEngine.Video.VideoPlayer bgv = Object.FindFirstObjectByType<UnityEngine.Video.VideoPlayer>();
        if (bgv != null) bgv.Pause();

        UpdateInGameLevelText();

        if (victoryPanel != null)
        {
            if (levelStatusText != null)
                levelStatusText.text = "Level " + currentLevel + " Survived!";

            if (currentLevel < 5)
            {
                // Level 1-4 Flow
                if (normalLevelContent != null) normalLevelContent.SetActive(true);
                if (finalLevelContent != null) finalLevelContent.SetActive(false);
                
                if (AudioManager.Instance != null) AudioManager.Instance.PlayLevelWinSound();
                victoryPanel.SetActive(true);
            }
            else
            {
                // Level 5 Flow: Sequence Scoreboard -> Leaderboard -> VictoryPanel
                if (normalLevelContent != null) normalLevelContent.SetActive(false);
                if (finalLevelContent != null) finalLevelContent.SetActive(true);

                if (AudioManager.Instance != null) AudioManager.Instance.PlayFinalWinSound();

                if (LeaderboardManager.Instance != null)
                {
                    LeaderboardManager.Instance.nextPanelToOpen = victoryPanel; 
                    int currentXP = (ScoreManager.Instance != null) ? ScoreManager.Instance.GetTotalXP() : 0;
                    LeaderboardManager.Instance.CheckForHighScore(currentXP);
                }
                else
                {
                    victoryPanel.SetActive(true);
                }
            }
        }
    }

    public void NextLevel()
    {
        if (currentLevel >= 5) return;

        currentLevel++;
        savedLevel = currentLevel;
        obstaclesPassed = 0;
        
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (normalLevelContent != null) normalLevelContent.SetActive(false);
        if (finalLevelContent != null) finalLevelContent.SetActive(false);
        
        Time.timeScale = 1f;
        
        if (playerMovement != null)
        {
            PlayerObstacleRules rules = playerMovement.GetComponent<PlayerObstacleRules>();
            if (rules != null) rules.UpdateGameSpeed();
        }
        else
        {
            GameSpeed.Multiplier = GetCurrentBaseSpeed();
        }

        if (AudioManager.Instance != null) AudioManager.Instance.PlayBackgroundMusic();

        UnityEngine.Video.VideoPlayer bgv = Object.FindFirstObjectByType<UnityEngine.Video.VideoPlayer>();
        if (bgv != null) bgv.Play();

        UpdateBackgroundVisibility();
        UpdateInGameLevelText();

        Debug.Log("Starting Level " + currentLevel);
    }
}

