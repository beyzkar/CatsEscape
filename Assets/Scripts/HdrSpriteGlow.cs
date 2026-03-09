using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class HdrSpriteGlow : MonoBehaviour
{
    [ColorUsage(true, true)]
    public Color glowColor = new Color(2f, 2f, 2f, 1f);

    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        // Applying HDR color at runtime
        spriteRenderer.color = glowColor;
    }
}
