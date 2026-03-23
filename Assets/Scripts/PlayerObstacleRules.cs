using UnityEngine;
using System.Collections;

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
    private const int MAX_ALLOWED_HEARTS = 5;
    private GameObject lastHitWall;

    [Header("Death kick")]
    public float deathKickX = -8f;
    public float deathKickY = 6f;
    public float deathGravity = 6f;

    private bool stuck = false;
    private bool dead = false;

    public bool IsStuck => stuck;
    public bool IsDead => dead;

    private Rigidbody2D rb;
    private PlayerMovement movementScript;
    private Animator animator;
    private UnityEngine.Video.VideoPlayer bgVideo;
    private Renderer[] allRenderers;

    [Header("Power-up Settings")]
    private bool isInvincible = false;
    private Coroutine powerUpCoroutine;

    private float jumpGraceTimer = 0f;
    private const float JUMP_GRACE_TIME = 0.5f; 
    
    [Header("Effects")]
    public ParticleSystem sparkleEffect;

    private float startX;
    [Header("Return Speed")]
    public float returnSpeed = 2f;

    [Header("Hit Recovery")]
    public float hitRecoveryDuration = 0.8f;
    private float hitRecoveryTimer = 0f;
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
        startX = transform.position.x;
        
        // Nudge Z closer to camera to stay in front of 2D sprites (at Z=0)
        transform.position = new Vector3(transform.position.x, transform.position.y, -5f);
        
        UpdateSortingOrder(100);
        if (sparkleEffect != null) sparkleEffect.gameObject.SetActive(false);

        // Initialize hearts

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

        if (hitRecoveryTimer > 0)
            hitRecoveryTimer -= Time.deltaTime;

        if (damageCooldown > 0)
            damageCooldown -= Time.deltaTime;


        if (stuck)
        {
            bool jumpInput = Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.UpArrow);
            bool moveInput = Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow);

            if (jumpInput || moveInput)
            {
                stuck = false;
                if (animator != null) animator.speed = 1f;
                GameSpeed.Multiplier = 1f;
                jumpGraceTimer = JUMP_GRACE_TIME;

                if (rb != null)
                {
                    rb.bodyType = RigidbodyType2D.Dynamic;
                    // Restore rotation freeze but allow position movement
                    rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                }

                if (movementScript != null)
                {
                    movementScript.ResetJumps();
                    // Only jump if the input was a jump key
                    if (jumpInput) movementScript.TryJump();
                }

                if (bgVideo != null) bgVideo.Play();
            }
        }

        // --- BOUNDARY FREEZE CHECK ---
        if (!dead && !stuck && movementScript != null)
        {
            // If we are at the left boundary and trying to move left
            if (transform.position.x <= movementScript.minX + 0.05f && Input.GetKey(KeyCode.LeftArrow))
            {
                EnterStuckState(null); // No specific obstacle
            }
        } 
    }

    private void EnterStuckState(GameObject hitSource)
    {
        if (dead) return;
        
        stuck = true;
        if (hitSource != null) 
        {
            lastHitWall = hitSource;
            
            // Nudge player slightly away from the obstacle to prevent clipping
            // Assuming obstacles come from the right (positive X relative to player)
            float nudgeX = -0.3f; 
            transform.position = new Vector3(transform.position.x + nudgeX, transform.position.y, transform.position.z);
        }

        if (animator != null) animator.speed = 0f;
        GameSpeed.Multiplier = 0f;

        if (movementScript != null) movementScript.ResetJumps();

        if (rb != null)
        {
            // Only stop horizontal progress
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        if (bgVideo != null) bgVideo.Pause();
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        if (dead) return;

        bool isBodyguard = IsBodyguard(col.gameObject);
        bool isBarbedWire = col.collider.CompareTag("BarbedWire") || col.transform.root.CompareTag("BarbedWire");

        if (isBodyguard || isBarbedWire)
        {
            if (isInvincible) return;

            // Removed `col.gameObject != lastHitWall` to ensure damage triggers even if lastHitWall wasn't cleared.
            // damageCooldown already prevents multiple hearts lost in a single collision frame.
            if (damageCooldown <= 0)
            {
                lastHitWall = col.gameObject;
                LoseHeart();
                damageCooldown = DAMAGE_COOLDOWN_TIME;
                
                if (ScoreManager.Instance != null) ScoreManager.Instance.ResetCleanJumps();

                if (isBarbedWire)
                {
                    hitRecoveryTimer = hitRecoveryDuration;
                }

                ObstacleMove moveScript = col.gameObject.GetComponent<ObstacleMove>();
                if (moveScript != null) moveScript.canRewardCleanJump = false;

                // Only freeze the world if it's a side hit
                Vector2 normal = col.GetContact(0).normal;
                if (Mathf.Abs(normal.x) > 0.4f)
                {
                    EnterStuckState(col.gameObject);
                }
            }
            return;
        }

        bool isWall = col.collider.CompareTag("Wall") || col.collider.CompareTag("LongWall");
        if (!col.collider.CompareTag("Obstacle") && !isWall) return;

        bool hitTop = false;
        for (int i = 0; i < col.contactCount; i++)
        {
            Vector2 n = col.GetContact(i).normal;
            if (n.y > topNormalThreshold)
            {
                hitTop = true;
                break;
            }
        }

        if (hitTop)
        {
            if (col.collider.CompareTag("Obstacle") || col.collider.CompareTag("Wall") || col.collider.CompareTag("LongWall"))
            {
                if (col.collider.CompareTag("Obstacle")) 
                {
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayCrush();
                    if (ScoreManager.Instance != null) ScoreManager.Instance.AddXP(20);
                    Destroy(col.gameObject);
                }
                
                if (LevelManager.Instance != null) LevelManager.Instance.ObstaclePassed();
            }
            return;
        }

        if (!hitTop && !stuck)
        {
            if (jumpGraceTimer > 0 && col.gameObject == lastHitWall)
            {
                if (col.gameObject.CompareTag("Wall"))
                {
                    if (jumpGraceTimer > (JUMP_GRACE_TIME - 0.25f)) return;
                }
                else return;
            }

            if (ScoreManager.Instance != null) ScoreManager.Instance.ResetCleanJumps();

            ObstacleMove moveScript = col.gameObject.GetComponent<ObstacleMove>();
            if (moveScript != null) moveScript.canRewardCleanJump = false;

            if (isWall || col.collider.CompareTag("Obstacle"))
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayHitWall();
                EnterStuckState(col.gameObject);
            }
        }
    }

    private void LoseHeart()
    {
        if (dead) return;
        currentHearts--;
        
        if (heartUI != null && currentHearts >= 0 && currentHearts < heartUI.Length)
        {
            if (heartUI[currentHearts] != null)
                heartUI[currentHearts].SetActive(false);
        }

        if (AudioManager.Instance != null) AudioManager.Instance.PlayHeartLost();

        StartCoroutine(FlashRecoveryEffect());

        if (currentHearts < 0) Die();
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

    private IEnumerator PowerUpSequence()
    {
        isInvincible = true;
        GameSpeed.Multiplier = 1.5f;
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
        GameSpeed.Multiplier = 1f;
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

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (dead) return;

        bool isBodyguard = IsBodyguard(other.gameObject);
        bool isBarbedWire = other.CompareTag("BarbedWire") || other.transform.root.CompareTag("BarbedWire");

        if (isBodyguard || isBarbedWire)
        {
            if (isInvincible) return;

            if (damageCooldown <= 0)
            {
                lastHitWall = other.gameObject;
                LoseHeart();
                damageCooldown = DAMAGE_COOLDOWN_TIME;
                
                if (ScoreManager.Instance != null) ScoreManager.Instance.ResetCleanJumps();

                if (isBarbedWire)
                {
                    hitRecoveryTimer = hitRecoveryDuration;
                }

                ObstacleMove moveScript = other.gameObject.GetComponent<ObstacleMove>();
                if (moveScript != null) moveScript.canRewardCleanJump = false;

                // Only freeze if we are not significantly above the obstacle
                if (transform.position.y < other.bounds.center.y + 0.5f)
                {
                    EnterStuckState(other.gameObject);
                }
            }
        }
        bool isWall = other.CompareTag("Wall") || other.CompareTag("LongWall");
        if (isWall && !stuck)
        {
            if (isInvincible) return;

            if (jumpGraceTimer > 0 && other.gameObject == lastHitWall)
            {
                if (jumpGraceTimer > (JUMP_GRACE_TIME - 0.25f)) return;
            }

            // Damage on any hit (except LongWall)
            if (damageCooldown <= 0 && !other.CompareTag("LongWall"))
            {
                LoseHeart();
                damageCooldown = DAMAGE_COOLDOWN_TIME;
            }

            if (AudioManager.Instance != null) AudioManager.Instance.PlayHitWall();
            
            // Pass reporting for triggers (especially LongWall)
            if (LevelManager.Instance != null) LevelManager.Instance.ObstaclePassed();

            // Only freeze if we are not significantly above the obstacle
            if (transform.position.y < other.bounds.center.y + 0.5f)
            {
                EnterStuckState(other.gameObject);
            }
        }

        if (other.CompareTag("Fish"))
        {
            CollectFish();
            Destroy(other.gameObject);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (dead || stuck) return;
        
        bool isBlockingObstacle = other.CompareTag("Wall") || other.CompareTag("LongWall") || other.CompareTag("Bodyguard");
        if (isBlockingObstacle && (other.gameObject != lastHitWall || !stuck))
        {
            EnterStuckState(other.gameObject);
        }
    }

    private void OnCollisionStay2D(Collision2D col)
    {
        if (dead || stuck) return;

        bool isBlockingObstacle = col.collider.CompareTag("Wall") || col.collider.CompareTag("LongWall") || col.collider.CompareTag("Bodyguard");
        if (isBlockingObstacle)
        {
            EnterStuckState(col.gameObject);
        }
    }

    public void UpdateSortingOrder(int order)
    {
        allRenderers = GetComponentsInChildren<Renderer>(true);
        if (allRenderers != null)
        {
            foreach (var r in allRenderers) 
            {
                if (r != null) 
                {
                    // Don't override the sparkle effect's custom sorting
                    if (sparkleEffect != null && r == sparkleEffect.GetComponent<Renderer>()) continue;
                    
                    r.sortingLayerName = "Default";
                    r.sortingOrder = order;
                }
            }
        }
    }

    private bool IsBodyguard(GameObject obj)
    {
        if (obj == null) return false;
        if (obj.CompareTag("Bodyguard")) return true;
        if (obj.transform.root != null && obj.transform.root.CompareTag("Bodyguard")) return true;
        return false;
    }

    private IEnumerator FlashRecoveryEffect()
    {
        float elapsed = 0f;
        // Only get renderers that are CURRENTLY enabled to avoid white square glitches
        var targetRenderers = new System.Collections.Generic.List<Renderer>();
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
