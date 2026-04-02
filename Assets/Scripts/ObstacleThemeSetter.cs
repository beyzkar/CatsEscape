using UnityEngine;
using System.Collections.Generic;

// Sets the sprite, scale, position, and collider shape of an obstacle based on the current level theme
public class ObstacleThemeSetter : MonoBehaviour
{
    public enum AssetType { Obstacle, Wall }
    
    [Header("Settings")]
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

        SpriteRenderer sr = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
        if (sr == null) return;

        bool isObstacle = gameObject.CompareTag("Obstacle") || assetType == AssetType.Obstacle;
        bool isWall = gameObject.CompareTag("Wall") || assetType == AssetType.Wall;

        // Apply Visuals and Transform
        if (isObstacle)
        {
            if (theme.obstacleSprite != null) sr.sprite = theme.obstacleSprite;
            transform.localScale = theme.obstacleScale;
            transform.position += new Vector3(0, theme.obstacleYOffset, 0);
        }
        else if (isWall)
        {
            if (theme.wallSprite != null) sr.sprite = theme.wallSprite;
            transform.localScale = theme.wallScale;
            transform.position += new Vector3(0, theme.wallYOffset, 0);
        }

        if (sr.sprite == null) return;

        // 1. BoxCollider2D Setup (Smart/Manual fit)
        BoxCollider2D box = GetComponent<BoxCollider2D>() ?? GetComponentInChildren<BoxCollider2D>();
        if (box != null)
        {
            // Use manual overrides if provided in the theme for walls
            if (isWall && theme.wallColliderSize != Vector2.zero)
            {
                box.size = theme.wallColliderSize;
                box.offset = theme.wallColliderOffset;
            }
            else
            {
                // SMART AUTO-FIT: Calculate tight bounds based on sprite physics shape (not texture size)
                Bounds tightBounds = CalculateTightBounds(sr.sprite);
                box.size = tightBounds.size;
                box.offset = tightBounds.center;
            }

            // Apply level-specific physics material (friction/ice/bouncing etc.)
            if (isWall && theme.wallPhysicsMaterial != null)
            {
                box.sharedMaterial = theme.wallPhysicsMaterial;
            }
        }

        // 2. PolygonCollider2D Setup
        PolygonCollider2D poly = GetComponent<PolygonCollider2D>() ?? GetComponentInChildren<PolygonCollider2D>();
        if (poly != null)
        {
            UpdatePolygonCollider(poly, sr.sprite);
            if (box != null) box.enabled = false;
        }
    }

    private Bounds CalculateTightBounds(Sprite sprite)
    {
        if (sprite == null) return new Bounds(Vector3.zero, Vector3.zero);

        int shapeCount = sprite.GetPhysicsShapeCount();
        if (shapeCount == 0) return sprite.bounds; // Fallback

        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);
        List<Vector2> path = new List<Vector2>();

        for (int i = 0; i < shapeCount; i++)
        {
            sprite.GetPhysicsShape(i, path);
            foreach (Vector2 p in path)
            {
                if (p.x < min.x) min.x = p.x;
                if (p.y < min.y) min.y = p.y;
                if (p.x > max.x) max.x = p.x;
                if (p.y > max.y) max.y = p.y;
            }
        }

        Vector2 size = max - min;
        Vector2 center = min + (size / 2f);
        return new Bounds(center, size);
    }
    }

    private void UpdatePolygonCollider(PolygonCollider2D poly, Sprite sprite)
    {
        int shapeCount = sprite.GetPhysicsShapeCount();
        poly.pathCount = shapeCount;
        
        List<Vector2> path = new List<Vector2>();
        for (int i = 0; i < shapeCount; i++)
        {
            sprite.GetPhysicsShape(i, path);
            poly.SetPath(i, path.ToArray());
        }
    }
}
