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


        if (stuck && (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKey(KeyCode.LeftArrow)))
        {
            stuck = false;
            if (animator != null) animator.speed = 1f;
            GameSpeed.Multiplier = 1f;
            jumpGraceTimer = JUMP_GRACE_TIME;

            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            }

            if (movementScript != null)
            {
                movementScript.ResetJumps();
                movementScript.TryJump();
            }

            if (bgVideo != null) bgVideo.Play();
        }
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        if (dead) return;

        bool isBodyguard = col.collider.CompareTag("Bodyguard") || col.transform.root.CompareTag("Bodyguard");
        bool isBarbedWire = col.collider.CompareTag("BarbedWire") || col.transform.root.CompareTag("BarbedWire");

        if (isBodyguard || isBarbedWire)
        {
            if (isInvincible) return;

            if (col.gameObject != lastHitWall && !stuck)
            {
                lastHitWall = col.gameObject;
                LoseHeart();
                
                if (ScoreManager.Instance != null) ScoreManager.Instance.ResetCleanJumps();

                // Removed flashing effect for Bodyguard per user request
                // Recovery flashing is now only for BarbedWire or generic recovery if added elsewhere
                if (isBarbedWire)
                {
                    hitRecoveryTimer = hitRecoveryDuration;
                    StartCoroutine(FlashRecoveryEffect());
                }

                ObstacleMove moveScript = col.gameObject.GetComponent<ObstacleMove>();
                if (moveScript != null) moveScript.canRewardCleanJump = false;

                if (!dead)
                {
                    stuck = true;
                    if (animator != null) animator.speed = 0f;
                    GameSpeed.Multiplier = 0f;

                    if (movementScript != null) movementScript.ResetJumps();

                    if (rb != null)
                    {
                        rb.linearVelocity = Vector2.zero;
                        rb.bodyType = RigidbodyType2D.Kinematic;
                    }

                    if (bgVideo != null) bgVideo.Pause();
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
                
                // Increment level progress when crushing obstacles
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
                lastHitWall = col.gameObject;
                stuck = true;
                if (animator != null) animator.speed = 0f;
                GameSpeed.Multiplier = 0f;

                if (AudioManager.Instance != null) AudioManager.Instance.PlayHitWall();
                if (movementScript != null) movementScript.ResetJumps();

                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                }

                if (bgVideo != null) bgVideo.Pause();
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

        // Expansion logic: If at max capacity but capacity is less than 5, increase capacity
        if (currentHearts >= maxHearts && maxHearts < MAX_ALLOWED_HEARTS)
        {
            maxHearts++;
            currentHearts = maxHearts;
        }
        else if (currentHearts < maxHearts)
        {
            // Simple heal logic: If taken damage, just heal
            currentHearts++;
        }

        // Update UI
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
                    allRenderers[i].sortingOrder = 0;
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

            if (other.gameObject != lastHitWall && !stuck)
            {
                lastHitWall = other.gameObject;
                LoseHeart();
                
                if (ScoreManager.Instance != null) ScoreManager.Instance.ResetCleanJumps();

                // Removed flashing effect for Bodyguard per user request
                if (isBarbedWire)
                {
                    hitRecoveryTimer = hitRecoveryDuration;
                    StartCoroutine(FlashRecoveryEffect());
                }

                ObstacleMove moveScript = other.gameObject.GetComponent<ObstacleMove>();
                if (moveScript != null) moveScript.canRewardCleanJump = false;

                if (!dead)
                {
                    stuck = true;
                    if (animator != null) animator.speed = 0f;
                    GameSpeed.Multiplier = 0f;

                    if (movementScript != null) movementScript.ResetJumps();

                    if (rb != null)
                    {
                        rb.linearVelocity = Vector2.zero;
                        rb.bodyType = RigidbodyType2D.Kinematic;
                    }

                    if (bgVideo != null) bgVideo.Pause();
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

            lastHitWall = other.gameObject;
            stuck = true;
            if (animator != null) animator.speed = 0f;
            GameSpeed.Multiplier = 0f;

            if (ScoreManager.Instance != null) ScoreManager.Instance.ResetCleanJumps();

            ObstacleMove moveScript = other.gameObject.GetComponent<ObstacleMove>();
            if (moveScript != null) moveScript.canRewardCleanJump = false;

            if (AudioManager.Instance != null) AudioManager.Instance.PlayHitWall();
            if (movementScript != null) movementScript.ResetJumps();

            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }

            if (bgVideo != null) bgVideo.Pause();
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
        
        // Re-check wall collision during stay if we missed the enter event while moving left
        if (other.CompareTag("Wall") || other.CompareTag("LongWall"))
        {
            if (other.gameObject != lastHitWall)
            {
                lastHitWall = other.gameObject;
                stuck = true;
                if (animator != null) animator.speed = 0f;
                GameSpeed.Multiplier = 0f;
                if (bgVideo != null) bgVideo.Pause();
            }
        }
    }

    private void OnCollisionStay2D(Collision2D col)
    {
        if (dead || stuck) return;

        if (col.collider.CompareTag("Wall") || col.collider.CompareTag("LongWall"))
        {
            // If we are touching a wall but not 'stuck', force stuck state
            // This prevents sliding through walls when moving left
            lastHitWall = col.gameObject;
            stuck = true;
            if (animator != null) animator.speed = 0f;
            GameSpeed.Multiplier = 0f;
            if (bgVideo != null) bgVideo.Pause();
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