using UnityEngine;

public class ObstacleThemeSetter : MonoBehaviour
{
    public enum AssetType { Obstacle, Wall }
    public AssetType assetType;

    private void Start()
    {
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        if (LevelManager.Instance == null) return;

        LevelManager.ThemeAssets theme = LevelManager.Instance.GetCurrentTheme();
        if (theme == null) return;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();

        if (sr == null) return;

        // Use Tag if possible, otherwise use the assetType field
        if (gameObject.CompareTag("Obstacle") || assetType == AssetType.Obstacle)
        {
            if (theme.obstacleSprite != null)
                sr.sprite = theme.obstacleSprite;
            
            // Apply scale from theme (User manages this in Unity Inspector)
            transform.localScale = theme.obstacleScale;
        }
        else if (gameObject.CompareTag("Wall") || assetType == AssetType.Wall)
        {
            if (theme.wallSprite != null)
                sr.sprite = theme.wallSprite;
            
            transform.localScale = theme.wallScale;
        }

        // Apply Y offset from theme
        transform.position += new Vector3(0, theme.yOffset, 0);
    }
}
