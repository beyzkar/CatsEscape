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

        // Apply specific Y offset from theme
        if (gameObject.CompareTag("Obstacle") || assetType == AssetType.Obstacle)
        {
            transform.position += new Vector3(0, theme.obstacleYOffset, 0);
        }
        else if (gameObject.CompareTag("Wall") || assetType == AssetType.Wall)
        {
            transform.position += new Vector3(0, theme.wallYOffset, 0);
        }

        // 1. BoxCollider2D varsa (Kutu şeklinde basit fit)
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box == null) box = GetComponentInChildren<BoxCollider2D>();
        
        if (box != null && sr.sprite != null)
        {
            box.size = sr.sprite.bounds.size;
            box.offset = sr.sprite.bounds.center;
        }

        // 2. PolygonCollider2D varsa (Görselin tam şeklini sarması için en iyisi)
        PolygonCollider2D poly = GetComponent<PolygonCollider2D>();
        if (poly == null) poly = GetComponentInChildren<PolygonCollider2D>();

        if (poly != null && sr.sprite != null)
        {
            UpdatePolygonCollider(poly, sr.sprite);
            
            // Eğer hem Box hem Polygon varsa, Polygon (hassas olan) önceliklidir.
            // BoxCollider'ı devre dışı bırakıyoruz ki kedi uzaktan çarpmasın.
            if (box != null) box.enabled = false;
        }
    }

    private void UpdatePolygonCollider(PolygonCollider2D poly, Sprite sprite)
    {
        int shapeCount = sprite.GetPhysicsShapeCount();
        poly.pathCount = shapeCount;
        
        System.Collections.Generic.List<Vector2> path = new System.Collections.Generic.List<Vector2>();
        for (int i = 0; i < shapeCount; i++)
        {
            sprite.GetPhysicsShape(i, path);
            poly.SetPath(i, path.ToArray());
        }
    }
}
