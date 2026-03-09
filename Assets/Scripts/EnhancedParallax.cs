using UnityEngine;

public class EnhancedParallax : MonoBehaviour
{
    [System.Serializable]
    public struct ParallaxLayer
    {
        public Transform layerParent; // Katman objesi (Grup_L1 gibi)
        public float speed;           // 0-1 arası hız
        [HideInInspector] public float width; // Otomatik hesaplanacak
    }

    public ParallaxLayer[] layers;
    public float baseSpeed = 5f;

    // Unity'de herhangi bir ayarı değiştirdiğinde veya sahnede bu objeye dokunduğunda otomatik hizalar
    void OnValidate()
    {
        // Editör modunda çalışması için
        AlignAllLayers();
    }

    // Unity'de script ismine sağ tıklayıp "Align All Layers" dersen resimleri otomatik uca ekler
    [ContextMenu("Align All Layers")]
    public void AlignAllLayers()
    {
        if (Application.isPlaying || layers == null) return; // Liste boşsa veya oyun açıksa yapma

        foreach (var layer in layers)
        {
            if (layer.layerParent == null || layer.layerParent.childCount < 2) continue;

            Transform firstChild = layer.layerParent.GetChild(0);
            Transform secondChild = layer.layerParent.GetChild(1);

            SpriteRenderer sr = firstChild.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                float spriteWidth = sr.sprite.bounds.size.x * firstChild.localScale.x;
                
                // Sadece X pozisyonunu değiştiriyoruz, Y ve Z (yükseklik/derinlik) korunuyor
                firstChild.localPosition = new Vector3(0, firstChild.localPosition.y, firstChild.localPosition.z);
                secondChild.localPosition = new Vector3(spriteWidth, secondChild.localPosition.y, secondChild.localPosition.z);
            }
        }
    }

    void OnEnable()
    {
        InitializeLayers();
    }

    void Start()
    {
        InitializeLayers();
    }

    [ContextMenu("Initialize Layers")]
    public void InitializeLayers()
    {
        if (layers == null) return;

        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i].layerParent != null && layers[i].layerParent.childCount > 0)
            {
                Transform firstChild = layers[i].layerParent.GetChild(0);
                SpriteRenderer sr = firstChild.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null)
                {
                    layers[i].width = sr.sprite.bounds.size.x * firstChild.localScale.x;
                }
            }
        }
    }

    void Update()
    {
        if (GameSpeed.Multiplier <= 0) return;

        float movement = baseSpeed * GameSpeed.Multiplier * Time.deltaTime;

        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i].layerParent == null) 
            {
                // Hangi objede hata olduğunu anlamak için log ekleyelim
                if (Time.frameCount % 300 == 0) // Çok sık doluşmasın diye
                    Debug.LogWarning($"EnhancedParallax on {gameObject.name}: Layer {i} is missing its Layer Parent!", this);
                continue;
            }

            if (layers[i].width <= 0) continue;

            Vector3 pos = layers[i].layerParent.localPosition;
            pos.x -= movement * layers[i].speed;

            if (pos.x <= -layers[i].width)
            {
                pos.x += layers[i].width;
            }

            layers[i].layerParent.localPosition = pos;
        }
    }
}
