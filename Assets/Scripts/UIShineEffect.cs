using UnityEngine;
using TMPro;

[ExecuteAlways]
[RequireComponent(typeof(TextMeshProUGUI))]
public class UIShineEffect : MonoBehaviour
{
    [Header("Glow Settings (Parlayan Işık Ayarları)")]
    public Color glowColor = new Color(1f, 1f, 0f, 1f); // Neon Sarı/Beyaz
    [Range(0f, 1f)] public float minIntensity = 0.2f;
    [Range(0f, 1f)] public float maxIntensity = 1.0f;
    public float pulseSpeed = 2.5f;

    [Header("Glow Appearance (Görünüm)")]
    [Range(0f, 1f)] public float glowSize = 0.7f; // Dışa doğru genişlik
    [Range(0f, 1f)] public float glowSoftness = 0.6f; // Yumuşaklık/Bulanıklık

    private TextMeshProUGUI textComponent;
    private Material internalMaterial;

    private void OnEnable()
    {
        Initialize();
    }

    private void Initialize()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
        
        if (textComponent != null)
        {
            // Create a unique material instance so we don't affect other text objects
            textComponent.fontMaterial = new Material(textComponent.fontMaterial);
            internalMaterial = textComponent.fontMaterial;
            
            // Enable the Underlay feature in the TMP shader
            internalMaterial.EnableKeyword("UNDERLAY_ON");
        }
    }

    private void Update()
    {
        if (textComponent == null) 
        {
            textComponent = GetComponent<TextMeshProUGUI>();
            return;
        }

        if (internalMaterial == null)
        {
            Initialize();
            if (internalMaterial == null) return;
        }

        // 1. Calculate Pulse (Nefes Alma)
        float time = Application.isPlaying ? Time.unscaledTime : Time.realtimeSinceStartup;
        float t = (Mathf.Sin(time * pulseSpeed) + 1f) / 2f;
        float currentAlpha = Mathf.Lerp(minIntensity, maxIntensity, t) * glowColor.a;

        // 2. Apply to Underlay Material Properties
        Color combinedColor = new Color(glowColor.r, glowColor.g, glowColor.b, currentAlpha);
        
        // Use TMP's internal property IDs
        internalMaterial.SetColor(ShaderUtilities.ID_UnderlayColor, combinedColor);
        internalMaterial.SetFloat(ShaderUtilities.ID_UnderlayDilate, glowSize);
        internalMaterial.SetFloat(ShaderUtilities.ID_UnderlaySoftness, glowSoftness);
        
        // Centered glow (no offset)
        internalMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0);
        internalMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, 0);
    }
}
