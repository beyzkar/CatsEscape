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

        // 1. BoxCollider2D Setup (Standard box fit)
        BoxCollider2D box = GetComponent<BoxCollider2D>() ?? GetComponentInChildren<BoxCollider2D>();
        if (box != null)
        {
            box.size = sr.sprite.bounds.size;
            box.offset = sr.sprite.bounds.center;
        }

        // 2. PolygonCollider2D Setup (Precise sprite fitting)
        PolygonCollider2D poly = GetComponent<PolygonCollider2D>() ?? GetComponentInChildren<PolygonCollider2D>();
        if (poly != null)
        {
            UpdatePolygonCollider(poly, sr.sprite);
            
            // If both exist, Polygon (precise) takes priority to prevent early collisions
            if (box != null) box.enabled = false;
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
