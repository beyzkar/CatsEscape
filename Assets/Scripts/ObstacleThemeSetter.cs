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

        // Apply Visuals and Transform (Applying Y-offsets from LevelManager)
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
            else if (isEnemy)
            {
                transform.position += new Vector3(0, theme.enemyYOffset, 0);
            }
            else if (isLongWall)
            {
                transform.position += new Vector3(0, theme.longWallYOffset, 0);
            }
            else if (isBush)
            {
                transform.position += new Vector3(0, theme.bushYOffset, 0);
            }
        }

        // BoxCollider2D Setup (Strictly following LevelManager overrides)
        BoxCollider2D box = GetComponent<BoxCollider2D>() ?? GetComponentInChildren<BoxCollider2D>();
        if (box != null)
        {
            // Level 4 requirement: bush must behave as a solid obstacle from both sides.
            if (isBush && LevelManager.Instance != null && LevelManager.Instance.currentLevel == 4)
            {
                box.isTrigger = false;
            }

            Vector2 targetSize = Vector2.zero;
            Vector2 targetOffset = Vector2.zero;
            bool hasFieldOverride = false;

            if (isWall && theme.wallColliderSize != Vector2.zero) {
                targetSize = theme.wallColliderSize;
                targetOffset = theme.wallColliderOffset;
                hasFieldOverride = true;
            }
            else if (isObstacle && theme.obstacleColliderSize != Vector2.zero) {
                targetSize = theme.obstacleColliderSize;
                targetOffset = theme.obstacleColliderOffset;
                hasFieldOverride = true;
            }
            else if (isEnemy && theme.enemyColliderSize != Vector2.zero) {
                targetSize = theme.enemyColliderSize;
                targetOffset = theme.enemyColliderOffset;
                hasFieldOverride = true;
            }
            else if (isLongWall && theme.longWallColliderSize != Vector2.zero) {
                targetSize = theme.longWallColliderSize;
                targetOffset = theme.longWallColliderOffset;
                hasFieldOverride = true;
            }
            else if (isBush && theme.bushColliderSize != Vector2.zero) {
                targetSize = theme.bushColliderSize;
                targetOffset = theme.bushColliderOffset;
                hasFieldOverride = true;
            }

            if (hasFieldOverride)
            {
                box.size = targetSize;
                
                // If a manual offset is provided, use it.
                // Otherwise, automatically center the custom size on the sprite's physics bounds.
                if (targetOffset != Vector2.zero)
                {
                    box.offset = targetOffset;
                }
                else if (sr != null && sr.sprite != null)
                {
                    Bounds tightBounds = CalculateTightBounds(sr.sprite);
                    box.offset = tightBounds.center;
                }
            }
            else if (sr != null && sr.sprite != null)
            {
                // SMART AUTO-FIT total fallback (size + offset)
                Bounds tightBounds = CalculateTightBounds(sr.sprite);
                box.size = tightBounds.size;
                box.offset = tightBounds.center;
            }

            // 1.5. Apply level-specific physics material (Force 0 friction)
            if (box != null)
            {
                if (theme.wallPhysicsMaterial != null && isWall)
                {
                    box.sharedMaterial = theme.wallPhysicsMaterial;
                }
                else
                {
                    // Create private no-friction material if none provided to prevent sticking
                    PhysicsMaterial2D noFric = new PhysicsMaterial2D("NoFriction");
                    noFric.friction = 0f;
                    noFric.bounciness = 0f;
                    box.sharedMaterial = noFric;
                }
            }
        }

        // 2. PolygonCollider2D Setup
        PolygonCollider2D poly = GetComponent<PolygonCollider2D>() ?? GetComponentInChildren<PolygonCollider2D>();
        if (poly != null)
        {
            if (isWall)
            {
                // Keep wall collision simple and stable: one solid box collider only.
                poly.enabled = false;
                if (box != null)
                {
                    box.enabled = true;
                    box.isTrigger = false;

                    // Level 4: round box corners a little to reduce corner snagging.
                    if (LevelManager.Instance != null && LevelManager.Instance.currentLevel == 4)
                    {
                        float minAxis = Mathf.Min(box.size.x, box.size.y);
                        box.edgeRadius = Mathf.Clamp(minAxis * 0.02f, 0f, 0.08f);
                    }
                    else
                    {
                        box.edgeRadius = 0f;
                    }
                }
            }
            else
            {
                if (sr != null && sr.sprite != null)
                {
                    UpdatePolygonCollider(poly, sr.sprite);
                }
                poly.enabled = true;
                if (box != null) box.enabled = false;
            }
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
