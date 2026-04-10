using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerObstacleRules : MonoBehaviour
{
    public float topNormalThreshold = 0.7f; // Daha yüksek değer = daha hassas yan vuruş algılama

    [Header("Hearts (Wall hits)")]
    public GameObject[] heartUI;
    public int initialHearts = 3;
    private int currentHearts;
    private int maxHearts;
    private const int MAX_ALLOWED_HEARTS = 5;
    private GameObject lastHitWall;
    private float lastHitNormalX = 0f; // Stores the horizontal side of the hit

    [Header("Death kick")]
    public float deathKickX = -8f;
    public float deathKickY = 6f;
    public float deathGravity = 6f;

    private bool stuck = false;
    private bool dead = false;

    public bool IsStuck => stuck;
    public bool IsDead => dead;
    public bool IsHorizontalBlocked => stuck || (unstickJumpTimer > 0f && IsBelowLastHitWall());

    private float unstickJumpTimer = 0f;

    private bool IsBelowLastHitWall()
    {
        if (lastHitWall == null) return false;
        Collider2D col = lastHitWall.GetComponent<Collider2D>();
        if (col == null) return false;
        // Block if our pivot (feet/center area) is still below the upper part of the wall
        return transform.position.y < col.bounds.max.y - 0.2f;
    }

    private Rigidbody2D rb;
    private PlayerMovement movementScript;
    private Animator animator;
    private UnityEngine.Video.VideoPlayer bgVideo;
    private Renderer[] allRenderers;

    [Header("Power-up Settings")]
    private bool isInvincible = false;
    private bool isPotionActive = false;
    private Vector3 originalScale;
    private Coroutine powerUpCoroutine;

    private float jumpGraceTimer = 0f;
    private const float JUMP_GRACE_TIME = 0.5f; 
    
    [Header("Effects")]
    public ParticleSystem sparkleEffect;
    [Header("Hit Recovery")]
    public float hitRecoveryDuration = 5f;
    private float damageCooldown = 0f;
    private const float DAMAGE_COOLDOWN_TIME = 0.5f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        movementScript = GetComponent<PlayerMovement>();
        animator = GetComponentInChildren<Animator>();
        bgVideo = Object.FindFirstObjectByType<UnityEngine.Video.VideoPlayer>();
        originalScale = transform.localScale;
        
        // Nudge Z closer to camera to stay in front of 2D sprites (at Z=0)
        transform.position = new Vector3(transform.position.x, transform.position.y, -5f);
        
        // Cache renderers and sorting
        allRenderers = GetComponentsInChildren<Renderer>(true);
        UpdateSortingOrder(100);
        
        if (sparkleEffect != null) sparkleEffect.gameObject.SetActive(false);
        
        if (AudioManager.Instance != null) AudioManager.Instance.PlayBackgroundMusic();
        
        UpdateGameSpeed();

        if (heartUI != null && heartUI.Length > 0)
        {
            currentHearts = initialHearts;
            maxHearts = initialHearts;

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

        if (damageCooldown > 0)
            damageCooldown -= Time.deltaTime;

        if (unstickJumpTimer > 0)
            unstickJumpTimer -= Time.deltaTime;

        if (stuck)
        {
            // Inputs for exiting the stuck state
            bool jumpInput = Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.UpArrow);
            float h = Input.GetAxisRaw("Horizontal");
            bool moveLeft =
                Input.GetKey(KeyCode.LeftArrow) ||
                Input.GetKey(KeyCode.A) ||
                h < -0.1f ||
                (movementScript != null && movementScript.IsMovingLeft);
            bool moveRight =
                Input.GetKey(KeyCode.RightArrow) ||
                Input.GetKey(KeyCode.D) ||
                h > 0.1f ||
                (movementScript != null && movementScript.IsMovingRight);

            // Direction-Aware Escape Logic:
            // If they hit the LEFT side of the wall (lastHitNormalX < 0), only moving LEFT escapes.
            // If they hit the RIGHT side of the wall (lastHitNormalX > 0), only moving RIGHT escapes.
            bool isEscaping = (lastHitNormalX < -0.3f && moveLeft) || (lastHitNormalX > 0.3f && moveRight);
            
            // Logic: Jump always allow recovery. 
            // Moving away (isEscaping) also allows recovery.
            // Right is ALLOWED only if we are at the screen edge (lastHitWall == null).
            bool canExit = jumpInput || isEscaping || (moveRight && lastHitWall == null);

            if (canExit)
            {
                stuck = false;
                if (animator != null) animator.speed = 1f;

                if (rb != null)
                {
                    rb.simulated = true; 
                    rb.bodyType = RigidbodyType2D.Dynamic;
                    rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                }

                // INSTANT RESUME (If frozen by screen edge): 
                // Ensure no delay when moving away from the retreat limit
                if (lastHitWall == null && LevelManager.Instance != null)
                {
                    float baseSpeed = LevelManager.Instance.GetCurrentBaseSpeed();
                    float multiplier = isInvincible ? 1.4f : (isPotionActive ? 1.2f : 1f);
                    GameSpeed.Multiplier = baseSpeed * multiplier;
                    LevelManager.Instance.SetTargetSpeed(baseSpeed * multiplier);
                }
                else
                {
                    UpdateGameSpeed();
                }
                
                jumpGraceTimer = JUMP_GRACE_TIME;
                
                // If exiting via jump, lock horizontal movement until we clear the wall height
                if (jumpInput) unstickJumpTimer = 0.45f;

                if (movementScript != null)
                {
                    movementScript.ResetJumps();
                    if (jumpInput) movementScript.TryJump();
                }

                if (bgVideo != null) bgVideo.Play();
            }
        }

        // Note: Left boundary handling is done by PlayerMovement clamping.
        // We do NOT enter stuck state for screen/world edges, to keep jump responsive.
    }

    private void EnterStuckState(GameObject hitSource)
    {
        if (dead) return;
        
        stuck = true;
        lastHitWall = hitSource; // Track if it's a solid obstacle or just the screen edge

        // POSITION SNAPPING REMOVED (User Request):
        // No longer forcing the player to snap to a specific X coordinate upon impact.
        // The character will now stop exactly where the physics engine caught the collision.

        if (animator != null) 
        {
            animator.speed = 1f;
            animator.SetBool("walking", false);
            animator.SetBool("Idle", true);
        }
        GameSpeed.Multiplier = 0f;
        UpdateGameSpeed(); // Reinforce target speed stop in LevelManager

        if (movementScript != null) 
        {
            movementScript.ResetJumps();
            movementScript.ResetHorizontalVelocity(); // Stop momentum instantly
        }

        if (rb != null)
        {
            // Zero out ALL movement instantly to prevent "sliding inside"
            rb.linearVelocity = Vector2.zero;
            rb.simulated = true; // Stay in simulation for gravity
        }

        if (bgVideo != null) bgVideo.Pause();
    }

    private void ApplyTightPlayerCollider()
    {
        BoxCollider2D boxCol = GetComponent<BoxCollider2D>();
        SpriteRenderer sr = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();

        if (boxCol != null && sr != null && sr.sprite != null)
        {
            Sprite sprite = sr.sprite;
            int shapeCount = sprite.GetPhysicsShapeCount();
            if (shapeCount == 0) return;

            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;
            List<Vector2> path = new List<Vector2>();

            for (int i = 0; i < shapeCount; i++)
            {
                sprite.GetPhysicsShape(i, path);
                foreach (Vector2 p in path)
                {
                    if (p.x < minX) minX = p.x; if (p.x > maxX) maxX = p.x;
                    if (p.y < minY) minY = p.y; if (p.y > maxY) maxY = p.y;
                }
            }

            Vector2 size = new Vector2(maxX - minX, maxY - minY);
            Vector2 center = new Vector2(minX + size.x / 2f, minY + size.y / 2f);

            // Apply ONLY the offset to keep the collider centered on visuals.
            // Preservation Rule: Keep the manually set size from the prefab/LevelManager.
            boxCol.offset = center;
            
            Debug.Log("Player: Auto-Centered Offset Applied (Size Preserved).");
        }
    }

    private float CalculateTightCatHalfWidth()
    {
        // Get the active sprite renderer (child objects might have the skin)
        SpriteRenderer sr = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
        if (sr == null || sr.sprite == null) return GetComponent<Collider2D>().bounds.size.x / 2f;

        Sprite sprite = sr.sprite;
        int shapeCount = sprite.GetPhysicsShapeCount();
        if (shapeCount == 0) return sprite.bounds.size.x / 2f;

        float minX = float.MaxValue;
        float maxX = float.MinValue;
        List<Vector2> path = new List<Vector2>();

        for (int i = 0; i < shapeCount; i++)
        {
            sprite.GetPhysicsShape(i, path);
            foreach (Vector2 p in path)
            {
                if (p.x < minX) minX = p.x;
                if (p.x > maxX) maxX = p.x;
            }
        }

        float visualWidth = (maxX - minX) * transform.localScale.x;
        return visualWidth / 2f;
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        if (dead) return;
        Vector2 normal = col.contactCount > 0 ? col.GetContact(0).normal : Vector2.left;
        HandleInteraction(col.gameObject, normal, false);
    }

    private void OnCollisionStay2D(Collision2D col)
    {
        if (dead || stuck) 
        {
            if (stuck) UpdateGameSpeed(); // Reinforce freeze
            return;
        }

        Vector2 normal = col.contactCount > 0 ? col.GetContact(0).normal : Vector2.left;
        HandleInteraction(col.gameObject, normal, false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (dead) return;
        
        if (other.CompareTag("Fish"))
        {
            CollectFish();
            Destroy(other.gameObject);
            return;
        }

        if (other.CompareTag("Potion"))
        {
            CollectPotion();
            Destroy(other.gameObject);
            return;
        }

        HandleInteraction(other.gameObject, GetTriggerNormal(other), true);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (dead || stuck)
        {
            if (stuck) UpdateGameSpeed(); // Reinforce freeze
            return;
        }
        
        // Only re-handle if it's a hazard
        bool isEnemyHit = IsEnemy(other.gameObject);
        if (other.CompareTag("Obstacle") || other.CompareTag("Wall") || other.CompareTag("LongWall") || other.CompareTag("Bush") || isEnemyHit)
        {
            HandleInteraction(other.gameObject, GetTriggerNormal(other), true);
        }
    }

    private Vector2 GetTriggerNormal(Collider2D other)
    {
        if (other == null) return Vector2.left;

        Vector2 delta = (Vector2)transform.position - (Vector2)other.bounds.center;
        if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
        {
            return new Vector2(Mathf.Sign(delta.x == 0f ? -1f : delta.x), 0f);
        }

        return new Vector2(0f, Mathf.Sign(delta.y == 0f ? 1f : delta.y));
    }

    private void HandleInteraction(GameObject other, Vector2 normal, bool isTrigger)
    {
        bool isEnemy = IsEnemy(other);
        bool isBush = other.CompareTag("Bush");
        bool isWall = other.CompareTag("Wall") || other.CompareTag("LongWall");
        bool isObstacle = other.CompareTag("Obstacle");

        if (!isEnemy && !isBush && !isWall && !isObstacle) return;

        // NEW: Priority Enemy Damage Rule
        // Any contact with an enemy (top, side, trigger, or physics) results in damage.
        if (isEnemy && damageCooldown <= 0 && !isInvincible)
        {
            if (isPotionActive)
            {
                ResetPotionEffect();
                damageCooldown = 0.2f;
            }
            else
            {
                LoseHeart();
                damageCooldown = DAMAGE_COOLDOWN_TIME;
            }
            if (AudioManager.Instance != null) AudioManager.Instance.PlayHitWall();
            lastHitWall = other;
            return; // Damage taken, stop processing other logic (like stuck/crush)
        }

        bool hitTop = isTrigger ? (transform.position.y > other.GetComponent<Collider2D>().bounds.center.y + 0.5f) : (normal.y > topNormalThreshold);

        if (hitTop && !isTrigger) // Crush logic (only for physics collisions)
        {
            if (isObstacle)
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayCrush();
                if (ScoreManager.Instance != null) ScoreManager.Instance.AddXP(20);
                Destroy(other);
                if (LevelManager.Instance != null) LevelManager.Instance.ObstaclePassed();
            }
            return;
        }

        if (isInvincible && (isEnemy || isBush || isWall)) 
        {
            return;
        }


        if (damageCooldown <= 0 && !(jumpGraceTimer > 0 && other == lastHitWall))
        {
            // NEW SAFETY: Don't take damage if moving left (retreating) unless it's a very specific intentional hazard.
            bool isRetreating = movementScript != null && movementScript.IsMovingLeft;
            bool isLethal = (isEnemy || isBush) && !isRetreating;
            
            if (isLethal)
            {
                if (isPotionActive)
                {
                    ResetPotionEffect();
                    damageCooldown = 0.2f;
                }
                else
                {
                    LoseHeart();
                    damageCooldown = DAMAGE_COOLDOWN_TIME;
                }
            }
            else if (isWall || isObstacle) // Harmless wall/bag hits
            {
                if (isPotionActive) ResetPotionEffect();
                damageCooldown = 0.5f; // Debounce timer to prevent sound/logic spam
            }
            
            if (AudioManager.Instance != null) AudioManager.Instance.PlayHitWall();
            lastHitWall = other;
            ObstacleMove move = other.GetComponent<ObstacleMove>();
            if (move != null) move.canRewardCleanJump = false;
        }

        // Side hit freeze
        if (!hitTop)
        {
            // NEW: Direction-Aware Escape Detection
            // If the player is moving AWAY from the hit side, skip entering stuck state.
            bool moveLeft = movementScript != null && movementScript.IsMovingLeft;
            bool moveRight = movementScript != null && movementScript.IsMovingRight;
            bool isMovingAway = (normal.x < -0.3f && moveLeft) || (normal.x > 0.3f && moveRight);

            if (isMovingAway) return;

            // Yan vuruş (duvara çarpma): Hassasiyeti artırdık (0.3 -> 0.1)
            bool shouldFreeze = isTrigger ? (transform.position.y < other.GetComponent<Collider2D>().bounds.center.y + 0.5f) : (Mathf.Abs(normal.x) > 0.1f);
            if (shouldFreeze) 
            {
                lastHitNormalX = normal.x;
                EnterStuckState(other);
            }
        }
    }

    public void LoseHeart()
    {
        if (dead) return;
        currentHearts--;
        
        if (heartUI != null && currentHearts >= 0 && currentHearts < heartUI.Length)
        {
            if (heartUI[currentHearts] != null)
                heartUI[currentHearts].SetActive(false);
        }

        if (AudioManager.Instance != null) AudioManager.Instance.PlayHeartLost();

        // Reset Level 5 progress (on user request)
        if (LevelManager.Instance != null) LevelManager.Instance.ResetProgress();

        StartCoroutine(FlashRecoveryEffect());

        if (currentHearts <= 0) Die();
    }


    public void CollectFish()
    {
        if (dead) return;
        if (powerUpCoroutine != null) 
        {
            StopCoroutine(powerUpCoroutine);
        }
        powerUpCoroutine = StartCoroutine(PowerUpSequence());

        if (currentHearts >= maxHearts && maxHearts < MAX_ALLOWED_HEARTS)
        {
            maxHearts++;
            currentHearts = maxHearts;
        }
        else if (currentHearts < maxHearts)
        {
            currentHearts++;
        }

        if (heartUI != null)
        {
            for (int i = 0; i < heartUI.Length; i++)
            {
                if (heartUI[i] != null)
                {
                    heartUI[i].SetActive(i < currentHearts);
                }
            }
        }

        if (AudioManager.Instance != null) AudioManager.Instance.PlayHeartFill();
        if (ScoreManager.Instance != null) ScoreManager.Instance.AddXP(50);
    }

    public void CollectPotion()
    {
        if (dead) return;
        
        // Scenario 1: First pickup (Normal -> Big)
        if (!isPotionActive)
        {
            isPotionActive = true;
            transform.localScale = originalScale * 1.2f;
            
            if (movementScript != null)
            {
                movementScript.SetJumpMultiplier(1.2f);
            }

            UpdateGameSpeed();

            // First pickup: Add XP but skip XP sound (user request)
            if (ScoreManager.Instance != null) ScoreManager.Instance.AddXP(75, false);
        }
        // Scenario 2: Already growing (Big -> Big)
        else
        {
            // Just add XP and play XP sound
            if (ScoreManager.Instance != null) ScoreManager.Instance.AddXP(75, true);
        }

        // ALWAYS play Increase sound for every bottle collected
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayPotionIncrease();
    }

    public void ResetPotionEffect()
    {
        if (!isPotionActive) return;

        isPotionActive = false;
        transform.localScale = originalScale;

        if (movementScript != null)
        {
            movementScript.ResetJumpMultiplier();
        }

        if (AudioManager.Instance != null) 
            AudioManager.Instance.PlayPotionDecrease();

        UpdateGameSpeed();
    }

    public void UpdateGameSpeed()
    {
        if (dead || stuck)
        {
            GameSpeed.Multiplier = 0f;
            if (LevelManager.Instance != null) LevelManager.Instance.SetTargetSpeed(0f);
            return;
        }

        float baseSpeed = 1f;
        if (LevelManager.Instance != null)
        {
            baseSpeed = LevelManager.Instance.GetCurrentBaseSpeed();
        }

        float multiplier = 1f;
        if (isInvincible)
        {
            multiplier = 1.4f; // Increased from 1.2f for more noticeable boost
        }
        else if (isPotionActive)
        {
            multiplier = 1.2f;
        }

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.SetTargetSpeed(baseSpeed * multiplier);
        }
        else
        {
            GameSpeed.Multiplier = baseSpeed * multiplier;
        }
    }

    public void ResetForLevelStart()
    {
        stuck = false;
        dead = false;
        lastHitWall = null;
        lastHitNormalX = 0f;
        unstickJumpTimer = 0f;
        jumpGraceTimer = 0f;
        damageCooldown = 0f;

        if (animator != null)
        {
            animator.speed = 1f;
            animator.SetBool("walking", false);
            animator.SetBool("Idle", true);
        }

        if (rb != null)
        {
            rb.simulated = true;
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        UpdateGameSpeed();
    }

    private IEnumerator PowerUpSequence()
    {
        isInvincible = true;
        UpdateGameSpeed(); // Dynamically calculate speed based on level + boost
        UpdateSortingOrder(100);
        
        if (sparkleEffect != null) 
        {
            sparkleEffect.gameObject.SetActive(true);
            sparkleEffect.Play();
        }
        
        yield return new WaitForSeconds(10f);

        if (sparkleEffect != null) 
        {
            sparkleEffect.Stop();
            sparkleEffect.gameObject.SetActive(false);
        }

        isInvincible = false;
        UpdateGameSpeed();
        if (allRenderers != null)
        {
            UpdateSortingOrder(100);
        }
        powerUpCoroutine = null;
    }

    private void Die()
    {
        dead = true;
        stuck = false;
        GameSpeed.Multiplier = 0f;
        if (bgVideo != null) bgVideo.Pause();
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopBackgroundMusic();
            AudioManager.Instance.PlayGameOverSound();
            AudioManager.Instance.PlayGameOverSFX();
        }
        if (movementScript != null) movementScript.enabled = false;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = deathGravity;
            rb.AddForce(new Vector2(deathKickX, deathKickY), ForceMode2D.Impulse);
        }
        if (GameOverManager.Instance != null) GameOverManager.Instance.ShowGameOver();
    }

    public void UpdateSortingOrder(int order)
    {
        if (allRenderers == null) allRenderers = GetComponentsInChildren<Renderer>(true);
        foreach (var r in allRenderers) 
        {
            if (r != null) 
            {
                if (sparkleEffect != null && r == sparkleEffect.GetComponent<Renderer>()) continue;
                r.sortingLayerName = "Default";
                r.sortingOrder = order;
            }
        }
    }

    private bool IsEnemy(GameObject obj)
    {
        if (obj == null) return false;
        if (obj.CompareTag("Enemy")) return true;
        if (obj.transform.root != null && obj.transform.root.CompareTag("Enemy")) return true;
        return false;
    }


    private IEnumerator FlashRecoveryEffect()
    {
        float elapsed = 0f;
        // Only get renderers that are CURRENTLY enabled to avoid white square glitches
        var targetRenderers = new List<Renderer>();
        Renderer[] all = GetComponentsInChildren<Renderer>(true);
        if (all != null)
        {
            foreach (var r in all)
            {
                // Only blink things that are part of the cat but NOT hidden helper objects
                // and were enabled when the hit happened.
                if (r != null && r.enabled && !r.gameObject.name.Contains("Square") && !r.gameObject.name.Contains("Indicator"))
                {
                    targetRenderers.Add(r);
                }
            }
        }

        while (elapsed < hitRecoveryDuration)
        {
            foreach (var r in targetRenderers) 
            {
                if (r != null) r.enabled = !r.enabled;
            }
            yield return new WaitForSeconds(0.08f);
            elapsed += 0.08f;
        }

        foreach (var r in targetRenderers) 
        {
            if (r != null) r.enabled = true;
        }
    }
}
