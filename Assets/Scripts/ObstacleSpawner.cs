using System.Collections;
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject obstaclePrefab;   // ObstacleBag
    public GameObject bodyguardPrefab;  // BodyGuard
    public GameObject barbedWirePrefab; // BarbedWire
    public GameObject wallPrefab;       // Wall
    public GameObject longWallPrefab;   // LongWall
    public GameObject fishPrefab;       // Fish

    [Header("Spawn Positions")]
    public float bagSpawnX = 12f;
    public float bagSpawnY = -2.3f;
    public float bodyguardSpawnX = 12f;
    public float bodyguardSpawnY = -2.0f;
    public float barbedWireSpawnX = 12f;
    public float barbedWireSpawnY = -3.66f;
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
        public float bagY = -2.3f;
        [Range(0f, 1f)] public float fishChance = 0.2f;
    }
    [System.Serializable]
    public class Level2Settings {
        [Range(0f, 1f)] public float bagChance = 0.4f;
        public float bagY = -2.3f;
        [Range(0f, 1f)] public float bodyguardChance = 0.4f;
        public float bodyguardY = -2.0f;
        [Range(0f, 1f)] public float fishChance = 0.2f;
    }
    [System.Serializable]
    public class Level3Settings {
        [Range(0f, 1f)] public float bagChance = 0.3f;
        public float bagY = -2.3f;
        [Range(0f, 1f)] public float bodyguardChance = 0.3f;
        public float bodyguardY = -2.0f;
        [Range(0f, 1f)] public float wallChance = 0.2f;
        public float wallY = -2.0f;
        [Range(0f, 1f)] public float longWallChance = 0.1f;
        public float longWallY = -2.0f;
        [Range(0f, 1f)] public float fishChance = 0.1f;
    }
    [System.Serializable]
    public class Level4Settings {
        [Range(0f, 1f)] public float bagChance = 0.25f;
        public float bagY = -2.3f;
        [Range(0f, 1f)] public float bodyguardChance = 0.25f;
        public float bodyguardY = -2.0f;
        [Range(0f, 1f)] public float wallChance = 0.25f;
        public float wallY = -2.0f;
        [Range(0f, 1f)] public float barbedWireChance = 0.15f;
        public float barbedWireY = -3.66f;
        [Range(0f, 1f)] public float fishChance = 0.1f;
    }

    [Header("Level Specific Settings")]
    public Level1Settings level1 = new Level1Settings();
    public Level2Settings level2 = new Level2Settings();
    public Level3Settings level3 = new Level3Settings();
    public Level4Settings level4 = new Level4Settings();

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

            float bag = 0, bodyguard = 0, wall = 0, longWall = 0, barbed = 0, fish = 0;
            float currentBagY = bagSpawnY;
            float currentBodyguardY = bodyguardSpawnY;
            float currentWallY = wallSpawnY;
            float currentLongWallY = longWallSpawnY;
            float currentBarbedY = barbedWireSpawnY;

            switch (currentLevel)
            {
                case 1:
                    bag = level1.bagChance;
                    currentBagY = level1.bagY;
                    fish = level1.fishChance;
                    break;
                case 2:
                    bag = level2.bagChance;
                    currentBagY = level2.bagY;
                    bodyguard = level2.bodyguardChance;
                    currentBodyguardY = level2.bodyguardY;
                    fish = level2.fishChance;
                    break;
                case 3:
                    bag = level3.bagChance;
                    currentBagY = level3.bagY;
                    bodyguard = level3.bodyguardChance;
                    currentBodyguardY = level3.bodyguardY;
                    wall = level3.wallChance;
                    currentWallY = level3.wallY;
                    longWall = level3.longWallChance;
                    currentLongWallY = level3.longWallY;
                    fish = level3.fishChance;
                    break;
                case 4:
                default:
                    bag = level4.bagChance;
                    currentBagY = level4.bagY;
                    bodyguard = level4.bodyguardChance;
                    currentBodyguardY = level4.bodyguardY;
                    wall = level4.wallChance;
                    currentWallY = level4.wallY;
                    barbed = level4.barbedWireChance;
                    currentBarbedY = level4.barbedWireY;
                    fish = level4.fishChance;
                    break;
            }

            float totalChance = bag + bodyguard + wall + longWall + barbed + fish;
            float rnd = Random.Range(0f, totalChance);
            float currentLimit = 0f;

            // Check Bag
            currentLimit += bag;
            if (rnd < currentLimit)
            {
                if (obstaclePrefab != null)
                    Instantiate(obstaclePrefab, new Vector3(bagSpawnX, currentBagY, 0f), Quaternion.identity);
                continue;
            }

            // Check Bodyguard
            currentLimit += bodyguard;
            if (rnd < currentLimit)
            {
                if (bodyguardPrefab != null)
                    Instantiate(bodyguardPrefab, new Vector3(bodyguardSpawnX, currentBodyguardY, 0f), Quaternion.identity);
                continue;
            }

            // Check Wall
            currentLimit += wall;
            if (rnd < currentLimit)
            {
                if (wallPrefab != null)
                {
                    float randomScaleY = Random.value < 0.5f ? wallScaleY1 : wallScaleY2;
                    GameObject w = Instantiate(wallPrefab, new Vector3(wallSpawnX, currentWallY, 0f), Quaternion.identity);
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
                    Instantiate(longWallPrefab, new Vector3(longWallSpawnX, currentLongWallY, 0f), Quaternion.identity);
                }
                else
                {
                    Debug.LogWarning("LongWall chosen but longWallPrefab is NULL! Please assign it in the Inspector.");
                }
                continue;
            }

            // Check BarbedWire
            currentLimit += barbed;
            if (rnd < currentLimit)
            {
                if (barbedWirePrefab != null)
                    Instantiate(barbedWirePrefab, new Vector3(barbedWireSpawnX, currentBarbedY, 0f), Quaternion.identity);
                continue;
            }

            // Check Fish
            currentLimit += fish;
            if (rnd < currentLimit)
            {
                if (fishPrefab != null)
                {
                    float randomY = Random.Range(-2f, 1.5f);
                    Instantiate(fishPrefab, new Vector3(fishSpawnX, randomY, 0f), Quaternion.identity);
                }
                continue;
            }
        }
    }
}