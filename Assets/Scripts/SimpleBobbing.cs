using UnityEngine;

/// <summary>
/// SimpleBobbing: Atandığı objeyi basit bir sinüs dalgasıyla yukarı-aşağı hareket ettirir.
/// Su, yüzen objeler veya parlayan itemlar için idealdir.
/// </summary>
public class SimpleBobbing : MonoBehaviour
{
    [Header("Hareket Ayarları")]
    public float amplitude = 0.1f;    // Ne kadar yükseğe çıkacak
    public float speed = 2f;        // Ne kadar hızlı sallanacak
    
    private Vector3 startPos;

    void Start()
    {
        startPos = transform.localPosition;
    }

    void Update()
    {
        // Sinüs dalgası kullanarak yeni Y pozisyonunu hesapla
        float newY = startPos.y + Mathf.Sin(Time.time * speed) * amplitude;
        transform.localPosition = new Vector3(startPos.x, newY, startPos.z);
    }
}
