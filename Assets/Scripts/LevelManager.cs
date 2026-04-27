using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Serialization;
using System.Collections;
using CatsEscape.Networking;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Level Settings")]
    public int currentLevel = 1;
    public int obstaclesPassed = 0;
    
    // Level persistence (static variable, -1: Not yet assigned)
    private static int savedLevel = -1;
    
    private int[] levelGoals = { 0, 7, 12, 17, 22, 5 };

    public float[] levelSpeeds = { 0.8f, 1.1f, 1.5f, 2.0f, 2.6f };
    public float[] enemySpeeds = { 0f, 0f, 0.8f, 1.5f, 2.2f, 3.2f };
    
    [Header("Speed Smoothing")]
    public float speedSmoothRate = 1.5f;
    private float targetGameMultiplier = 1f;

    [Header("UI Panels")]
    public GameObject victoryPanel;
    public TMP_Text levelStatusText;
    public TMP_Text inGameLevelText;
    public GameObject normalLevelContent;
    public GameObject finalLevelContent;
    
    [Header("Backgrounds")]
    public GameObject[] levelBackgrounds;

    [Header("Final Level 5 Portal")]
    [Tooltip("Assign the static portal prefab/instance. When null, Level 5 falls back to the legacy grounded victory.")]
    [SerializeField] private FinalPortal finalPortal;

    [Header("Home Exit (Levels 1-4)")]
    public Sprite homeSprite;
    public Vector2 homeSpawnPosition = new Vector2(12f, -2.2f);
    public Vector3 homeScale = Vector3.one;
    public Vector2 homeColliderSize = new Vector2(1.4f, 1.4f);
    public float homeMoveSpeed = 7f;
    public float homeDestroyX = -15f;

    [Header("Home Transition")]
    public TransitionController transitionController;
    public LevelVideoTransitionManager videoTransitionManager;
    public Animator homeTransitionAnimator;
    public string homeTransitionTriggerName = "Play";
    public float homeTransitionDuration = 1.1f;

    [System.Serializable]
    public class ThemeAssets
    {
        public Sprite obstacleSprite;
        public Vector3 obstacleScale = Vector3.one; 
        public Sprite wallSprite;
        public Vector3 wallScale = Vector3.one;
        
        [FormerlySerializedAs("yOffset")]
        public float obstacleYOffset = 0f;
        public float wallYOffset = 0f;
        public float longWallYOffset = 0f;
        public float enemyYOffset = 0f;
        public float bushYOffset = 0f;
        public float playerYOffset = 0f;

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
    public ThemeAssets[] levelThemes;

    public ThemeAssets GetCurrentTheme()
    {
        int index = currentLevel - 1;
        if (levelThemes != null && index >= 0 && index < levelThemes.Length)
            return levelThemes[index];
        return null;
    }

    public float GetCurrentBaseSpeed()
    {
        int index = currentLevel - 1;
        if (levelSpeeds != null && index >= 0 && index < levelSpeeds.Length)
            return levelSpeeds[index];
        return 1f;
    }

    private bool pendingVictory = false;

    /// <summary>Obstacle count required to finish the current level (from internal goals; Level 5 uses this for the portal).</summary>
    public int LevelTargetObstacleCount
    {
        get
        {
            if (currentLevel < 0 || currentLevel >= levelGoals.Length) return 0;
            return levelGoals[currentLevel];
        }
    }
    private PlayerMovement playerMovement;
    private GameObject homeExitObject;
    private bool homeExitActive = false;
    private bool homeTransitionInProgress = false;
    private bool worldStoppedForHome = false;
    private bool goalReachedPendingHome = false;
    private float distanceAtGoal = 0f;

    private void Start()
    {
        if (Camera.main != null)
        {
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.backgroundColor = new Color(0.12f, 0.12f, 0.16f);
        }

        InitializeTransitionPanel();
        
        if (GameplayStatsTracker.Instance != null)
            GameplayStatsTracker.Instance.ResetStats(currentLevel);
    }

    private void InitializeTransitionPanel()
    {
        if (transitionController == null) 
        {
            Debug.LogWarning("LEVEL_MANAGER: TransitionController atanmamış. " +
                             "Level geçişleri fallback kullanacak.");
            return;
        }

        transitionController.ForceReset();
        Debug.Log("LEVEL_MANAGER: TransitionController başlatıldı ve gizlendi.");
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
        if (savedLevel > 0)
        {
            currentLevel = savedLevel;
            Debug.Log("LEVEL MANAGER: Saved Level'dan devam ediliyor: " + currentLevel);
        }
        else
        {
            savedLevel = currentLevel;
        }

        // --- HIGHLANDER PATTERN: Ensure only a single Player instance exists ---
        PlayerMovement[] allPlayers = Object.FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        if (allPlayers.Length > 1)
        {
            Debug.LogWarning($"[LevelManager] {allPlayers.Length} players detected. Cleanup in progress...");
            foreach (var p_obj in allPlayers)
            {
                if (p_obj != PlayerMovement.Instance && p_obj != null)
                {
                    Destroy(p_obj.gameObject);
                }
            }
        }

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerMovement = p.GetComponent<PlayerMovement>();

        // Ensure we prioritize any portal already placed/edited in the Scene Hierarchy
        if (finalPortal == null || !finalPortal.gameObject.scene.IsValid())
        {
            FinalPortal existingInScene = Object.FindAnyObjectByType<FinalPortal>();
            if (existingInScene != null) 
            {
                finalPortal = existingInScene;
                Debug.Log($"LEVEL_MANAGER: Found existing portal in scene: {finalPortal.name}. Using its settings.");
            }
        }

        if (finalPortal != null)
        {
            Scene s = finalPortal.gameObject.scene;
            if (!s.IsValid() || !s.isLoaded)
            {
                // Only instantiate if the reference is a prefab and no scene instance was found
                GameObject spawned = Instantiate(finalPortal.gameObject);
                spawned.name = "FinalPortal";
                spawned.SetActive(false);
                finalPortal = spawned.GetComponent<FinalPortal>();
                Debug.Log("LEVEL_MANAGER: Instantiated new portal from prefab.");
            }
        }

        UpdateBackgroundVisibility();
        UpdateInGameLevelText();
        ApplyPlayerOffset();
        PrepareHomeExitForCurrentLevel();
    }

    /// <summary>
    /// Surgical Fix 5.0: Resets the static saved level to force a fresh start.
    /// </summary>
    public static void ResetSavedLevel()
    {
        savedLevel = -1;
        Debug.Log("[LevelManager] Global SavedLevel has been reset for a fresh start.");
    }

    private void ApplyPlayerOffset()
    {
        ThemeAssets theme = GetCurrentTheme();
        if (playerMovement != null)
        {
            float targetY = (theme != null) ? theme.playerYOffset : 0f;
            playerMovement.externalVisualYOffset = targetY;
            
            if (targetY != 0)
                Debug.Log("Level " + currentLevel + ": Player Y Offset -> " + targetY);
        }
    }

    private void UpdateInGameLevelText()
    {
        if (inGameLevelText != null)
            inGameLevelText.text = "LEVEL " + currentLevel;
    }

    private void PrepareHomeExitForCurrentLevel()
    {
        homeExitActive = false;
        worldStoppedForHome = false;

        if (currentLevel >= 5)
        {
            if (homeExitObject != null) homeExitObject.SetActive(false);
            return;
        }

        EnsureHomeExitObject();
        if (homeExitObject != null)
        {
            homeExitObject.SetActive(false);
            homeExitObject.transform.position = new Vector3(homeSpawnPosition.x, homeSpawnPosition.y, 0f);
        }
    }

    private void EnsureHomeExitObject()
    {
        if (homeExitObject != null || homeSprite == null) return;

        homeExitObject = new GameObject("HomeExit");
        homeExitObject.transform.position = new Vector3(homeSpawnPosition.x, homeSpawnPosition.y, 0f);
        homeExitObject.transform.localScale = homeScale;

        SpriteRenderer sr = homeExitObject.AddComponent<SpriteRenderer>();
        sr.sprite = homeSprite;
        sr.sortingLayerName = "Default";
        sr.sortingOrder = 25;

        BoxCollider2D col = homeExitObject.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = homeColliderSize;

        Rigidbody2D rb2d = homeExitObject.AddComponent<Rigidbody2D>();
        rb2d.bodyType = RigidbodyType2D.Kinematic;
        rb2d.simulated = true;
        rb2d.gravityScale = 0f;
        rb2d.useFullKinematicContacts = true;

        ObstacleMove move = homeExitObject.AddComponent<ObstacleMove>();
        move.speed = homeMoveSpeed;
        move.destroyX = homeDestroyX;
        move.moveEvenWhenMultiplierIsZero = false;

        homeExitObject.AddComponent<HomeExitTrigger>();
        homeExitObject.SetActive(false);
    }

    private void ActivateHomeExit()
    {
        if (currentLevel >= 5 || homeExitActive) return;

        EnsureHomeExitObject();
        if (homeExitObject == null) return;

        homeExitObject.transform.position = new Vector3(homeSpawnPosition.x, homeSpawnPosition.y, 0f);
        homeExitObject.transform.localScale = homeScale;

        ObstacleMove move = homeExitObject.GetComponent<ObstacleMove>();
        if (move != null)
        {
            move.speed = homeMoveSpeed;
            move.destroyX = homeDestroyX;
        }

        homeExitObject.SetActive(true);
        homeExitActive = true;

        if (playerMovement != null)
            playerMovement.DisableJumping();
    }

    public void TryCompleteLevelViaHome()
    {
        if (currentLevel >= 5 || !homeExitActive || homeTransitionInProgress) return;

        if (AudioManager.Instance != null) AudioManager.Instance.PlayPassTheGate();

        homeTransitionInProgress = true;
        homeExitActive = false;
        if (homeExitObject != null) homeExitObject.SetActive(false);

        if (videoTransitionManager != null)
            StartCoroutine(HomeVideoTransition());
        else if (transitionController != null)
            StartCoroutine(HomeTransitionWithController());
        else
        {
            Debug.LogError("LEVEL_MANAGER: TransitionController atanmamış! Fallback kullanılıyor.");
            StartCoroutine(HomeTransitionFallback());
        }
    }

    private IEnumerator HomeVideoTransition()
    {
        if (homeTransitionAnimator != null)
            homeTransitionAnimator.SetTrigger(homeTransitionTriggerName);

        DisablePlayerMovement();

        videoTransitionManager.StartTransition(() =>
        {
            NextLevel();
            homeTransitionInProgress = false;
            EnablePlayerMovement();
        });

        yield break;
    }

    private void DisablePlayerMovement()
    {
        if (playerMovement != null)
        {
            playerMovement.FreezeForTransition(true);
            playerMovement.enabled = false;
        }
    }

    private void EnablePlayerMovement()
    {
        if (playerMovement != null)
        {
            playerMovement.enabled = true;
            playerMovement.FreezeForTransition(false);
        }
    }

    private IEnumerator HomeTransitionWithController()
    {
        if (homeTransitionAnimator != null)
            homeTransitionAnimator.SetTrigger(homeTransitionTriggerName);

        transitionController.StartTransition(() =>
        {
            NextLevel();
            homeTransitionInProgress = false;
        });

        yield break;
    }

    private IEnumerator HomeTransitionFallback()
    {
        SetTargetSpeed(0f);
        GameSpeed.Multiplier = 0f;

        bool hadPlayerMovement = (playerMovement != null && playerMovement.enabled);
        if (playerMovement != null) playerMovement.enabled = false;

        yield return new WaitForSecondsRealtime(homeTransitionDuration);

        if (playerMovement != null) playerMovement.enabled = hadPlayerMovement;
        
        NextLevel();
        homeTransitionInProgress = false;
    }

    private void Update()
    {
        if (targetGameMultiplier != GameSpeed.Multiplier)
        {
            if (targetGameMultiplier == 0f)
                GameSpeed.Multiplier = 0f;
            else
                GameSpeed.Multiplier = Mathf.MoveTowards(
                    GameSpeed.Multiplier, targetGameMultiplier, speedSmoothRate * Time.deltaTime);
        }

        // --- EMPTY ROAD DELAY ---
        if (goalReachedPendingHome && playerMovement != null)
        {
            if (playerMovement.totalDistance >= distanceAtGoal + 12f)
            {
                goalReachedPendingHome = false;
                ActivateHomeExit();
            }
        }

        // --- HOME EXIT CINEMATIC LOGIC ---
        if (homeExitActive && homeExitObject != null && !worldStoppedForHome)
        {
            if (homeExitObject.transform.position.x <= 0f) // Mid-screen
            {
                worldStoppedForHome = true;
                SetTargetSpeed(0f);
                GameSpeed.Multiplier = 0f; // Force instant stop for better feel
                
                if (playerMovement != null)
                    playerMovement.StartLevelEndWalk();
            }
        }

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
        if (!Application.isPlaying)
            UpdateBackgroundVisibility();
        else
            ApplyPlayerOffset();
    }

    private void UpdateBackgroundVisibility()
    {
        if (levelBackgrounds == null || levelBackgrounds.Length == 0) return;

        for (int i = 0; i < levelBackgrounds.Length; i++)
        {
            if (levelBackgrounds[i] != null)
                levelBackgrounds[i].SetActive(i == (currentLevel - 1));
        }
    }

    public static void ResetPersistentLevel()
    {
        savedLevel = 1;
    }

    public void MainMenu()
    {
        ResetPersistentLevel();
        
        // Global hard reset for XP when returning to start
        if (ScoreManager.Instance != null) ScoreManager.Instance.ResetAllXP();

        Time.timeScale = 1f;
        targetGameMultiplier = 1f;
        GameSpeed.Multiplier = 1f;
        
        if (GameOverManager.Instance != null)
            GameOverManager.Instance.LoadMainMenu();
    }

    public void ObstaclePassed()
    {
        // Level 5 Completion Lock: Ensure we never count beyond the target for a clean end-state
        if (currentLevel == 5 && obstaclesPassed >= LevelTargetObstacleCount) 
        {
            return;
        }

        obstaclesPassed++;
        
        if (currentLevel == 5)
            Debug.Log("[LevelManager] Level 5 Bridge Landed: " + obstaclesPassed + "/" + LevelTargetObstacleCount);

        CheckLevelProgress();
        UpdateInGameLevelText();
    }

    public void ResetProgress()
    {
        if (currentLevel == 5)
        {
            obstaclesPassed = 0;
            if (finalPortal != null)
                finalPortal.ResetAfterLevelProgressCleared();
            UpdateInGameLevelText();
            Debug.Log("Level 5: Engel çarpması! Progress SIFIRLAND.");
        }
    }

    private void CheckLevelProgress()
    {
        if (currentLevel > 5) return;

        if (obstaclesPassed >= levelGoals[currentLevel])
        {
            if (currentLevel < 5)
            {
                if (!goalReachedPendingHome && !homeExitActive)
                {
                    if (ObstacleSpawner.Instance != null) ObstacleSpawner.Instance.SetSpawningEnabled(false);
                    goalReachedPendingHome = true;
                    distanceAtGoal = (playerMovement != null) ? playerMovement.totalDistance : 0f;
                    Debug.Log("Level goal reached. Spawning stopped. Waiting for empty road...");
                }
            }
            else
            {
                // Stop spawning ground pieces to create a 'gap' / end of path
                if (GroundSpawner.Instance != null && currentLevel == 5)
                    GroundSpawner.Instance.StopSpawning();

                if (finalPortal != null)
                    finalPortal.TrySpawnFromLevelManager();
                else
                {
                    Debug.LogWarning("LEVEL_MANAGER: FinalPortal atanmamış — Level 5 için eski 'zeminde zafer' akışı kullanılıyor.");
                    pendingVictory = true;
                }
            }
        }
    }

    /// <summary>Invoked by FinalPortal after the exit animation and delay; opens victory / save-score flow once.</summary>
    public void OpenFinalVictoryAfterPortalExit()
    {
        ShowVictory();
    }

    private void ShowVictory()
    {
        Time.timeScale = 0f;
        targetGameMultiplier = 0f;
        GameSpeed.Multiplier = 0f;

        if (AudioManager.Instance != null) AudioManager.Instance.StopBackgroundMusic();

        UpdateInGameLevelText();

        if (victoryPanel != null)
        {
            if (levelStatusText != null)
                levelStatusText.text = "Level " + currentLevel + " Survived!";

            if (currentLevel < 5)
            {
                if (normalLevelContent != null) normalLevelContent.SetActive(true);
                if (finalLevelContent != null) finalLevelContent.SetActive(false);
                
                if (AudioManager.Instance != null) AudioManager.Instance.PlayLevelWinSound();
                victoryPanel.SetActive(true);
            }
            else
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayFinalWinSound();
                
                // NEW: Update game stats
                if (CatsEscape.Auth.AuthManager.Instance != null)
                {
                    CatsEscape.Auth.AuthManager.Instance.TotalCompletions++;
                    CatsEscape.Auth.AuthManager.Instance.LastLevelReached = 5;
                }

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

        // Send results to backend
        if (GameDataApiClient.Instance != null)
            GameDataApiClient.Instance.SendLevelResult("completed");
    }

    public void NextLevel()
    {
        if (currentLevel >= 5) return;

        // NEW: Sync results to backend IMMEDIATELY
        if (CatsEscape.Networking.GameDataApiClient.Instance != null)
        {
            CatsEscape.Networking.GameDataApiClient.Instance.SendLevelResult("completed");
        }
        else
        {
            Debug.LogError("[CRITICAL-API-ERROR] GameDataApiClient.Instance is NULL in NextLevel! Is the object missing from MainMenu?");
        }

        // Bank current XP before switching levels
        if (ScoreManager.Instance != null) ScoreManager.Instance.CommitSessionXP();

        homeExitActive = false;
        if (homeExitObject != null) homeExitObject.SetActive(false);

        ClearCurrentObstacles();

        currentLevel++;
        savedLevel = currentLevel;

        // NEW: Update last reached level
        if (CatsEscape.Auth.AuthManager.Instance != null)
        {
            CatsEscape.Auth.AuthManager.Instance.LastLevelReached = currentLevel;
        }

        obstaclesPassed = 0;
        
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (normalLevelContent != null) normalLevelContent.SetActive(false);
        if (finalLevelContent != null) finalLevelContent.SetActive(false);

        goalReachedPendingHome = false;
        if (ObstacleSpawner.Instance != null) ObstacleSpawner.Instance.SetSpawningEnabled(true);
        
        Time.timeScale = 1f;
        
        if (playerMovement != null)
        {
            PlayerObstacleRules rules = playerMovement.GetComponent<PlayerObstacleRules>();
            if (rules != null) rules.ResetForLevelStart();
            playerMovement.PrepareForLevelStart();
            if (rules != null) rules.UpdateGameSpeed();
        }
        else
        {
            GameSpeed.Multiplier = GetCurrentBaseSpeed();
        }

        if (AudioManager.Instance != null) AudioManager.Instance.PlayBackgroundMusic();

        if (transitionController != null)
            transitionController.ForceReset();

        if (videoTransitionManager != null)
            videoTransitionManager.ForceReset();

        UpdateBackgroundVisibility();
        UpdateInGameLevelText();
        ApplyPlayerOffset();
        PrepareHomeExitForCurrentLevel();

        if (GameplayStatsTracker.Instance != null)
            GameplayStatsTracker.Instance.ResetStats(currentLevel);

        Debug.Log("Level " + currentLevel + " başladı.");
    }

    private void ClearCurrentObstacles()
    {
        ObstacleMove[] activeObstacles = Object.FindObjectsByType<ObstacleMove>(FindObjectsSortMode.None);
        foreach (var obs in activeObstacles)
        {
            if (obs != null) Destroy(obs.gameObject);
        }
    }
}