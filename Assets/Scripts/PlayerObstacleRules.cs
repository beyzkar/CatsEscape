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
    public Material playerGlowMaterial;
    private Material[] originalMaterials;
    private bool isInvincible = false;
    private Coroutine powerUpCoroutine;

    private float jumpGraceTimer = 0f;
    private const float JUMP_GRACE_TIME = 0.5f; 

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
        movementScript = GetComponent<PlayerMovement>();
        animator = GetComponentInChildren<Animator>();
        bgVideo = Object.FindFirstObjectByType<UnityEngine.Video.VideoPlayer>();
        startX = transform.position.x;

        // Set constant high sorting order to stay in front of obstacles
        allRenderers = GetComponentsInChildren<Renderer>();
        if (allRenderers != null)
        {
            foreach (var r in allRenderers) if (r != null) r.sortingOrder = 50;
        }

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
        if (hitSource != null) lastHitWall = hitSource;

        if (animator != null) animator.speed = 0f;
        GameSpeed.Multiplier = 0f;

        if (movementScript != null) movementScript.ResetJumps();

        if (rb != null)
        {
            // Only stop horizontal progress
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            rb.bodyType = RigidbodyType2D.Dynamic;
            // Removed FreezeAll to allow smooth falling
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        if (bgVideo != null) bgVideo.Pause();
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        if (dead) return;

        bool isBodyguard = col.collider.CompareTag("Bodyguard") || col.transform.root.CompareTag("Bodyguard");
        bool isBarbedWire = col.collider.CompareTag("BarbedWire") || col.transform.root.CompareTag("BarbedWire");

        if (isBodyguard || isBarbedWire)
        {
            if (isInvincible) return;

            if (col.gameObject != lastHitWall && damageCooldown <= 0)
            {
                lastHitWall = col.gameObject;
                LoseHeart();
                damageCooldown = DAMAGE_COOLDOWN_TIME;
                
                if (ScoreManager.Instance != null) ScoreManager.Instance.ResetCleanJumps();

                if (isBarbedWire)
                {
                    hitRecoveryTimer = hitRecoveryDuration;
                    StartCoroutine(FlashRecoveryEffect());
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
            if (col.collider.CompareTag("Obstacle"))
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayCrush();
                if (ScoreManager.Instance != null) ScoreManager.Instance.AddXP(20);
                if (LevelManager.Instance != null) LevelManager.Instance.ObstaclePassed();
                Destroy(col.gameObject);
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

        if (currentHearts < 0) Die();
    }


    public void CollectFish()
    {
        if (dead) return;
        if (powerUpCoroutine != null) StopCoroutine(powerUpCoroutine);
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
        allRenderers = GetComponentsInChildren<Renderer>();
        if (allRenderers != null && allRenderers.Length > 0 && playerGlowMaterial != null)
        {
            originalMaterials = new Material[allRenderers.Length];
            for (int i = 0; i < allRenderers.Length; i++)
            {
                originalMaterials[i] = allRenderers[i].sharedMaterial;
                allRenderers[i].sortingOrder = 100;
                Material glowInstance = new Material(playerGlowMaterial);
                if (originalMaterials[i].HasProperty("_MainTex")) glowInstance.mainTexture = originalMaterials[i].mainTexture;
                allRenderers[i].material = glowInstance;
            }
        }

        yield return new WaitForSeconds(10f);

        isInvincible = false;
        GameSpeed.Multiplier = 1f;
        if (allRenderers != null && originalMaterials != null)
        {
            for (int i = 0; i < allRenderers.Length; i++)
            {
                if (allRenderers[i] != null && i < originalMaterials.Length)
                {
                    allRenderers[i].material = originalMaterials[i];
                    allRenderers[i].sortingOrder = 50; // Restore to our fixed high sorting order
                }
            }
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

        bool isBodyguard = other.CompareTag("Bodyguard") || other.transform.root.CompareTag("Bodyguard");
        bool isBarbedWire = other.CompareTag("BarbedWire") || other.transform.root.CompareTag("BarbedWire");

        if (isBodyguard || isBarbedWire)
        {
            if (isInvincible) return;

            if (other.gameObject != lastHitWall && damageCooldown <= 0)
            {
                lastHitWall = other.gameObject;
                LoseHeart();
                damageCooldown = DAMAGE_COOLDOWN_TIME;
                
                if (ScoreManager.Instance != null) ScoreManager.Instance.ResetCleanJumps();

                if (isBarbedWire)
                {
                    hitRecoveryTimer = hitRecoveryDuration;
                    StartCoroutine(FlashRecoveryEffect());
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

            // Damage on any hit
            if (damageCooldown <= 0)
            {
                LoseHeart();
                damageCooldown = DAMAGE_COOLDOWN_TIME;
            }

            if (AudioManager.Instance != null) AudioManager.Instance.PlayHitWall();

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

    private IEnumerator FlashRecoveryEffect()
    {
        float elapsed = 0f;
        allRenderers = GetComponentsInChildren<Renderer>();
        while (elapsed < hitRecoveryDuration)
        {
            if (allRenderers != null)
            {
                foreach (var r in allRenderers) if (r != null) r.enabled = !r.enabled;
            }
            yield return new WaitForSeconds(0.08f);
            elapsed += 0.08f;
        }
        if (allRenderers != null)
        {
            foreach (var r in allRenderers) if (r != null) r.enabled = true;
        }
    }
}