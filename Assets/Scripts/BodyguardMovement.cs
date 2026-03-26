using UnityEngine;

/// <summary>
/// BodyguardMovement: Oyunun geri kalan hızından (GameSpeed.Multiplier) bağımsız olarak 
/// sürekli hareket eden Bodyguard engeli için özel hareket scripti.
/// </summary>
public class BodyguardMovement : MonoBehaviour
{
    [Header("Hareket Ayarları")]
    public float speed = 6f;          // Bodyguard'ın kendi hızı
    public float destroyX = -15f;     // Ekrandan çıkınca silineceği nokta

    void Update()
    {
        // 1. Sürekli Hareket: 
        // GameSpeed.Multiplier kullanmadığımız için kedi bir engele çarpıp dursa bile 
        // Bodyguard objesi kendi hızıyla ilerlemeye devam eder.
        transform.position += Vector3.left * speed * Time.deltaTime;

        // 2. Temizlik:
        // Belirlenen sınırı geçtiğinde objeyi sahneden siler.
        if (transform.position.x < destroyX)
        {
            Destroy(gameObject);
        }
    }
}
