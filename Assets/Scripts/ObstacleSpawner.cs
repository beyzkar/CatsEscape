using System.Collections;
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject obstaclePrefab;   // ObstacleBag
    public GameObject bodyguardPrefab;  // BodyGuard
    public GameObject barbedWirePrefab; // BarbedWire
    public GameObject wallPrefab;       // Wall

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
    public float wallSpawnY = -3.0f; // Adjust this in Inspector to touch ground
    public float wallScaleY1 = 1.5f;
    public float wallScaleY2 = 2.5f;

    [Header("Random Time")]
    public float minDelay = 1.2f;
    public float maxDelay = 2.6f;

    [Header("Chances")]
    [Range(0f, 1f)]
    public float bodyguardChance = 0.3f;
    [Range(0f, 1f)]
    public float barbedWireChance = 0.2f;
    [Range(0f, 1f)]
    public float wallChance = 0.2f;

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

            // Decide which one to spawn based on chances
            float rnd = Random.value;

            if (rnd < bodyguardChance)
            {
                if (bodyguardPrefab != null)
                    Instantiate(bodyguardPrefab, new Vector3(bodyguardSpawnX, bodyguardSpawnY, 0f), Quaternion.identity);
                else
                    Debug.LogWarning("ObstacleSpawner: Bodyguard chosen but prefab is NULL!");
            }
            else if (rnd < (bodyguardChance + barbedWireChance))
            {
                if (barbedWirePrefab != null)
                    Instantiate(barbedWirePrefab, new Vector3(barbedWireSpawnX, barbedWireSpawnY, 0f), Quaternion.identity);
                else
                    Debug.LogWarning("ObstacleSpawner: BarbedWire chosen but prefab is NULL!");
            }
            else if (rnd < (bodyguardChance + barbedWireChance + wallChance))
            {
                if (wallPrefab != null)
                {
                    float randomScaleY = Random.value < 0.5f ? wallScaleY1 : wallScaleY2;
                    GameObject wall = Instantiate(wallPrefab, new Vector3(wallSpawnX, wallSpawnY, 0f), Quaternion.identity);
                    wall.transform.localScale = new Vector3(wall.transform.localScale.x, randomScaleY, wall.transform.localScale.z);
                }
                else
                    Debug.LogWarning("ObstacleSpawner: Wall chosen but prefab is NULL!");
            }
            else if (obstaclePrefab != null)
            {
                Instantiate(obstaclePrefab, new Vector3(bagSpawnX, bagSpawnY, 0f), Quaternion.identity);
            }
            else
            {
                Debug.LogWarning("ObstacleSpawner: Default Bag chosen but prefab is NULL!");
            }
        }
    }
}