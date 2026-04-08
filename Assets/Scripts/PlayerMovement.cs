using UnityEngine;

// Core player movement controller: handles horizontal movement, jumping, and rotation/flipping
public class PlayerMovement : MonoBehaviour
{
    public static PlayerMovement Instance { get; private set; }

    [Header("Jump Settings")]
    public float jumpForce = 12f;
    public float fallMultiplier = 2.5f; // Düşerken uygulanacak yerçekimi çarpanı
    public float lowJumpMultiplier = 2f; // Tuşa kısa basınca uygulanacak çarpan
    private float currentJumpForce;
    public int maxJumps = 2; 
    private int jumpsLeft;

    [Header("Level 5 Fall Settings")]
    public float deathThresholdY = -4.2f; // Height at which jumping is disabled in Level 5

    [Header("Ground Detection")]
    public Transform groundCheck; // Point used to detect if the player is on the ground
    public float groundCheckRadius = 0.15f;
    public LayerMask groundLayer; // Specifies which layer is ground
    /*
     Layer:
       - Groups objects
       - Provides physics and detection control
       - Prevents errors
       - Improves performance
     */

    private Rigidbody2D rb; // Rigidbody2D: Adds physics to the object
    /*
     - Applies gravity
     - Enables falling
     - Allows applying forces
     - Reacts to collisions
     */
    private bool isGrounded;
    public bool IsGrounded => isGrounded;

    [Header("Horizontal Movement")]
    public float moveRightSpeed = 7.5f; // Increased from 5f to match 1.5x request
    public float moveLeftSpeed = 5f; 
    public float minX = -5.3f; // Initial left boundary
    public float maxX = 5f;    // Initial right boundary
    public float acceleration = 5f;
    public float deceleration = 10f;
    private float currentHorizontalVelocity = 0f;
    private float targetVelocityX = 0f;

    [Header("Viewport Clamping")]
    public bool useViewportClamping = true;
    [Range(0f, 1f)] public float airControlMultiplier = 0.6f; // Reduced horizontal speed while in air
    [Range(0.1f, 10f)] public float xScrollLimit = 0.35f; // Boundary multiplier
    public float ScreenMaxX { get; private set; } // Right edge of screen for other scripts
    public float viewportPaddingX = 0.9f; // Balanced buffer to prevent hiding under the notch
    private Vector3 bottomLeft;
    private Vector3 topRight;
    private float levelAnchorX; // Universal anchor for the level start
    private float peakCameraX = -1000f; // Furthest right the camera edge has reached

    [Header("Visual Orientation")]
    public float rotationRight = 120f;
    public float rotationLeft = 300f; 
    public float rotationSpeed = 720f; // Speed of the turn-around pivot
    public int WorldDirection { get; private set; } = 1;
    private BoxCollider2D boxCol;
    public float CurrentScreenWidth => (topRight.x - bottomLeft.x);
    public float FullScreenWidth { get; private set; } // True edge-to-edge screen width

    [Header("Intro Settings")]
    public float introSpeed = 3f;
    public float stopX = -4f;
    private bool introFinished = false;
    private bool dead = false;
    public float externalVisualYOffset { get; set; } = 0f; // Persistent visual offset managed by LevelManager
    
    // Internal references and state
    private PlayerObstacleRules rules;
    private Animator anim;
    private float originalAbsScaleX;
    private float originalVisualLocalY;
    public float totalDistance { get; private set; } = 0f; // Cumulative distance traveled
    public float peakDistance { get; private set; } = 0f;  // Furthest distance reached
    public float DistanceGap => peakDistance - totalDistance;
    private bool mobileLeft = false;
    private bool mobileRight = false;

    void Awake()
    {
        Instance = this;
        rb = GetComponent<Rigidbody2D>();
        if (rb != null) 
        {
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation; // Lock physics rotation
        }
        
        
        rules = GetComponent<PlayerObstacleRules>();
        anim = GetComponentInChildren<Animator>(); 
        boxCol = GetComponent<BoxCollider2D>();
    }

