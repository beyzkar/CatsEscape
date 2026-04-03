using UnityEngine;
using System.Collections.Generic;

// Sets the sprite, scale, position, and collider shape of an obstacle based on the current level theme
public class ObstacleThemeSetter : MonoBehaviour
{
    public enum AssetType { Obstacle, Wall, Enemy, LongWall, Bush }
    
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

        bool isObstacle = assetType == AssetType.Obstacle || gameObject.CompareTag("Obstacle");
        bool isWall = assetType == AssetType.Wall || gameObject.CompareTag("Wall");
        bool isEnemy = assetType == AssetType.Enemy || gameObject.CompareTag("Enemy");
        bool isLongWall = assetType == AssetType.LongWall || gameObject.CompareTag("LongWall");
        bool isBush = assetType == AssetType.Bush || gameObject.CompareTag("Bush");

        // Apply Visuals and Transform (Mainly for Obstacle/Wall)
        if (sr != null)
        {
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
        }

        // BoxCollider2D Setup (Smart/Manual fit)
        BoxCollider2D box = GetComponent<BoxCollider2D>() ?? GetComponentInChildren<BoxCollider2D>();
        if (box != null)
        {
            Vector2 targetSize = Vector2.zero;

            if (isWall && theme.wallColliderSize != Vector2.zero) {
                targetSize = theme.wallColliderSize;
            }
            else if (isObstacle && theme.obstacleColliderSize != Vector2.zero) {
                targetSize = theme.obstacleColliderSize;
            }
            else if (isEnemy && theme.enemyColliderSize != Vector2.zero) {
                targetSize = theme.enemyColliderSize;
            }
            else if (isLongWall && theme.longWallColliderSize != Vector2.zero) {
                targetSize = theme.longWallColliderSize;
            }
            else if (isBush && theme.bushColliderSize != Vector2.zero) {
                targetSize = theme.bushColliderSize;
            }

            if (targetSize != Vector2.zero)
            {
                box.size = targetSize;
            }
            else if (sr != null && sr.sprite != null)
            {
                // SMART AUTO-FIT
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
            
            // For Walls, BoxCollider is more reliable for stopping the player
            if (box != null && isWall) box.enabled = true;
            else if (box != null) box.enabled = false;
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
