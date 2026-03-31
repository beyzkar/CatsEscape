using System.Collections;
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    public static ObstacleSpawner Instance { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    [Header("Prefabs")]
    public GameObject obstaclePrefab;   // ObstacleBag
    public GameObject enemyPrefab;      // Was Bodyguard
    public GameObject wallPrefab;       // Wall
    public GameObject longWallPrefab;   // LongWall
    public GameObject fishPrefab;       // Fish
    public GameObject potionPrefab;     // Potion

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
    public float fishSpawnX = 12f;

    [Header("Random Time")]
    public float minDelay = 1.2f;
    public float maxDelay = 2.6f;

    [System.Serializable]
    public class Level1Settings {
        [Range(0f, 1f)] public float bagChance = 0.8f;
        [Range(0f, 1f)] public float fishChance = 0.2f;
        [Range(0f, 1f)] public float potionChance = 0.1f;
    }
    [System.Serializable]
    public class Level2Settings {
        [Range(0f, 1f)] public float bagChance = 0.4f;
        [Range(0f, 1f)] public float enemyChance = 0.4f;
        public GameObject levelEnemyPrefab; // Level unique enemy
        [Range(0f, 1f)] public float fishChance = 0.2f;
        [Range(0f, 1f)] public float potionChance = 0.1f;
    }
    [System.Serializable]
    public class Level3Settings {
        [Range(0f, 1f)] public float bagChance = 0.3f;
        [Range(0f, 1f)] public float enemyChance = 0.3f;
        public GameObject levelEnemyPrefab; // Level unique enemy
        [Range(0f, 1f)] public float wallChance = 0.2f;
        [Range(0f, 1f)] public float longWallChance = 0.1f;
        [Range(0f, 1f)] public float fishChance = 0.1f;
        [Range(0f, 1f)] public float potionChance = 0.1f;
    }
    [System.Serializable]
    public class Level4Settings {
        [Range(0f, 1f)] public float bagChance = 0.25f;
        [Range(0f, 1f)] public float enemyChance = 0.25f;
        public GameObject levelEnemyPrefab; // Level unique enemy
        [Range(0f, 1f)] public float wallChance = 0.25f;
        [Range(0f, 1f)] public float fishChance = 0.1f;
        [Range(0f, 1f)] public float potionChance = 0.1f;
    }
    [System.Serializable]
    public class Level5Settings {
        [Range(0f, 1f)] public float bagChance = 0.7f;
        [Range(0f, 1f)] public float fishChance = 0.2f;
        [Range(0f, 1f)] public float potionChance = 0.1f;
        [Range(0f, 1f)] public float pitChance = 0.2f; // New: Ground pit probability
    }

    [Header("Level Specific Settings")]
    public Level1Settings level1 = new Level1Settings();
    public Level2Settings level2 = new Level2Settings();
    public Level3Settings level3 = new Level3Settings();
    public Level4Settings level4 = new Level4Settings();
    public Level5Settings level5 = new Level5Settings();

    void Start()
    {
        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            if (GameSpeed.Multiplier <= 0)
            {
                yield return null;
                continue;
            }
            yield return new WaitForSeconds(Random.Range(minDelay, maxDelay));

            int currentLevel = 1;
            if (LevelManager.Instance != null) currentLevel = LevelManager.Instance.currentLevel;

            float bag = 0, enemy = 0, wall = 0, longWall = 0, fish = 0, potion = 0;
            
            // ... (switch remains same)

            switch (currentLevel)
            {
                case 1:
                    bag = level1.bagChance;
                    fish = level1.fishChance;
                    potion = 0.25f; // Set to a higher default for visibility
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
                    fish = level4.fishChance;
                    potion = level4.potionChance;
                    break;
                case 5:
                default:
                    // Strictly NO obstacles in Level 5
                    bag = 0f;
                    enemy = 0f; // Was bodyguard
                    wall = 0f;
                    longWall = 0f;
                    
                    // Allow only Fish and Potions
                    fish = level5.fishChance;
                    potion = level5.potionChance;
                    break;
            }

            // LEVEL 5 Check: Don't spawn any object if the ground below is a pit (to keep visuals clean)
            if (currentLevel == 5 && GroundSpawner.Instance != null && GroundSpawner.Instance.IsPitAtX(bagSpawnX))
            {
                // Debug.Log("[ObstacleSpawner] Spawn skipped because of pit at " + bagSpawnX);
                continue;
            }

            float totalChance = bag + enemy + wall + longWall + fish + potion;
            if (totalChance <= 0) 
            {
                Debug.LogWarning("[ObstacleSpawner] Total chance is 0! Check Inspector settings.");
                continue; 
            }
            
            float rnd = Random.Range(0f, totalChance);
            float currentLimit = 0f;
            

            // Check Bag
            currentLimit += bag;
            if (rnd < currentLimit)
            {
                if (obstaclePrefab != null)
                    Instantiate(obstaclePrefab, new Vector3(bagSpawnX, bagSpawnY, obstaclePrefab.transform.position.z), obstaclePrefab.transform.rotation);
                continue;
            }

            // Check Enemy (Was Bodyguard)
            currentLimit += enemy;
            if (rnd < currentLimit)
            {
                // Determine which prefab to use (Level-specific or Default)
                GameObject prefabToSpawn = enemyPrefab;
                if (currentLevel == 2 && level2.levelEnemyPrefab != null) prefabToSpawn = level2.levelEnemyPrefab;
                else if (currentLevel == 3 && level3.levelEnemyPrefab != null) prefabToSpawn = level3.levelEnemyPrefab;
                else if (currentLevel == 4 && level4.levelEnemyPrefab != null) prefabToSpawn = level4.levelEnemyPrefab;

                if (prefabToSpawn != null)
                    Instantiate(prefabToSpawn, new Vector3(enemySpawnX, enemySpawnY, prefabToSpawn.transform.position.z), prefabToSpawn.transform.rotation);
                continue;
            }

            // Check Wall
            currentLimit += wall;
            if (rnd < currentLimit)
            {
                if (wallPrefab != null)
                {
                    float randomScaleY = Random.value < 0.5f ? wallScaleY1 : wallScaleY2;
                    GameObject w = Instantiate(wallPrefab, new Vector3(wallSpawnX, wallSpawnY, wallPrefab.transform.position.z), wallPrefab.transform.rotation);
                    w.transform.localScale = new Vector3(w.transform.localScale.x, randomScaleY, w.transform.localScale.z);
                }
                continue;
            }

            // Check LongWall
            currentLimit += longWall;
            if (rnd < currentLimit)
            {
                if (longWallPrefab != null)
                {
                    Instantiate(longWallPrefab, new Vector3(longWallSpawnX, longWallSpawnY, 0f), Quaternion.identity);
                }
                else
                {
                    Debug.LogWarning("LongWall chosen but longWallPrefab is NULL! Please assign it in the Inspector.");
                }
                continue;
            }


            // Check Fish
            currentLimit += fish;
            if (rnd < currentLimit)
            {
                if (fishPrefab != null)
                    Instantiate(fishPrefab, new Vector3(fishSpawnX, fishPrefab.transform.position.y, fishPrefab.transform.position.z), fishPrefab.transform.rotation);
                continue;
            }

            // Check Potion
            currentLimit += potion;
            if (rnd < currentLimit)
            {
                if (potionPrefab != null)
                {
                    float randomY = Random.Range(-2f, 1.5f);
                    GameObject p = Instantiate(potionPrefab, new Vector3(fishSpawnX, randomY, 0f), Quaternion.identity);
                }
                else
                {
                    Debug.LogWarning("[ObstacleSpawner] Potion chosen but potionPrefab is NULL!");
                }
                continue;
            }
        }
    }
}