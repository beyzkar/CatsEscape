using UnityEngine;

public class FloatingXP : MonoBehaviour
{
    public float floatSpeed = 2f;
    public float fadeSpeed = 1.5f;
    public float lifetime = 1f;

    private SpriteRenderer sr;
    private Color color;

    void Start()
    {
        // Nesnenin kendisinde veya alt nesnelerinde SpriteRenderer ara
        sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) 
        {
            color = sr.color;
        }

        // Belirlenen süre sonunda nesneyi yok et
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        // Yukarı doğru hareket
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;

        // Yavaşça şeffaflaşma (fade out)
        if (sr != null)
        {
            color.a -= (1.0f / lifetime) * Time.deltaTime;
            sr.color = color;
        }
    }
}
