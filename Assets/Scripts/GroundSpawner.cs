using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// GroundSpawner: Modüler zemin parçalarını ve rastgele çukurları (Pit) uç uca ekleyerek sonsuz bir yol oluşturur.
/// Level 5 için idealdir.
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
    
    [Header("VFX Ayarları")]
    public GameObject pitVFXPrefab;       // Çukurların içinden çıkacak efekt (Ateş vb.)
    public float vfxYOffset = -1f;        // Efektin dikey konumu
    
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
            // Oyun hızını al (ObstacleMove ile aynı mantık)
            float speed = 7f; 
            float moveStep = speed * GameSpeed.Multiplier * Time.deltaTime;

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

            // En arkadaki parça belirli bir sınırı geçtiyse yenisini ekle
            if (activeGrounds.Count > 0 && activeGrounds[0].transform.position.x < spawnTriggerX)
            {
                SpawnNextSegment(true);
            }

            // Çok geride kalanları temizle (activeGrounds[0] her zaman en soldakidir)
            if (activeGrounds.Count > 0 && activeGrounds[0].transform.position.x < destroyLimitX)
            {
                RemoveOldestSegment();
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
    /// <param name="allowPit">Rastgele bir çukur çıkma şansı olsun mu?</param>
    public void SpawnNextSegment(bool allowPit)
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

        if (allowPit && currentLevel == 5 && Random.value < currentPitChance)
        {
            prefabToSpawn = groundPitPrefab;
            currentOffset = pitYOffset;
        }

        if (prefabToSpawn == null)
        {
            Debug.LogWarning("GroundSpawner: Spawn edilecek prefab atanmamış!");
            return;
        }

        // Objeyi oluştur ve listeye ekle
        GameObject segment = Instantiate(prefabToSpawn, new Vector3(nextSpawnX, groundY + currentOffset, 0), Quaternion.identity);
        
        // Eğer bir efekt atanmışsa ve bu bir çukursa çukurun içine oluştur
        if (prefabToSpawn == groundPitPrefab && pitVFXPrefab != null)
        {
            GameObject vfx = Instantiate(pitVFXPrefab, new Vector3(nextSpawnX, groundY + vfxYOffset, 0), Quaternion.identity);
            vfx.transform.SetParent(segment.transform); 
        }

        // Bu objeyi GroundSpawner'ın altında tutmak hiyerarşiyi düzenli yapar
        segment.transform.SetParent(transform);
        
        activeGrounds.Add(segment);

        // Bir sonraki parçanın konumunu güncelle
        nextSpawnX += groundWidth;
    }

    /// <summary>
    /// En arkada (solda) kalan eski zemin parçasını siler.
    /// </summary>
    void RemoveOldestSegment()
    {
        GameObject oldest = activeGrounds[0];
        activeGrounds.RemoveAt(0);
        Destroy(oldest);
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