    void Start()
    {
        currentJumpForce = jumpForce;
        jumpsLeft = maxJumps;
        originalAbsScaleX = Mathf.Abs(transform.localScale.x);
        
        // UNIVERSAL PARITY: Force camera to standard Level 1 settings
        if (Camera.main != null)
        {
            Camera.main.orthographicSize = 5f; // Standardize zoom level
        }

        // PROFESSIONAL: Initialize screen dimensions for backtracking logic
        UpdateViewportBounds();
        FullScreenWidth = topRight.x - bottomLeft.x;
        
        // Reset progress tracking to current camera position at level start
        peakCameraX = bottomLeft.x;
        
        ResetRetreatLimit(); // Set the fixed boundary for this level

        // Capture original visual height to apply offsets additively
        if (anim != null) originalVisualLocalY = anim.transform.localPosition.y;
        totalDistance = 0f;
        peakDistance = 0f;
        
        // Ensure parent rotation is locked at identity
        transform.localRotation = Quaternion.identity; 
    }

    public void ResetRetreatLimit()
    {
        // UNIVERSAL: Set the fixed world-space anchor for the current level
        levelAnchorX = transform.position.x;
        
        // Force bounds update to get fresh bottomLeft coordinates
        UpdateViewportBounds(); 
        peakCameraX = bottomLeft.x; // Initialize to current edge
        
        // Reset progress tracking to ensure consistency across all level transitions
        totalDistance = 0f;
        peakDistance = 0f;
    }

    public void SetDead(bool isDead)
    {
        dead = isDead;
        if (dead && anim != null) 
        {
            anim.SetBool("walking", false);
            anim.SetBool("Idle", true);
        }
    }

    void Update()
    {
        if (dead) return;

        // Handle Intro Sequence
        if (!introFinished)
        {
            DoIntroWalk();
            
            // Lock visuals for intro
            transform.localScale = new Vector3(originalAbsScaleX, transform.localScale.y, transform.localScale.z);
            transform.localRotation = Quaternion.identity;
            if (anim != null) anim.transform.localRotation = Quaternion.Euler(0, rotationRight, 0);

            if (anim != null) 
            {
                anim.SetBool("walking", true);
                anim.SetBool("Idle", false);
            }
            return;
        } 

        // Handle Death State
        if (rules != null && rules.IsDead) 
        {
            targetVelocityX = 0f;
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            }
            return;
        }

        // Ground check logic
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        if (isGrounded && rb.linearVelocity.y <= 0.1f)
        {
            jumpsLeft = maxJumps;
        }

        // Input processing
        float hInput = Input.GetAxisRaw("Horizontal");
        if (mobileLeft) hInput = -1f;
        if (mobileRight) hInput = 1f;

        // FIXED MOVEMENT: Removed Harmonic Agility Scaling for simpler, predictable controls
        float actualRightSpeed = moveRightSpeed;
        float actualLeftSpeed = moveLeftSpeed;
        float actualAccel = acceleration;
        float actualDecel = deceleration;

        // Smoothed velocity calculation with turn-brake logic
        float desiredVelocity = hInput * (hInput > 0 ? actualRightSpeed : actualLeftSpeed);
        
        // Detect if we are changing direction (Turning)
        bool isTurning = (desiredVelocity > 0 && currentHorizontalVelocity < 0) || 
                         (desiredVelocity < 0 && currentHorizontalVelocity > 0);
        
        // If turning, use deceleration to stop quickly first. Otherwise use acceleration/deceleration normally.
        float accelRate = isTurning ? actualDecel : (Mathf.Abs(desiredVelocity) > 0.01f ? actualAccel : actualDecel);
        
        currentHorizontalVelocity = Mathf.MoveTowards(currentHorizontalVelocity, desiredVelocity, accelRate * Time.deltaTime);
        targetVelocityX = currentHorizontalVelocity;

        // Dynamic animator fetch for multiple character skins
        if (anim == null || !anim.gameObject.activeInHierarchy)
            anim = GetComponentInChildren<Animator>();

        // Animation state management (Locked to Idle when stuck)
        if (anim != null)
        {
            bool isStuck = (rules != null && rules.IsStuck);
            bool isMovingInput = (hInput != 0) && !isStuck;
            anim.SetBool("walking", isMovingInput);
            anim.SetBool("Idle", !isMovingInput || isStuck);
        }
        
        // Direction and Rotation management (Synchronized with velocity to prevent moonwalking)
        if (hInput > 0 && currentHorizontalVelocity > -0.5f) 
        {
            WorldDirection = 1;
        }
        else if (hInput < 0 && currentHorizontalVelocity < 0.5f) 
        {
            WorldDirection = -1;
        }

