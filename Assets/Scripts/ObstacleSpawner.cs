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
    public GameObject potionPrefab;     // Potion

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
        [Range(0f, 1f)] public float fishChance = 0.2f;
        [Range(0f, 1f)] public float potionChance = 0.1f;
    }
    [System.Serializable]
    public class Level2Settings {
        [Range(0f, 1f)] public float bagChance = 0.4f;
        [Range(0f, 1f)] public float bodyguardChance = 0.4f;
        [Range(0f, 1f)] public float fishChance = 0.2f;
        [Range(0f, 1f)] public float potionChance = 0.1f;
    }
    [System.Serializable]
    public class Level3Settings {
        [Range(0f, 1f)] public float bagChance = 0.3f;
        [Range(0f, 1f)] public float bodyguardChance = 0.3f;
        [Range(0f, 1f)] public float wallChance = 0.2f;
        [Range(0f, 1f)] public float longWallChance = 0.1f;
        [Range(0f, 1f)] public float fishChance = 0.1f;
        [Range(0f, 1f)] public float potionChance = 0.1f;
    }
    [System.Serializable]
    public class Level4Settings {
        [Range(0f, 1f)] public float bagChance = 0.25f;
        [Range(0f, 1f)] public float bodyguardChance = 0.25f;
        [Range(0f, 1f)] public float wallChance = 0.25f;
        [Range(0f, 1f)] public float barbedWireChance = 0.15f;
        [Range(0f, 1f)] public float fishChance = 0.1f;
        [Range(0f, 1f)] public float potionChance = 0.1f;
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

            float bag = 0, bodyguard = 0, wall = 0, longWall = 0, barbed = 0, fish = 0, potion = 0;

            switch (currentLevel)
            {
                case 1:
                    bag = level1.bagChance;
                    fish = level1.fishChance;
                    potion = level1.potionChance;
                    break;
                case 2:
                    bag = level2.bagChance;
                    bodyguard = level2.bodyguardChance;
                    fish = level2.fishChance;
                    potion = level2.potionChance;
                    break;
                case 3:
                    bag = level3.bagChance;
                    bodyguard = level3.bodyguardChance;
                    wall = level3.wallChance;
                    longWall = level3.longWallChance;
                    fish = level3.fishChance;
                    potion = level3.potionChance;
                    break;
                case 4:
                default:
                    bag = level4.bagChance;
                    bodyguard = level4.bodyguardChance;
                    wall = level4.wallChance;
                    barbed = level4.barbedWireChance;
                    fish = level4.fishChance;
                    potion = level4.potionChance;
                    break;
            }

            float totalChance = bag + bodyguard + wall + longWall + barbed + fish + potion;
            float rnd = Random.Range(0f, totalChance);
            float currentLimit = 0f;

            // Check Bag
            currentLimit += bag;
            if (rnd < currentLimit)
            {
                if (obstaclePrefab != null)
                    Instantiate(obstaclePrefab, new Vector3(bagSpawnX, bagSpawnY, 0f), Quaternion.identity);
                continue;
            }

            // Check Bodyguard
            currentLimit += bodyguard;
            if (rnd < currentLimit)
            {
                if (bodyguardPrefab != null)
                    Instantiate(bodyguardPrefab, new Vector3(bodyguardSpawnX, bodyguardSpawnY, 0f), Quaternion.identity);
                continue;
            }

            // Check Wall
            currentLimit += wall;
            if (rnd < currentLimit)
            {
                if (wallPrefab != null)
                {
                    float randomScaleY = Random.value < 0.5f ? wallScaleY1 : wallScaleY2;
                    GameObject w = Instantiate(wallPrefab, new Vector3(wallSpawnX, wallSpawnY, 0f), Quaternion.identity);
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

            // Check BarbedWire
            currentLimit += barbed;
            if (rnd < currentLimit)
            {
                if (barbedWirePrefab != null)
                    Instantiate(barbedWirePrefab, new Vector3(barbedWireSpawnX, barbedWireSpawnY, 0f), Quaternion.identity);
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

            // Check Potion
            currentLimit += potion;
            if (rnd < currentLimit)
            {
                if (potionPrefab != null)
                {
                    float randomY = Random.Range(-2f, 1.5f);
                    Instantiate(potionPrefab, new Vector3(fishSpawnX, randomY, 0f), Quaternion.identity);
                }
                continue;
            }
        }
    }
}