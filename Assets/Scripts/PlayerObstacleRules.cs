using UnityEngine;

public class PlayerObstacleRules : MonoBehaviour
{
    [Header("Side hit freeze")]
    public float topNormalThreshold = 0.4f;
    public float sideNormalThreshold = 0.6f;

    [Header("Hearts (Wall hits)")]
    public GameObject[] heartUI;
    public int initialHearts = 3;
    private int currentHearts;
    private int maxHearts;
    private bool hasTakenDamage = false;
    private GameObject lastHitWall;

    // local AudioClips have been removed for global control in AudioManager

    [Header("Death kick")]
    public float deathKickX = -8f;
    public float deathKickY = 6f;
    public float deathGravity = 6f;

    private bool stuck = false;
    private bool dead = false;

    private Rigidbody2D rb;
    private PlayerMovement movementScript;
    private Animator animator;
    private UnityEngine.Video.VideoPlayer bgVideo;

    private float jumpGraceTimer = 0f;
    private const float JUMP_GRACE_TIME = 0.5f; 

    private float startX;
    [Header("Return Speed")]
    public float returnSpeed = 2f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        movementScript = GetComponent<PlayerMovement>();
        animator = GetComponentInChildren<Animator>();

        bgVideo = Object.FindFirstObjectByType<UnityEngine.Video.VideoPlayer>();

        startX = transform.position.x;

