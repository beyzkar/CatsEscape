using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// GroundSpawner: Modüler zemin parçalarını ve rastgele çukurları (Pit) uç uca ekleyerek sonsuz bir yol oluşturur.
/// </summary>
public class GroundSpawner : MonoBehaviour
{
    [Header("Prefab Ayarları")]
    public GameObject groundNormalPrefab; // Standart düz zemin parçası
    public float normalYOffset = 0f;      // Düz zemin için ek yükseklik ayarı
    public GameObject groundPitPrefab;    // İçinde boşluk ve PitTrigger olan zemin parçası
    public float pitYOffset = 0f;         // Çukurlu zemin için ek yükseklik ayarı
    
    public static GroundSpawner Instance { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
    }
    
    [Header("Düzenleme Ayarları")]
    public float groundWidth = 12f;       // Her bir zemin prefabının genişliği (Unity birimi cinsinden)
    public float groundY = -2.5f;         // Zeminlerin oluşacağı dikey konum
    public int initialAmount = 6;         // Oyun başında kaç parça hazır dursun

    [Header("Level Geçiş Ayarları")]
    public GameObject staticEnvironment;  // Level 1-2'deki sabit zeminleri tutan obje (Environment)
    
    [Header("Oyun Mantığı")]
    public int minSafeDistance = 3;       // Başlangıçta kaç parça güvenli (çukursuz) gelsin

    private List<GameObject> activeGrounds = new List<GameObject>();
    private float nextSpawnX = 0f;        // Bir sonraki parçanın ekleneceği X koordinatı
    private bool isSpawnerStarted = false;
    private int segmentsSinceLastPit = 0; // İki köprü arasındaki mesafeyi korumak için

    void Start()
    {
        CheckLevelAndInitialize();
    }

    void Update()
    {
        if (Time.timeScale <= 0f) return;

        CheckLevelAndInitialize();

        if (isSpawnerStarted)
        {
            // Match world movement to player velocity
            float moveStep = PlayerMovement.Instance.CurrentVelocityX * GameSpeed.Multiplier * Time.deltaTime;

            // Tüm aktif zeminleri hareket ettir
            for (int i = 0; i < activeGrounds.Count; i++)
            {
                if (activeGrounds[i] != null)
                {
                    activeGrounds[i].transform.Translate(Vector3.left * moveStep);
                }
            }

            // Bir sonraki parçanın ekleneceği konumu da aynı oranda kaydır
            nextSpawnX -= moveStep;

            // Spawning: Handle both directions
            float rightLimit = (PlayerMovement.Instance.ScreenMaxX > 0) ? PlayerMovement.Instance.ScreenMaxX : PlayerMovement.Instance.maxX;
            
            // Forward (Right side)
            if (activeGrounds.Count > 0 && activeGrounds[activeGrounds.Count - 1].transform.position.x < (rightLimit + groundWidth))
            {
                SpawnNextSegment(true, true);
            }

            // Backward (Left side)
            if (activeGrounds.Count > 0 && activeGrounds[0].transform.position.x > (PlayerMovement.Instance.minX - groundWidth))
            {
                SpawnNextSegment(true, false);
            }

            // Cleanup: remove far away segments
            if (activeGrounds.Count > 0 && activeGrounds[0].transform.position.x < (PlayerMovement.Instance.minX - groundWidth * 3))
            {
                RemoveOldestSegment(0);
            }
            if (activeGrounds.Count > 1 && activeGrounds[activeGrounds.Count - 1].transform.position.x > (PlayerMovement.Instance.maxX + groundWidth * 3))
            {
                RemoveOldestSegment(activeGrounds.Count - 1);
            }
        }
    }

    [ContextMenu("Reset Y Offsets")]
    public void ResetOffsets()
    {
        groundY = -2.5f;
        normalYOffset = 0f;
        pitYOffset = 0f;
        Debug.Log("GroundSpawner: Y ofsetleri sıfırlandı.");
    }

    void CheckLevelAndInitialize()
    {
        int currentLevel = LevelManager.Instance != null ? LevelManager.Instance.currentLevel : 1;

        if (currentLevel < 5)
        {
            isSpawnerStarted = false;
            
            if (staticEnvironment != null && !staticEnvironment.activeInHierarchy)
            {
                staticEnvironment.SetActive(true);
            }

            if (activeGrounds.Count > 0)
            {
                foreach (var g in activeGrounds) if (g != null) Destroy(g);
                activeGrounds.Clear();
                nextSpawnX = 0f;
            }
        }
        else
        {
            if (!isSpawnerStarted)
            {
                isSpawnerStarted = true;
                nextSpawnX = -20f; 

                if (staticEnvironment != null) staticEnvironment.SetActive(false);

                for (int i = 0; i < initialAmount; i++)
                {
                    SpawnNextSegment(i >= minSafeDistance);
                }
            }
        }
    }

    public void SpawnNextSegment(bool allowPit, bool atRight = true)
    {
        GameObject prefabToSpawn = groundNormalPrefab;
        int currentLevel = (LevelManager.Instance != null) ? LevelManager.Instance.currentLevel : 1;
        float currentOffset = normalYOffset;

        float currentPitChance = 0.2f;
        if (ObstacleSpawner.Instance != null)
        {
            currentPitChance = ObstacleSpawner.Instance.level5.pitChance;
        }

        int minDistanceBetweenPits = 2;
        if (allowPit && currentLevel == 5 && segmentsSinceLastPit >= minDistanceBetweenPits && Random.value < currentPitChance)
        {
            prefabToSpawn = groundPitPrefab;
            currentOffset = pitYOffset;
            segmentsSinceLastPit = 0;
        }
        else
        {
            segmentsSinceLastPit++;
        }

        if (prefabToSpawn == null) return;

        float spawnX = atRight ? nextSpawnX : (activeGrounds[0].transform.position.x - groundWidth);

        GameObject segment = Instantiate(prefabToSpawn, new Vector3(spawnX, groundY + currentOffset, 0), Quaternion.identity);
        segment.transform.SetParent(transform);
        
        if (atRight)
        {
            activeGrounds.Add(segment);
            nextSpawnX += groundWidth;
        }
        else
        {
            activeGrounds.Insert(0, segment);
        }
    }

    void RemoveOldestSegment(int index)
    {
        if (index < 0 || index >= activeGrounds.Count) return;
        
        GameObject target = activeGrounds[index];
        activeGrounds.RemoveAt(index);
        
        if (index == activeGrounds.Count) 
        {
            nextSpawnX -= groundWidth;
        }
        
        Destroy(target);
    }

    public bool IsPitAtX(float xPosition)
    {
        float margin = groundWidth * 0.8f;
        foreach (var ground in activeGrounds)
        {
            if (ground == null) continue;
            
            float dist = Mathf.Abs(ground.transform.position.x - xPosition);
            if (dist < margin)
            {
                bool isPit = ground.name.Contains("Pit") || 
                             ground.name.Contains("pit") || 
                             ground.GetComponentInChildren<PitTrigger>() != null ||
                             (ground.transform.Find("Pit") != null) ||
                             (ground.transform.Find("PitTrigger") != null);

                if (isPit) return true;
            }
        }
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 start = new Vector3(nextSpawnX - groundWidth, groundY, 0);
        Vector3 end = new Vector3(nextSpawnX, groundY, 0);
        Gizmos.DrawLine(start, end);
        Gizmos.DrawWireSphere(end, 0.5f);
    }
}