using System.Collections;
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject obstaclePrefab;   // ObstacleBag
    public GameObject bodyguardPrefab;  // BodyGuard

    [Header("Spawn Bag (Obstacle) X/Y")]
    public float bagSpawnX = 12f;
    public float bagSpawnY = -2.3f;

    [Header("Spawn Bodyguard X/Y")]
    public float bodyguardSpawnX = 12f;
    public float bodyguardSpawnY = -2.0f;

    [Header("Random Time")]
    public float minDelay = 1.2f;
    public float maxDelay = 2.6f;

    [Header("Chances")]
    [Range(0f, 1f)]
    public float bodyguardChance = 0.3f;

    void Start()
    {
        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minDelay, maxDelay));

            // Decide which one to spawn (picking only one per interval)
            bool spawnBodyguard = Random.value < bodyguardChance;

            if (spawnBodyguard && bodyguardPrefab != null)
            {
                Instantiate(bodyguardPrefab, new Vector3(bodyguardSpawnX, bodyguardSpawnY, 0f), Quaternion.identity);
            }
            else if (!spawnBodyguard && obstaclePrefab != null)
            {
                Instantiate(obstaclePrefab, new Vector3(bagSpawnX, bagSpawnY, 0f), Quaternion.identity);
            }
        }
    }
}