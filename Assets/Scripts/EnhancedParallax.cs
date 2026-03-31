using UnityEngine;

public class EnhancedParallax : MonoBehaviour
{
    [System.Serializable]
    public struct ParallaxLayer
    {
        public Transform layerParent; // Katman objesi 
        public float speed;           
        [HideInInspector] public float width; 
    }

    public ParallaxLayer[] layers;
    public float baseSpeed = 5f;

    // Unity'de herhangi bir ayarı değiştirdiğinde veya sahnede bu objeye dokunduğunda otomatik hizalar
    void OnValidate()
    {
        // Editör modunda çalışması için
        AlignAllLayers();
        InitializeLayers();
    }

    [ContextMenu("Fit Height to Camera")]
    public void FitHeightToCamera()
    {
        Camera cam = Camera.main;
        if (cam == null || !cam.orthographic || layers == null) return;

        float targetHeight = cam.orthographicSize * 2f;

        foreach (var layer in layers)
        {
            if (layer.layerParent == null || layer.layerParent.childCount == 0) continue;

            for (int i = 0; i < layer.layerParent.childCount; i++)
            {
                Transform child = layer.layerParent.GetChild(i);
                SpriteRenderer sr = child.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null)
                {
                    float currentSpriteHeight = sr.sprite.bounds.size.y;
                    if (currentSpriteHeight > 0)
                    {
                        float scale = targetHeight / currentSpriteHeight;
                        // Uniform scale to prevent distortion
                        child.localScale = new Vector3(scale, scale, 1f);
                    }
                }
            }
        }
        
        // After scaling, we must realign
        AlignAllLayers();
        Debug.Log("Background fitted to camera height.");
    }

    // Unity'de script ismine sağ tıklayıp "Align All Layers" dersen resimleri otomatik uca ekler
    [ContextMenu("Align All Layers")]
    public void AlignAllLayers()
    {
        if (Application.isPlaying || layers == null) return; // Liste boşsa veya oyun açıksa yapma

        foreach (var layer in layers)
        {
            if (layer.layerParent == null || layer.layerParent.childCount < 2) continue;

            float currentX = 0;
            for (int i = 0; i < layer.layerParent.childCount; i++)
            {
                Transform child = layer.layerParent.GetChild(i);
                SpriteRenderer sr = child.GetComponent<SpriteRenderer>();
                
                child.localPosition = new Vector3(currentX, child.localPosition.y, child.localPosition.z);
                
                if (sr != null && sr.sprite != null)
                {
                    currentX += sr.sprite.bounds.size.x * child.localScale.x;
                }
            }
        }
    }

    void OnEnable()
    {
        InitializeLayers();
    }

    private PlayerMovement player;

    void Start()
    {
        InitializeLayers();
        player = FindFirstObjectByType<PlayerMovement>();
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

        int dir = (player != null) ? player.WorldDirection : 1;
        float movement = baseSpeed * GameSpeed.Multiplier * dir * Time.deltaTime;

        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i].layerParent == null) 
            {
                // Hangi objede hata olduğunu anlamak için log ekleyelim
                if (Time.frameCount % 300 == 0) // Çok sık doluşmasın diye
                    Debug.LogWarning($"EnhancedParallax on {gameObject.name}: Layer {i} is missing its Layer Parent!", this);
                continue;
            }

            if (layers[i].width <= 0) 
            {
                // Try to initialize on the fly if width is not set
                Transform firstChild = layers[i].layerParent.GetChild(0);
                SpriteRenderer sr = firstChild.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null)
                {
                    layers[i].width = sr.sprite.bounds.size.x * firstChild.localScale.x;
                }
                
                if (layers[i].width <= 0) continue;
            }

            if (layers[i].speed == 0) continue; 

            Vector3 pos = layers[i].layerParent.localPosition;
            pos.x -= movement * Mathf.Abs(layers[i].speed);

            // Loop logic needs to handle both directions
            if (dir > 0)
            {
                if (pos.x <= -layers[i].width) pos.x += layers[i].width;
            }
            else
            {
                if (pos.x >= 0) pos.x -= layers[i].width;
            }

            layers[i].layerParent.localPosition = pos;
        }
    }
}
