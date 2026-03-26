using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// PitTrigger: Karakter çukura düştüğünde ölme ve yeniden başlama mantığını yönetir.
/// Hem 2D hem de 3D projelerde çalışacak şekilde esnek tasarlanmıştır.
/// </summary>
public class PitTrigger : MonoBehaviour
{
    [Header("Ayarlar")]
    public float restartDelay = 1.0f; // Yeniden başlamadan önceki bekleme süresi
    public Color gizmoColor = new Color(1, 0, 0, 0.5f); // Editördeki çukur rengi

    private bool isRestarting = false;

    // 2D Çarpışma Algılama
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !isRestarting)
        {
            HandlePlayerFall(collision.gameObject);
        }
    }

    // 3D Çarpışma Algılama (Esneklik için)
    private void OnTriggerEnter(Collider collision)
    {
        if (collision.CompareTag("Player") && !isRestarting)
        {
            HandlePlayerFall(collision.gameObject);
        }
    }

    private void HandlePlayerFall(GameObject player)
    {
        isRestarting = true;
        Debug.Log("Oyuncu çukura düştü!");

        // 1. Oyuncu Kontrolünü Dondur
        FreezePlayer(player);

        // 2. Varsa mevcut Die() fonksiyonunu tetikle (Projenin geri kalanıyla uyum için)
        // Eğer PlayerObstacleRules veya benzeri bir scriptte Die() varsa onu çağırır.
        player.SendMessage("Die", SendMessageOptions.DontRequireReceiver);

        // 3. Yeniden Başlatma Coroutine'ini çalıştır
        StartCoroutine(RestartSequence());
    }

    private void FreezePlayer(GameObject player)
    {
        // Rigidbody2D Kontrolü
        Rigidbody2D rb2d = player.GetComponent<Rigidbody2D>();
        if (rb2d != null)
        {
            rb2d.linearVelocity = Vector2.zero;
            rb2d.simulated = false; // Fizik motorundan çıkar
        }

        // Rigidbody (3D) Kontrolü
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        // CharacterController (3D) Kontrolü
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
        }

        // Varsa PlayerMovement scriptini durdur (dead = true yaparak)
        player.SendMessage("SetDead", true, SendMessageOptions.DontRequireReceiver);
    }

    private IEnumerator RestartSequence()
    {
        yield return new WaitForSeconds(restartDelay);
        
        // Mevcut sahneyi yeniden yükle
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    [ContextMenu("Fit Visual To Collider")]
    public void FitVisualToCollider()
    {
        BoxCollider2D box2d = GetComponent<BoxCollider2D>();
        if (box2d == null || transform.childCount == 0) return;

        foreach (Transform child in transform)
        {
            // Çocuğu collider'ın merkezine oturt
            child.localPosition = (Vector3)box2d.offset;
            // Çocuğun boyutunu collider boyutuna ayarla (Sprite Renderer'ın scale'ini kullanarak)
            child.localScale = new Vector3(box2d.size.x, box2d.size.y, 1f);
        }
    }

    // Editörde çukuru görselleştirme
    private void OnDrawGizmos()
    {
        BoxCollider2D box2d = GetComponent<BoxCollider2D>();
        BoxCollider box3d = GetComponent<BoxCollider>();

        Gizmos.color = gizmoColor;
        Gizmos.matrix = transform.localToWorldMatrix; // Önemli: Ölçeği ve rotasyonu hesaba kat

        if (box2d != null)
        {
            // Matrix kullandığımız için sadece offset ve size yeterlidir
            Gizmos.DrawCube((Vector3)box2d.offset, (Vector3)box2d.size);
        }
        else if (box3d != null)
        {
            Gizmos.DrawCube(box3d.center, box3d.size);
        }
    }
}
