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
    public float spawnTriggerX = -10f;    // En arkadaki parça buradan sola geçtiğinde yenisini ekle
    public float destroyLimitX = -30f;    // Çok geride kalan parçaları silme sınırı

    [Header("Level Geçiş Ayarları")]
    public GameObject staticEnvironment;  // Level 1-2'deki sabit zeminleri tutan obje (Environment)
    public int startSpawnerLevel = 5;     // Spawner'ın ve çukurların aktif olacağı seviye
    
    [Header("Oyun Mantığı")]
    public int minSafeDistance = 3;       // Başlangıçta kaç parça güvenli (çukursuz) gelsin

    private List<GameObject> activeGrounds = new List<GameObject>();
    private float nextSpawnX = 0f;        // Bir sonraki parçanın ekleneceği X koordinatı
    private bool isSpawnerStarted = false;
    private int segmentsSinceLastPit = 0; // İki köprü arasındaki mesafeyi korumak için

    void Start()
    {
        // Başlangıçta seviye 3'ten küçükse hiçbir şey yapma
        CheckLevelAndInitialize();
    }

    void Update()
    {
        // Zaman donmuşsa (Örn: Game Over veya Pause) işlem yapma
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
            // Forward (Right)
            float rightLimit = (PlayerMovement.Instance.ScreenMaxX > 0) ? PlayerMovement.Instance.ScreenMaxX : PlayerMovement.Instance.maxX;
            if (activeGrounds.Count > 0 && activeGrounds[activeGrounds.Count - 1].transform.position.x < (rightLimit + groundWidth))
            {
                SpawnNextSegment(true, true);
            }

            // Backward (Left)
            if (activeGrounds.Count > 0 && activeGrounds[0].transform.position.x > (PlayerMovement.Instance.minX - groundWidth))
            {
                SpawnNextSegment(true, false);
            }

            // Cleanup: remove far away segments to save performance
            if (activeGrounds.Count > 0 && activeGrounds[0].transform.position.x < (PlayerMovement.Instance.minX - groundWidth * 3))
            {
                RemoveOldestSegment(0); // Left side
            }
            if (activeGrounds.Count > 1 && activeGrounds[activeGrounds.Count - 1].transform.position.x > (PlayerMovement.Instance.maxX + groundWidth * 3))
            {
                RemoveOldestSegment(activeGrounds.Count - 1); // Right side
            }
        }
    }

    [ContextMenu("Reset Y Offsets")]
    public void ResetOffsets()
    {
        groundY = -2.5f; // Başlangıç standart değeri
        normalYOffset = 0f;
        pitYOffset = 0f;
        Debug.Log("GroundSpawner: Y ofsetleri sıfırlandı. Köprü artık prefabdaki konumunda çıkmalı.");
    }

    void CheckLevelAndInitialize()
    {
        int currentLevel = LevelManager.Instance != null ? LevelManager.Instance.currentLevel : 1;

        // SEVİYE 5'TEN KÜÇÜKSE (Level 1, 2, 3, 4): Dinamik sistemi kapat, sabit sistemi aç
        if (currentLevel < 5)
        {
            isSpawnerStarted = false;
            
            // Sabit zemini (Hierarchy'deki Environment) görünür yap
            if (staticEnvironment != null && !staticEnvironment.activeInHierarchy)
            {
                staticEnvironment.SetActive(true);
            }

            // Oluşmuş dinamik parçalar varsa temizle (Çukurları sil)
            if (activeGrounds.Count > 0)
            {
                foreach (var g in activeGrounds) if (g != null) Destroy(g);
                activeGrounds.Clear();
                nextSpawnX = 0f;
            }
        }
        // SADECE LEVEL 5'TE: Dinamik sistemi başlat
        else
        {
            if (!isSpawnerStarted)
            {
                isSpawnerStarted = true;
                
                // Spawner başlarken zeminlerin kedinin biraz gerisinden başlamasını sağla
                nextSpawnX = -20f; 

                // Sabit zemini gizle
                if (staticEnvironment != null) staticEnvironment.SetActive(false);

                // İlk parçaları oluştur
                for (int i = 0; i < initialAmount; i++)
                {
                    SpawnNextSegment(i >= minSafeDistance);
                }
            }
        }
    }

    /// <summary>
    /// Yeni bir zemin segmanı oluşturur ve listeye ekler.
    /// </summary>
    public void SpawnNextSegment(bool allowPit, bool atRight = true)
    {
        GameObject prefabToSpawn = groundNormalPrefab;

        // LevelManager'dan mevcut seviyeyi kontrol et
        int currentLevel = 1;
        if (LevelManager.Instance != null)
        {
            currentLevel = LevelManager.Instance.currentLevel;
        }

        float currentOffset = normalYOffset;

        // Sadece Level 5'te ve şans yaver giderse çukur çıkar (Level 3-4'ten kaldırıldı)
        float currentPitChance = 0.2f;
        if (ObstacleSpawner.Instance != null)
        {
            currentPitChance = ObstacleSpawner.Instance.level5.pitChance;
        }

        // Köprüler arası mesafe kontrolü: En az 2 normal parça geçmeli
        int minDistanceBetweenPits = 2;
        if (allowPit && currentLevel == 5 && segmentsSinceLastPit >= minDistanceBetweenPits && Random.value < currentPitChance)
        {
            prefabToSpawn = groundPitPrefab;
            currentOffset = pitYOffset;
            segmentsSinceLastPit = 0; // Köprü oluştu, sayacı sıfırla
        }
        else
        {
            segmentsSinceLastPit++; // Normal parça oluştu, sayacı arttır
        }

        if (prefabToSpawn == null)
        {
            Debug.LogWarning("GroundSpawner: Spawn edilecek prefab atanmamış!");
            return;
        }

        // Determine spawn Position
        float spawnX = atRight ? nextSpawnX : (activeGrounds[0].transform.position.x - groundWidth);

        // Objeyi oluştur ve listeye ekle
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
            // nextSpawnX stays the same as it's the rightmost point
        }
    }

    /// <summary>
    /// Fazlalık zemin parçalarını siler.
    /// </summary>
    void RemoveOldestSegment(int index)
    {
        if (index < 0 || index >= activeGrounds.Count) return;
        
        GameObject target = activeGrounds[index];
        activeGrounds.RemoveAt(index);
        
        // If we removed the rightmost, we need to adjust nextSpawnX
        if (index == activeGrounds.Count) // After removal, previous Count is index+1
        {
            nextSpawnX -= groundWidth;
        }
        // If we remove the leftmost, we don't need to change nextSpawnX
        
        Destroy(target);
    }

    /// <summary>
    /// Verilen X pozisyonunda bir çukur olup olmadığını kontrol eder.
    /// </summary>
    public bool IsPitAtX(float xPosition)
    {
        float margin = groundWidth * 0.8f; // Larger margin for safety
        foreach (var ground in activeGrounds)
        {
            if (ground == null) continue;
            
            float dist = Mathf.Abs(ground.transform.position.x - xPosition);
            if (dist < margin)
            {
                // Identification: Check name, children name, or PitTrigger component
                bool isPit = ground.name.Contains("Pit") || 
                             ground.name.Contains("pit") || 
                             ground.GetComponentInChildren<PitTrigger>() != null ||
                             (ground.transform.Find("Pit") != null) ||
                             (ground.transform.Find("PitTrigger") != null);

                if (isPit)
                {
                    Debug.Log($"[GroundSpawner] Pit detected at X:{ground.transform.position.x} (dist:{dist} to checkX:{xPosition}). Blocking spawn.");
                    return true;
                }
            }
        }
        return false;
    }

    // Editörde diziliş mesafesini görmek için opsiyonel
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 start = new Vector3(nextSpawnX - groundWidth, groundY, 0);
        Vector3 end = new Vector3(nextSpawnX, groundY, 0);
        Gizmos.DrawLine(start, end);
        Gizmos.DrawWireSphere(end, 0.5f);
    }
}