        if (heartUI != null && heartUI.Length > 0)
        {
            // Start with initial amount (e.g., 3)
            currentHearts = initialHearts;
            maxHearts = initialHearts;

            // Hide all hearts initially beyond the starting ones
            for (int i = 0; i < heartUI.Length; i++)
            {
                if (heartUI[i] != null)
                {
                    heartUI[i].SetActive(i < initialHearts);
                }
            }
        }
    }

    void Update()
    {
        if (dead) return;

        if (jumpGraceTimer > 0)
            jumpGraceTimer -= Time.deltaTime;

        // RETURN TO HOME POSITION
        if (!stuck && !dead && transform.position.x < startX)
        {
            float newX = Mathf.MoveTowards(transform.position.x, startX, returnSpeed * Time.deltaTime);
            transform.position = new Vector3(newX, transform.position.y, transform.position.z);
        }

        if (stuck && (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.UpArrow)))
        {
            stuck = false;
            if (animator != null) animator.speed = 1f; // Resumes animation
            // DO NOT set lastHitWall = null here; keeping it prevents re-damage from the same object
            GameSpeed.Multiplier = 1f;
            jumpGraceTimer = JUMP_GRACE_TIME;

            // Unlock physics immediately
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            }

            // Explicitly trigger the jump to guarantee escape
            if (movementScript != null)
            {
                movementScript.ResetJumps();
                movementScript.TryJump();
            }

            // Nudge back slightly to prevent overlapping the wall collider
            transform.position += Vector3.left * 0.2f;

            if (bgVideo != null) bgVideo.Play();
        }
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        if (dead) return;

        // Bodyguard or BarbedWire: Heart removal + Stop (used to be instant death)
        if (col.collider.CompareTag("Bodyguard") || col.collider.CompareTag("BarbedWire"))
        {
            // Ignore if we are in jump grace and hitting the SAME object
            if (jumpGraceTimer > 0 && col.gameObject == lastHitWall) return;

            if (col.gameObject != lastHitWall && !stuck)
            {
                lastHitWall = col.gameObject;
                LoseHeart();
                
                // Reset clean jumps combo on hit
                if (ScoreManager.Instance != null) ScoreManager.Instance.ResetCleanJumps();

                // Disable reward for the obstacle we hit
                ObstacleMove moveScript = col.gameObject.GetComponent<ObstacleMove>();
                if (moveScript != null) moveScript.canRewardCleanJump = false;

                if (!dead)
                {
                    stuck = true;
                    if (animator != null) animator.speed = 0f; // Pauses animation
                    GameSpeed.Multiplier = 0f;

                    // Grant a jump to ensure player can escape
                    if (movementScript != null) movementScript.ResetJumps();

                    // LOCK POSITION (Kinematic) + small immediate nudge to stay on surface
                    if (rb != null)
                    {
                        rb.linearVelocity = Vector2.zero;
                        rb.bodyType = RigidbodyType2D.Kinematic;
                    }

                    transform.position += Vector3.left * 0.3f;

                    if (bgVideo != null) bgVideo.Pause();
                }
            }
            return;
        }

        // Obstacle (Bag) or Wall logic
        if (!col.collider.CompareTag("Obstacle") && !col.collider.CompareTag("Wall")) return;

        bool hitTop = false;

        // Check all contact points for better reliability
        for (int i = 0; i < col.contactCount; i++)
        {
            Vector2 n = col.GetContact(i).normal;

            // Player lands on top -> normal points up
            if (n.y > topNormalThreshold)
            {
                hitTop = true;
                break;
            }
        }

        // ÜSTTEN temas: ses + kutuyu yok et (Sadece Bag/Obstacle için) + 20 XP
        if (hitTop)
        {
            if (col.collider.CompareTag("Obstacle"))
            {
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlayCrush();

                // Add 20 XP for crushing obstacle bag
                if (ScoreManager.Instance != null) ScoreManager.Instance.AddXP(20);

                Destroy(col.gameObject);
            }
            return;
        }

        // YANDAN veya ALT'TAN temas: STUCK and STOP (Wall or Obstacle)
        if (!hitTop && !stuck)
        {
            // Ignore if we are in jump grace and hitting the SAME object
            if (jumpGraceTimer > 0 && col.gameObject == lastHitWall) return;

            // Reset clean jumps combo on hit/stuck
            if (ScoreManager.Instance != null) ScoreManager.Instance.ResetCleanJumps();

            // Disable reward for the obstacle we hit
            ObstacleMove moveScript = col.gameObject.GetComponent<ObstacleMove>();
            if (moveScript != null) moveScript.canRewardCleanJump = false;

            // Wall and Obstacle (Bag) now ONLY stop the player without losing hearts
            if (col.collider.CompareTag("Wall") || col.collider.CompareTag("Obstacle"))
            {
                stuck = true;
                if (animator != null) animator.speed = 0f; // Pauses animation
                GameSpeed.Multiplier = 0f;

                // Play hit wall sound
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlayHitWall();

                // Grant a jump
                if (movementScript != null) movementScript.ResetJumps();

                // LOCK POSITION (Kinematic)
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                    rb.bodyType = RigidbodyType2D.Kinematic;
                }

                transform.position += Vector3.left * 0.3f;

                if (bgVideo != null) bgVideo.Pause();
            }
        }
    }

    private void LoseHeart()
    {
        if (dead) return;

        hasTakenDamage = true; // Mark as damaged for CatFood logic

        // If currentHearts is 3, first hit makes it 2 and hides heartUI[2]
        // If currentHearts is 1, third hit makes it 0 and hides heartUI[0]
        // If currentHearts is 0, fourth hit makes it -1 and triggers Die()
        currentHearts--;
        
        Debug.Log("Heart lost! Remaining CurrentHearts value: " + currentHearts);

        // Turn off hearts (uses index from 0 to Length-1)
        if (heartUI != null && currentHearts >= 0 && currentHearts < heartUI.Length)
        {
            if (heartUI[currentHearts] != null)
                heartUI[currentHearts].SetActive(false);
        }

        // Play Cat Voice sound
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayHeartLost();

        // ONLY DIE ON THE 4TH HIT (when currentHearts becomes -1)
        if (currentHearts < 0)
        {
            Die();
        }
    }

    public void CollectCatFood()
    {
        if (dead) return;

        // 1. If never taken damage and max hearts < 9, increase capacity
        if (!hasTakenDamage && maxHearts < 9 && heartUI != null && maxHearts < heartUI.Length)
        {
            maxHearts++;
            currentHearts = maxHearts;
            
            // Show the new heart in UI
            if (heartUI[currentHearts - 1] != null)
                heartUI[currentHearts - 1].SetActive(true);
            
            Debug.Log("Max Hearts increased to: " + maxHearts);
        }
        // 2. If damaged, heal one heart (up to current maxHearts)
        else if (currentHearts < maxHearts)
        {
            currentHearts++;
            
            // Show the restored heart in UI
            if (heartUI != null && currentHearts > 0 && currentHearts <= heartUI.Length)
            {
                if (heartUI[currentHearts - 1] != null)
                    heartUI[currentHearts - 1].SetActive(true);
            }
            
            Debug.Log("Healed! Current Hearts: " + currentHearts);
        }

        // Play heart fill sound
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayHeartFill();

        // Add 50 XP for collecting Cat Food
        if (ScoreManager.Instance != null) ScoreManager.Instance.AddXP(50);
    }

    private void OnCollisionStay2D(Collision2D col)
    {
        if (dead) return;
        // No longer instant death
    }

    private void Die()
    {
        dead = true;
        stuck = false;

        GameSpeed.Multiplier = 0f;
        if (bgVideo != null) bgVideo.Pause();

        // Stop background music and play game over sound
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopBackgroundMusic();
            AudioManager.Instance.PlayGameOverSound();
        }

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayGameOverSFX();

        if (movementScript != null)
            movementScript.enabled = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = deathGravity;
            rb.AddForce(new Vector2(deathKickX, deathKickY), ForceMode2D.Impulse);
        }

        if (GameOverManager.Instance != null)
            GameOverManager.Instance.ShowGameOver();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (dead) return;

        // Ensure Bodyguard or BarbedWire causes heart loss and stop
        if (other.CompareTag("Bodyguard") || other.CompareTag("BarbedWire"))
        {
            // Ignore if in jump grace and same object
            if (jumpGraceTimer > 0 && other.gameObject == lastHitWall) return;

            if (other.gameObject != lastHitWall && !stuck)
            {
                lastHitWall = other.gameObject;
                LoseHeart();
                
                // Reset combo on trigger hit
                if (ScoreManager.Instance != null) ScoreManager.Instance.ResetCleanJumps();

                // Disable reward for this obstacle
                ObstacleMove moveScript = other.gameObject.GetComponent<ObstacleMove>();
                if (moveScript != null) moveScript.canRewardCleanJump = false;

                if (!dead)
                {
                    stuck = true;
                    if (animator != null) animator.speed = 0f; // Pauses animation
                    GameSpeed.Multiplier = 0f;

                    // Grant a jump
                    if (movementScript != null) movementScript.ResetJumps();

                    // LOCK POSITION (Kinematic)
                    if (rb != null)
                    {
                        rb.linearVelocity = Vector2.zero;
                        rb.bodyType = RigidbodyType2D.Kinematic;
                    }

                    transform.position += Vector3.left * 0.3f;

                    if (bgVideo != null) bgVideo.Pause();
                }
            }
        }

        // Wall trigger (if collider is trigger)
        if (other.CompareTag("Wall") && !stuck)
        {
            // Ignore if in jump grace and same object
            if (jumpGraceTimer > 0 && other.gameObject == lastHitWall) return;

            // Wall only stops now
            stuck = true;
            if (animator != null) animator.speed = 0f; // Pauses animation
            GameSpeed.Multiplier = 0f;

            // Reset combo on trigger hit
            if (ScoreManager.Instance != null) ScoreManager.Instance.ResetCleanJumps();

            // Disable reward for this obstacle
            ObstacleMove moveScript = other.gameObject.GetComponent<ObstacleMove>();
            if (moveScript != null) moveScript.canRewardCleanJump = false;

            // Play hit wall sound
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayHitWall();

            // Grant a jump
            if (movementScript != null) movementScript.ResetJumps();

            // LOCK POSITION (Kinematic)
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.bodyType = RigidbodyType2D.Kinematic;
            }

            transform.position += Vector3.left * 0.3f;

            if (bgVideo != null) bgVideo.Pause();
        }

        // CatFood detection
        if (other.CompareTag("CatFood"))
        {
            CollectCatFood();
            Destroy(other.gameObject); // Remove the food sprite
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (dead) return;
        // No longer instant death
    }
}