        // Explicit transform and child rotation enforcement
        transform.localScale = new Vector3(originalAbsScaleX, transform.localScale.y, transform.localScale.z);
        transform.localRotation = Quaternion.identity;

        if (anim != null)
        {
            float targetRY = (WorldDirection == 1) ? rotationRight : rotationLeft;
            Quaternion targetRot = Quaternion.Euler(0, targetRY, 0);
            
            // Smoothly rotate towards the target direction
            anim.transform.localRotation = Quaternion.RotateTowards(
                anim.transform.localRotation, 
                targetRot, 
                rotationSpeed * Time.deltaTime
            );
        }

        // Jump input handling
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            MobileJumpDown();
        }
    }

    void FixedUpdate()
    {
        UpdateViewportBounds();
        
        // UNIFIED Movement logic: Calculate desired step
        float distanceStep = targetVelocityX * GameSpeed.Multiplier * Time.fixedDeltaTime;
        
        // Prevent distance tracking from going negative BEYOND what's visually possible at the screen edge
        // This ensures the "Stuck" state only triggers if the world flow truly pushes them back.
        if (transform.position.x <= bottomLeft.x + viewportPaddingX + 0.1f && distanceStep < 0)
        {
            distanceStep = 0; // Don't accumulate "backtrack" if already at the visual edge
        }

        totalDistance += distanceStep;
        if (totalDistance > peakDistance) peakDistance = totalDistance;

        if (!introFinished || (rules != null && rules.IsDead)) return;

        float finalVelocityX = targetVelocityX;

        // Apply Air Control: Reduce horizontal speed if in the air to prevent "flying"
        if (!isGrounded)
        {
            finalVelocityX *= airControlMultiplier;
        }

        // Strict Velocity Lock: No movement allowed if strictly stuck or in recovery
        if (rules != null && rules.IsHorizontalBlocked)
        {
            if (finalVelocityX > 0) finalVelocityX = 0f;
            if (rules.IsStuck && finalVelocityX > 0) finalVelocityX = 0f;
        }

        // Distance-Based Retreat Limit: Exactly 1 FULL screen width behind furthest progress
        if (DistanceGap >= FullScreenWidth && finalVelocityX < 0) finalVelocityX = 0;
        
        // Viewport clamping (for visual boundaries)
        if (transform.position.x >= maxX && finalVelocityX > 0) finalVelocityX = 0;

        rb.linearVelocity = new Vector2(finalVelocityX, rb.linearVelocity.y);

        // BETTER JUMP Mekaniği: Havada asılı kalmayı önler ve düşüşü hızlandırır
        if (rb.linearVelocity.y < 0) // Kedi aşağı düşüyorsa
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }
        else if (rb.linearVelocity.y > 0 && !(Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.UpArrow) || mobileRight || mobileLeft)) 
        {
            // Kedi yukarı çıkıyor ama zıplama tuşu bırakıldıysa (Kısa zıplama)
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;
        }

        // Position enforcement for hard screen boundaries (X and Y)
        // Note: Distance clamping only limits VELOCITY to prevent backtracking.
        // Screen position clamping handles the visuals.
        if (transform.position.x > maxX)
        {
            rb.position = new Vector2(maxX, rb.position.y);
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }

    void LateUpdate()
    {
        UpdateViewportBounds();
        
        if (!useViewportClamping) return;
        float currentMinY = bottomLeft.y;
        float clampedX = Mathf.Clamp(transform.position.x, minX, maxX);
        
        float effectiveMinY = bottomLeft.y;
        if (LevelManager.Instance != null)
        {
            if (LevelManager.Instance.currentLevel == 5) effectiveMinY = -20f;
            else effectiveMinY = -3.7f;
        }
        
        float clampedY = Mathf.Clamp(transform.position.y, effectiveMinY, topRight.y);

        if (clampedX != transform.position.x || clampedY != transform.position.y)
        {
            transform.position = new Vector3(clampedX, clampedY, transform.position.z);
            if (clampedX != transform.position.x) rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }

         if (LevelManager.Instance != null && LevelManager.Instance.currentLevel <= 4)
        {
            if (transform.position.y < -3.7f)
            {
                transform.position = new Vector3(transform.position.x, -3.7f, transform.position.z);
                if (rb.linearVelocity.y < 0) rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
            }
        }

         if (anim != null)
        {
            Vector3 visualPos = anim.transform.localPosition;
            visualPos.x = 0f; // Force visual to stay centered on the collider/pivot
            visualPos.y = originalVisualLocalY + externalVisualYOffset;
            anim.transform.localPosition = visualPos;
        }

        if (boxCol != null && LevelManager.Instance != null && LevelManager.Instance.currentLevel <= 4)
        {
           boxCol.size = new Vector2(1.050137f, 1.623463f);
            
            // Mirror the horizontal offset based on facing direction
            float dynamicOffsetX = 0.1874768f * WorldDirection;
            boxCol.offset = new Vector2(dynamicOffsetX, 0.6628802f);
        }
    }

    void DoIntroWalk()
    {
        float step = introSpeed * Time.deltaTime;
        Vector3 targetPos = new Vector3(stopX, transform.position.y, transform.position.z);
        transform.position = Vector3.MoveTowards(transform.position, targetPos, step);

        if (Mathf.Abs(transform.position.x - stopX) < 0.1f || transform.position.x >= maxX - 0.1f)
        {
            introFinished = true;
        }
    }

    public void ResetJumps()
    {
        jumpsLeft = maxJumps;
    }

    public void TryJump()
    {
        if (jumpsLeft <= 0) return;  

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * currentJumpForce, ForceMode2D.Impulse);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayJump();

        jumpsLeft--;
    }

    public void SetJumpMultiplier(float multiplier) => currentJumpForce = jumpForce * multiplier;
    public void ResetJumpMultiplier() => currentJumpForce = jumpForce;

    // Mobile input flags management
    public void SetMoveLeft(bool isMoving) { mobileLeft = isMoving; }
    public void SetMoveRight(bool isMoving) { mobileRight = isMoving; }
    
    public bool IsMovingLeft => Input.GetKey(KeyCode.LeftArrow) || mobileLeft;
    public bool IsMovingRight => Input.GetKey(KeyCode.RightArrow) || mobileRight;
    
    public float CurrentVelocityX => currentHorizontalVelocity;
    
    public void MobileJumpDown()
    {
        if (dead) return;
        
        // Level 5 specific check for pit depths
        bool canJump = true;
        if (LevelManager.Instance != null && LevelManager.Instance.currentLevel == 5)
        {
            if (transform.position.y < deathThresholdY) canJump = false;
        }

        if (canJump) TryJump();
        else if (AudioManager.Instance != null) AudioManager.Instance.PlayFalling();
    }

    private void UpdateViewportBounds()
    {
        Camera cam = Camera.main;
        if (cam == null) cam = Object.FindFirstObjectByType<Camera>();
        if (cam == null) return;

        float zDist = Mathf.Abs(cam.transform.position.z);
        
        // Dinamik olarak ekran kenarlarını dünya koordinatlarına çeviriyoruz
        Vector3 trueLeft = cam.ViewportToWorldPoint(new Vector3(0, 0, zDist));
        bottomLeft = trueLeft; // LateUpdate içinde Y ekseni kısıtlaması için gerekli
        Vector3 trueRight = cam.ViewportToWorldPoint(new Vector3(1, 1, zDist));
        Vector3 trueCenter = cam.ViewportToWorldPoint(new Vector3(0.5f, 0, zDist)); // Ekranın tam ortası

        FullScreenWidth = trueRight.x - trueLeft.x;
        ScreenMaxX = trueRight.x;

        // MARIO TARZI SINIRLAR:
        // minX: Sol ekran kenarı + padding
        // maxX: Ekranın tam ortası (0.5 Viewport)
        
        float effectivePadding = Mathf.Max(viewportPaddingX, 0.7f);
        
        minX = trueLeft.x + effectivePadding;
        maxX = trueCenter.x; // Tam orta nokta

        // Güvenlik: minX, maxX'ten büyük olamaz
        if (minX > maxX - 0.5f) minX = maxX - 0.5f;
    }

    public void ResetHorizontalVelocity()
    {
        currentHorizontalVelocity = 0f;
        targetVelocityX = 0f;
        if (rb != null) rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
    }
}
