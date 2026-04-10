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
    
    private int[] levelGoals = { 0, 7, 12, 17, 22, 27 };

    public float[] levelSpeeds = { 0.8f, 1.1f, 1.5f, 2.0f, 2.6f }; // Relaxed Geometric Scaling
    public float[] enemySpeeds = { 0f, 0f, 0.8f, 1.5f, 2.2f, 3.2f }; // Harmonized slower enemies
    
    [Header("Speed Smoothing")]
    public float speedSmoothRate = 1.5f; // Ultra-smooth acceleration for global speed
    private float targetGameMultiplier = 1f;

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
        public float longWallYOffset = 0f; // Y-offset for LongWalls
        public float enemyYOffset = 0f;    // Y-offset for Enemies
        public float bushYOffset = 0f;     // Y-offset for Bushes
        public float playerYOffset = 0f;   // Specific Y start for Player (used mainly in Level 5)

        public Vector2 obstacleColliderSize = Vector2.zero; 
        public Vector2 obstacleColliderOffset = Vector2.zero;
        
        public Vector2 enemyColliderSize = Vector2.zero;
        public Vector2 enemyColliderOffset = Vector2.zero;

        public Vector2 wallColliderSize = Vector2.zero;
        public Vector2 wallColliderOffset = Vector2.zero;
        public Vector2 longWallColliderSize = Vector2.zero;
        public Vector2 longWallColliderOffset = Vector2.zero;

        public Vector2 bushColliderSize = Vector2.zero;
        public Vector2 bushColliderOffset = Vector2.zero;

        public PhysicsMaterial2D wallPhysicsMaterial; 
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

    private void Start()
    {
        // PROFESSIONAL: Self-Healing Camera Init
        // This forces the screen to clear every frame, fixing the "ghosting" effect
        if (Camera.main != null)
        {
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.backgroundColor = new Color(0.12f, 0.12f, 0.16f); // Deep professional blue
        }
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        // --- RESUME MECHANIC ---
        // If a level was previously reached and saved (static), load it.
        if (savedLevel > 0)
        {
            currentLevel = savedLevel;
            Debug.Log("LEVEL MANAGER: Resuming from Saved Level " + currentLevel);
        }
        else
        {
            // First run: sync savedLevel to current editor-set level
            savedLevel = currentLevel;
        }
        // -----------------------

        // --- HIGHLANDER PATTERN: FORCE SINGLE PLAYER ---
        // Find ALL objects that might be a player and destroy any extras!
        PlayerMovement[] allPlayers = Object.FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        if (allPlayers.Length > 1)
        {
            Debug.LogWarning("LEVEL MANAGER: Duplicate players found! Cleaning up the scene...");
            for (int i = 1; i < allPlayers.Length; i++) 
            {
                // Bir tanesini (genellikle ilk bulunmayanı) yok ediyoruz
                Destroy(allPlayers[i].gameObject);
            }
        }
        // -----------------------------------------------

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerMovement = p.GetComponent<PlayerMovement>();

        UpdateBackgroundVisibility();
        UpdateInGameLevelText();
        ApplyPlayerOffset();
    }

    private void ApplyPlayerOffset()
    {
        ThemeAssets theme = GetCurrentTheme();
        if (playerMovement != null)
        {
            float targetY = (theme != null) ? theme.playerYOffset : 0f;
            playerMovement.externalVisualYOffset = targetY;
            
            if (targetY != 0)
                Debug.Log("Level " + currentLevel + ": Assigned Persistent Player Y Offset -> " + targetY);
        }
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
        // Smoothly transition the actual GameSpeed.Multiplier towards the target
        if (targetGameMultiplier != GameSpeed.Multiplier)
        {
            // MEGA DÜZELTME: Eğer hedef 0 ise (engel çarpması), bekletmeden anında durdur
            if (targetGameMultiplier == 0f) 
            {
                GameSpeed.Multiplier = 0f;
            }
            else
            {
                GameSpeed.Multiplier = Mathf.MoveTowards(GameSpeed.Multiplier, targetGameMultiplier, speedSmoothRate * Time.deltaTime);
            }
        }

        // If the goal is reached, show victory only when the cat is grounded
        if (pendingVictory && playerMovement != null && playerMovement.IsGrounded)
        {
            pendingVictory = false;
            ShowVictory();
        }
    }

    public void SetTargetSpeed(float target)
    {
        targetGameMultiplier = target;
    }

    private void OnValidate()
    {
        // Allow editor to preview backgrounds when changing currentLevel
        if (!Application.isPlaying)
        {
            UpdateBackgroundVisibility();
        }
        else
        {
            // Real-time tuning during playmode
            ApplyPlayerOffset();
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
        targetGameMultiplier = 1f;
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
        targetGameMultiplier = 0f;
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

        ClearCurrentObstacles();

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
        ApplyPlayerOffset();

        Debug.Log("Starting Level " + currentLevel);
    }

    private void ClearCurrentObstacles()
    {
        // Find and destroy everything that moves like an obstacle/hazard
        ObstacleMove[] activeObstacles = Object.FindObjectsByType<ObstacleMove>(FindObjectsSortMode.None);
        foreach (var obs in activeObstacles)
        {
            if (obs != null) Destroy(obs.gameObject);
        }
    }
}

