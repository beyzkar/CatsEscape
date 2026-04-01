using UnityEngine;

// Moves obstacles and enemies in the game world relative to the player's speed
public class ObstacleMove : MonoBehaviour
{
    public float speed = 7f;
    public float destroyX = -15f;
    public bool moveEvenWhenMultiplierIsZero = false; // Logic for Enemy behavior
    [HideInInspector] public bool canRewardCleanJump = true;

    private bool passedPlayer = false;
    private Transform player;

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
        }
    }

    void Update()
    {
        // Calculate horizontal movement only
        float playerVel = (PlayerMovement.Instance != null) ? PlayerMovement.Instance.CurrentVelocityX : 0f;
        float independentSpeed = (moveEvenWhenMultiplierIsZero || CompareTag("Enemy")) ? speed : 0f;
        
        // Final move amount = player world velocity + internal autonomous speed
        float totalScrollSpeed = (playerVel + independentSpeed) * GameSpeed.Multiplier;
        transform.position += Vector3.left * totalScrollSpeed * Time.deltaTime;

        // Check if the obstacle has passed the player's X position to reward points
        if (!passedPlayer && player != null && transform.position.x < player.position.x)
        {
            passedPlayer = true;
            
            // Only reward if it's a valid obstacle and wasn't hit
            if (canRewardCleanJump && (CompareTag("Obstacle") || CompareTag("Wall") || CompareTag("LongWall") || CompareTag("Enemy") || CompareTag("Bush")))
            {
                // Level 5 uses GroundPoint.cs for progression; Levels 1-4 use this check
                if (LevelManager.Instance != null && LevelManager.Instance.currentLevel < 5)
                {
                    LevelManager.Instance.ObstaclePassed();
                }
            }
        }

        // Cleanup: destroy object when well off-screen
        if (transform.position.x < destroyX)
            Destroy(gameObject);
    }
}