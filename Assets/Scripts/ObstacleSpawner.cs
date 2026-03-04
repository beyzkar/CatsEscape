using System.Collections;
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject obstaclePrefab;   // ObstacleBag
    public GameObject bodyguardPrefab;  // BodyGuard
    public GameObject barbedWirePrefab; // BarbedWire
    public GameObject wallPrefab;       // Wall
    public GameObject catFoodPrefab;    // CatFood

    [Header("Spawn Bag (Obstacle) X/Y")]
    public float bagSpawnX = 12f;
    public float bagSpawnY = -2.3f;

    [Header("Spawn Bodyguard X/Y")]
    public float bodyguardSpawnX = 12f;
    public float bodyguardSpawnY = -2.0f;

    [Header("Spawn BarbedWire X/Y")]
    public float barbedWireSpawnX = 12f;
    public float barbedWireSpawnY = -3.66f;

    [Header("Spawn Wall X/Y/Scale")]
    public float wallSpawnX = 12f;
    public float wallSpawnY = -3.0f; 
    public float wallScaleY1 = 1.5f;
    public float wallScaleY2 = 2.5f;

    [Header("Spawn CatFood X/Y")]
    public float catFoodSpawnX = 12f;
    public float catFoodSpawnY = -1.5f; // Spawn a bit higher so the cat can jump to it

    [Header("Random Time")]
    public float minDelay = 1.2f;
    public float maxDelay = 2.6f;

    [Header("Chances")]
    [Range(0f, 1f)]
    public float bagChance = 0.2f;
    [Range(0f, 1f)]
    public float bodyguardChance = 0.3f;
    [Range(0f, 1f)]
    public float barbedWireChance = 0.2f;
    [Range(0f, 1f)]
    public float wallChance = 0.2f;
    [Range(0f, 1f)]
    public float catFoodChance = 0.15f;

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

            // Decide which one to spawn based on chances AND level constraints
            float totalChance = bagChance;
            if (currentLevel >= 2) totalChance += bodyguardChance;
            if (currentLevel >= 3) totalChance += wallChance;
            if (currentLevel >= 4) totalChance += barbedWireChance;
            
            // CatFood always spawns across all levels
            totalChance += catFoodChance;

            float rnd = Random.Range(0f, totalChance);
            float currentLimit = bagChance;

            if (rnd < currentLimit)
            {
                if (obstaclePrefab != null)
                    Instantiate(obstaclePrefab, new Vector3(bagSpawnX, bagSpawnY, 0f), Quaternion.identity);
            }
            else
            {
                bool spawned = false;

                // Level 2+: Bodyguard
                if (currentLevel >= 2)
                {
                    currentLimit += bodyguardChance;
                    if (rnd < currentLimit && !spawned)
                    {
                        if (bodyguardPrefab != null)
                            Instantiate(bodyguardPrefab, new Vector3(bodyguardSpawnX, bodyguardSpawnY, 0f), Quaternion.identity);
                        spawned = true;
                    }
                }

                // Level 3+: Wall
                if (currentLevel >= 3 && !spawned)
                {
                    currentLimit += wallChance;
                    if (rnd < currentLimit)
                    {
                        if (wallPrefab != null)
                        {
                            float randomScaleY = Random.value < 0.5f ? wallScaleY1 : wallScaleY2;
                            GameObject wall = Instantiate(wallPrefab, new Vector3(wallSpawnX, wallSpawnY, 0f), Quaternion.identity);
                            wall.transform.localScale = new Vector3(wall.transform.localScale.x, randomScaleY, wall.transform.localScale.z);
                        }
                        spawned = true;
                    }
                }

                // Level 4+: Barbed Wire
                if (currentLevel >= 4 && !spawned)
                {
                    currentLimit += barbedWireChance;
                    if (rnd < currentLimit)
                    {
                        if (barbedWirePrefab != null)
                            Instantiate(barbedWirePrefab, new Vector3(barbedWireSpawnX, barbedWireSpawnY, 0f), Quaternion.identity);
                        spawned = true;
                    }
                }

                // Cat Food (Last chance if not spawned yet)
                if (!spawned && catFoodPrefab != null)
                {
                    Instantiate(catFoodPrefab, new Vector3(catFoodSpawnX, catFoodSpawnY, 0f), Quaternion.identity);
                }
            }
        }
    }
}