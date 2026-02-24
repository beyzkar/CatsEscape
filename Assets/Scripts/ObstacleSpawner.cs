using System.Collections;
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject obstaclePrefab;

    [Header("Spawn X/Y")]
    public float spawnX = 12f;
    public float spawnY = -2.3f; 

    [Header("Random Time")]
    public float minDelay = 1.2f;
    public float maxDelay = 2.6f;

    void Start()
    {
        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            float wait = Random.Range(minDelay, maxDelay);
            yield return new WaitForSeconds(wait);

            Vector3 pos = new Vector3(spawnX, spawnY, 0f);
            Instantiate(obstaclePrefab, pos, Quaternion.identity);
        }
    }
}