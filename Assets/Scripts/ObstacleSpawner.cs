using System.Collections;
using UnityEngine;

// Manages the procedural spawning of obstacles, enemies, and power-ups across different levels
public class ObstacleSpawner : MonoBehaviour
{
    public static ObstacleSpawner Instance { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    [Header("Prefabs")]
    public GameObject obstaclePrefab;   // ObstacleBag
    public GameObject enemyPrefab;      // Standard Enemy
    public GameObject wallPrefab;       // Wall
    public GameObject longWallPrefab;   // LongWall
    public GameObject fishPrefab;       // Fish
    public GameObject potionPrefab;     // Potion
    public GameObject bushPrefab;         // Bush

    [Header("Spawn Positions")]
    public float bagSpawnX = 12f;
    public float bagSpawnY = -2.3f;
    public float enemySpawnX = 12f;
    public float enemySpawnY = -2.0f;
    public float wallSpawnX = 12f;
    public float wallSpawnY = -2.0f; 
    public float longWallSpawnX = 12f;
    public float longWallSpawnY = -2.0f;
    public float wallScaleY1 = 1.5f;
    public float wallScaleY2 = 2.5f;
    public float bushSpawnY = -3.3f;
    public float fishSpawnX = 12f;
    public float fishSpawnY = -1.5f;
    public float potionSpawnMinY = -2f;
    public float potionSpawnMaxY = 1.5f;

    [Header("Distance Tracking")]
    private float accumulatedDistance = 0f;
    private float nextSpawnTarget = 0f;
    private float lastRewardDistance = -100f; // Track distance of last potion/fish
    private const float MIN_DISTANCE_BETWEEN_REWARDS = 80f; // Each reward now requires at least distance units of travel
    
    private int lastTrackedLevel = -1;
    private int lethalPassedCount = 0; // Tracks how many Enemies/Bushes the player bypassed
    private bool isSpawningDisabled = false;

    [System.Serializable]
    public class Level1Settings {
        public float minDistance = 28f; // Even more relaxed
        public float maxDistance = 38f;
        [Range(0f, 1f)] public float bagChance = 0.5f;
        [Range(0f, 1f)] public float fishChance = 0.4f;
        [Range(0f, 1f)] public float potionChance = 0.15f;
    }
    [System.Serializable]
    public class Level2Settings {
        public float minDistance = 24f;
        public float maxDistance = 32f;
        [Range(0f, 1f)] public float bagChance = 0.4f;
        [Range(0f, 1f)] public float enemyChance = 0.35f;
        public GameObject levelEnemyPrefab;
        [Range(0f, 1f)] public float fishChance = 0.25f;
        [Range(0f, 1f)] public float potionChance = 0.1f;
    }
    [System.Serializable]
    public class Level3Settings {
        public float minDistance = 20f;
        public float maxDistance = 26f;
        [Range(0f, 1f)] public float bagChance = 0.3f;
        [Range(0f, 1f)] public float enemyChance = 0.3f;
        public GameObject levelEnemyPrefab;
        [Range(0f, 1f)] public float wallChance = 0.2f;
        [Range(0f, 1f)] public float longWallChance = 0.1f;
        [Range(0f, 1f)] public float fishChance = 0.15f;
        [Range(0f, 1f)] public float potionChance = 0.08f;
    }
    [System.Serializable]
    public class Level4Settings {
        public float minDistance = 15f;
        public float maxDistance = 22f;
        [Range(0f, 1f)] public float bagChance = 0.25f;
        [Range(0f, 1f)] public float enemyChance = 0.25f;
        public GameObject levelEnemyPrefab;
        [Range(0f, 1f)] public float wallChance = 0.3f;
        [Range(0f, 1f)] public float bushChance = 0.2f;
        [Range(0f, 1f)] public float fishChance = 0.08f;
        [Range(0f, 1f)] public float potionChance = 0.05f;
    }
    [System.Serializable]
    public class Level5Settings {
        public float minDistance = 12f;
        public float maxDistance = 16f;
        [Range(0f, 1f)] public float fishChance = 0.05f;
        [Range(0f, 1f)] public float potionChance = 0.03f;
        [Range(0f, 1f)] public float pitChance = 0.3f; 
    }

    [Header("Level Specific Settings")]
    public Level1Settings level1 = new Level1Settings();
    public Level2Settings level2 = new Level2Settings();
    public Level3Settings level3 = new Level3Settings();
    public Level4Settings level4 = new Level4Settings();
    public Level5Settings level5 = new Level5Settings();

    void Start()
    {
        // Set the very first spawn target distance using Level 1 settings
        nextSpawnTarget = Random.Range(level1.minDistance, level1.maxDistance);
    }

    void Update()
    {
        if (isSpawningDisabled) return;

        // Only accumulate distance if the game is actually "moving" and player is NOT stuck
        if (GameSpeed.Multiplier <= 0) return;
        if (PlayerMovement.Instance != null)
        {
            PlayerObstacleRules pRules = PlayerMovement.Instance.GetComponent<PlayerObstacleRules>();
            if (pRules != null && pRules.IsStuck) return;
        }

        float playerVel = (PlayerMovement.Instance != null) ? PlayerMovement.Instance.CurrentVelocityX : 0f;
        
        // Advance the spawn timer ONLY when the player is moving FORWARD (Right)
        // This ensures spawning is perfectly synced with actual world travel
        if (playerVel > 0)
        {
            accumulatedDistance += playerVel * GameSpeed.Multiplier * Time.deltaTime;
        }

        // Check if we've reached the point to spawn something new
        if (accumulatedDistance >= nextSpawnTarget)
        {
            SpawnOne();
            
            // Set the next target relative to current accumulated distance based on current level
            int currentLevel = (LevelManager.Instance != null) ? LevelManager.Instance.currentLevel : 1;
            float minD = 20f, maxD = 30f;

            switch (currentLevel)
            {
                case 1: minD = level1.minDistance; maxD = level1.maxDistance; break;
                case 2: minD = level2.minDistance; maxD = level2.maxDistance; break;
                case 3: minD = level3.minDistance; maxD = level3.maxDistance; break;
                case 4: minD = level4.minDistance; maxD = level4.maxDistance; break;
                case 5: minD = level5.minDistance; maxD = level5.maxDistance; break;
            }

            nextSpawnTarget = accumulatedDistance + Random.Range(minD, maxD);
        }
    }

    private void SpawnOne()
    {
        int currentLevel = (LevelManager.Instance != null) ? LevelManager.Instance.currentLevel : 1;

        // Reset obstacle tracking if the level changed
        if (currentLevel != lastTrackedLevel)
        {
            lastTrackedLevel = currentLevel;
            lethalPassedCount = 0;
        }

        float bag = 0, enemy = 0, wall = 0, longWall = 0, bush = 0, fish = 0, potion = 0;
        
        switch (currentLevel)
        {
            case 1:
                bag = level1.bagChance;
                fish = level1.fishChance;
                potion = level1.potionChance;
                break;
            case 2:
                bag = level2.bagChance;
                enemy = level2.enemyChance;
                fish = level2.fishChance;
                potion = level2.potionChance;
                break;
            case 3:
                bag = level3.bagChance;
                enemy = level3.enemyChance;
                wall = level3.wallChance;
                longWall = level3.longWallChance;
                fish = level3.fishChance;
                potion = level3.potionChance;
                break;
            case 4:
                bag = level4.bagChance;
                enemy = level4.enemyChance;
                wall = level4.wallChance;
                bush = level4.bushChance;
                fish = level4.fishChance;
                potion = level4.potionChance;
                break;
            case 5:
            default:
                fish = level5.fishChance;
                potion = level5.potionChance;
                break;
        }

        // Logic Requirement: No fish or potions before passing at least 3 lethal obstacles (Enemy/Bush)
        // Only applies to Levels 2, 3, and 4
        if (currentLevel >= 2 && currentLevel <= 4 && lethalPassedCount < 3)
        {
            fish = 0f;
            potion = 0f;
        }
        
        // FORCE Rarity: Only in Level 5, travel distance must be respected
        if (currentLevel == 5 && (accumulatedDistance - lastRewardDistance < MIN_DISTANCE_BETWEEN_REWARDS))
        {
            fish = 0f;
            potion = 0f;
        }

        // Level 5 Safety: Skip spawning items directly above pits
        if (currentLevel == 5 && GroundSpawner.Instance != null && GroundSpawner.Instance.IsPitAtX(bagSpawnX))
        {
            return;
        }

        float totalChance = bag + enemy + wall + longWall + bush + fish + potion;
        if (totalChance <= 0) return; 
        
        float rnd = Random.Range(0f, totalChance);
        float currentLimit = 0f;

        // Spawn Bag
        currentLimit += bag;
        if (rnd < currentLimit)
        {
            if (obstaclePrefab != null)
                Instantiate(obstaclePrefab, new Vector3(bagSpawnX, bagSpawnY, 0f), Quaternion.identity);
            return;
        }

        // Spawn Enemy
        currentLimit += enemy;
        if (rnd < currentLimit)
        {
            GameObject prefab = enemyPrefab;
            if (currentLevel == 2 && level2.levelEnemyPrefab != null) prefab = level2.levelEnemyPrefab;
            else if (currentLevel == 3 && level3.levelEnemyPrefab != null) prefab = level3.levelEnemyPrefab;
            else if (currentLevel == 4 && level4.levelEnemyPrefab != null) prefab = level4.levelEnemyPrefab;

            if (prefab != null)
                Instantiate(prefab, new Vector3(enemySpawnX, enemySpawnY, 0f), Quaternion.identity);
            return;
        }

        // Spawn Wall
        currentLimit += wall;
        if (rnd < currentLimit)
        {
            if (wallPrefab != null)
            {
                float randomScaleY = Random.value < 0.5f ? wallScaleY1 : wallScaleY2;
                GameObject w = Instantiate(wallPrefab, new Vector3(wallSpawnX, wallSpawnY, 0f), Quaternion.identity);
                w.transform.localScale = new Vector3(w.transform.localScale.x, randomScaleY, w.transform.localScale.z);
            }
            return;
        }

        // Spawn LongWall
        currentLimit += longWall;
        if (rnd < currentLimit)
        {
            if (longWallPrefab != null)
                Instantiate(longWallPrefab, new Vector3(longWallSpawnX, longWallSpawnY, 0f), Quaternion.identity);
            return;
        }

        // Spawn Bush
        currentLimit += bush;
        if (rnd < currentLimit)
        {
            if (bushPrefab != null)
                Instantiate(bushPrefab, new Vector3(wallSpawnX, bushSpawnY, 0f), Quaternion.identity);
            return;
        }

        // Spawn Fish
        currentLimit += fish;
        if (rnd < currentLimit)
        {
            if (fishPrefab != null)
            {
                Instantiate(fishPrefab, new Vector3(fishSpawnX, fishSpawnY, 0f), Quaternion.identity);
                lastRewardDistance = accumulatedDistance; // Reset cooldown
            }
            return;
        }

        // Spawn Potion
        currentLimit += potion;
        if (rnd < currentLimit)
        {
            if (potionPrefab != null)
            {
                float randomY = Random.Range(potionSpawnMinY, potionSpawnMaxY);
                Instantiate(potionPrefab, new Vector3(fishSpawnX, randomY, 0f), Quaternion.identity);
                lastRewardDistance = accumulatedDistance; // Reset cooldown
            }
            return;
        }
    }

    public void NotifyLethalPassed()
    {
        lethalPassedCount++;
    }

    public void SetSpawningEnabled(bool enabled) => isSpawningDisabled = !enabled;
}