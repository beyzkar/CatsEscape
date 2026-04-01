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
                        child.localScale = new Vector3(scale, scale, 1f);
                    }
                }
            }
        }
        
        AlignAllLayers();
        Debug.Log("Background fitted to camera height.");
    }

    [ContextMenu("Align All Layers")]
    public void AlignAllLayers()
    {
        if (Application.isPlaying || layers == null) return;

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
        if (PlayerMovement.Instance == null || GameSpeed.Multiplier <= 0) return;

        // Drive background movement strictly by player velocity
        float movement = PlayerMovement.Instance.CurrentVelocityX * GameSpeed.Multiplier * Time.deltaTime;

        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i].layerParent == null) continue;

            // Recalculate width if missing
            if (layers[i].width <= 0) 
            {
                if (layers[i].layerParent.childCount > 0)
                {
                    Transform firstChild = layers[i].layerParent.GetChild(0);
                    SpriteRenderer sr = firstChild.GetComponent<SpriteRenderer>();
                    if (sr != null && sr.sprite != null)
                        layers[i].width = sr.sprite.bounds.size.x * firstChild.localScale.x;
                }
                if (layers[i].width <= 0) continue;
            }

            if (layers[i].speed == 0) continue; 

            Vector3 pos = layers[i].layerParent.localPosition;
            pos.x -= movement * Mathf.Abs(layers[i].speed);

            // Seamless infinite loop handling for both directions
            if (movement > 0)
            {
                if (pos.x <= -layers[i].width) pos.x += layers[i].width;
            }
            else if (movement < 0)
            {
                if (pos.x >= 0) pos.x -= layers[i].width;
            }

            layers[i].layerParent.localPosition = pos;
        }
    }
}
