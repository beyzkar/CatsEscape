using UnityEngine;

// Core player movement controller: handles horizontal movement, jumping, and rotation/flipping
public class PlayerMovement : MonoBehaviour
{
    private const float FixedLeftBoundaryX = -9f;
    private const float LeftBoundaryEpsilon = 0.01f;

    public static PlayerMovement Instance { get; private set; }

    [Header("Jump Settings")]
    public float jumpForce = 12f;
    public float fallMultiplier = 2.5f; 
    public float lowJumpMultiplier = 2f; // Bunu geri ekliyorum, FixedUpdate içinde kullanılıyor
    private float currentJumpMultiplier = 1f;
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
    public float minX = FixedLeftBoundaryX; // Fixed world left boundary
    public float maxX = 5f;    // Initial right boundary
    public float acceleration = 5f;
    public float deceleration = 10f;
    public float stoppingDeceleration = 60f; // High rate for instant stops when input is released
    [Range(0.1f, 1f)] public float groundedSpeedMultiplier = 0.75f; // Global horizontal speed reduction on ground
    [Range(0.1f, 1f)] public float airAccelerationMultiplier = 0.55f; // Prevents building excessive X speed in air
    [Range(0.01f, 0.5f)] public float landingVelocityBlendTime = 0.12f; // Smooths airborne -> grounded transition
    private float currentHorizontalVelocity = 0f;
    private float targetVelocityX = 0f;
    private float appliedHorizontalVelocity = 0f;

    [Header("Viewport Clamping")]
    public bool useViewportClamping = true;
[Range(0f, 1f)] public float airControlMultiplier = 0.5f; // Scales horizontal speed while airborne
[Range(0f, 1f)] public float jumpTakeoffHorizontalMultiplier = 0.7f; // Damp X speed exactly when jump starts
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
    public Transform levelStartPoint;
    public float levelStartProbeHeight = 3f;
    public float levelStartProbeDistance = 15f;
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
    private bool wasAtRetreatLimit = false;
    private bool jumpDisabled = false;
    private bool isLevelEnding = false;
    private bool portalExitSequenceActive = false;
    // Keep a single global retreat wall for all levels.
    private float CurrentLeftBoundaryX => FixedLeftBoundaryX;

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

        if (portalExitSequenceActive)
            return;

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

        // Ground check logic: Consider both physics overlap and the manual Y-clamp as grounded.
        bool physicsGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        bool clampGrounded = (LevelManager.Instance != null && LevelManager.Instance.currentLevel <= 4 && transform.position.y <= -3.69f);
        
        isGrounded = physicsGrounded || clampGrounded;

        // Reset jumps immediately upon touching the ground. 
        // We allow a small upward velocity buffer (0.1f) to ensure the reset happens 
        // even if the character jumps and lands in very rapid succession.
        if (isGrounded && rb.linearVelocity.y <= 0.1f)
        {
            ResetJumps();
        }

        // Input processing
        float hInput = Input.GetAxisRaw("Horizontal");
        if (isLevelEnding)
        {
            hInput = 1f; // Force move right automatically
        }
        else
        {
            if (mobileLeft) hInput = -1f;
            if (mobileRight) hInput = 1f;
        }

        // FIXED MOVEMENT: Removed Harmonic Agility Scaling for simpler, predictable controls
        float actualRightSpeed = moveRightSpeed;
        float actualLeftSpeed = moveLeftSpeed;
        float actualAccel = acceleration;
        float actualDecel = deceleration;

        // Smoothed velocity calculation with smart deceleration
        float desiredVelocity = hInput * (hInput > 0 ? actualRightSpeed : actualLeftSpeed);
        
        // Differentiate between actively turning around and simply stopping
        bool isStopping = (hInput == 0);
        bool isTurning = (desiredVelocity > 0 && currentHorizontalVelocity < 0) || 
                         (desiredVelocity < 0 && currentHorizontalVelocity > 0);
        
        // Determine the rates: Stopping is immediate, Turning is snappy but fluid
        float currentDecelRate = isStopping ? stoppingDeceleration : deceleration;
        float accelRate = isTurning ? (deceleration * 2f) : (Mathf.Abs(desiredVelocity) > 0.01f ? actualAccel : currentDecelRate);
        
        if (!isGrounded) accelRate *= airAccelerationMultiplier;
        
        currentHorizontalVelocity = Mathf.MoveTowards(currentHorizontalVelocity, desiredVelocity, accelRate * Time.deltaTime);
        
        // SNAP TO ZERO: Prevent micro-sliding when input is released
        if (isStopping && Mathf.Abs(currentHorizontalVelocity) < 0.01f)
            currentHorizontalVelocity = 0f;

        targetVelocityX = currentHorizontalVelocity;

        // Dynamic animator fetch for multiple character skins
        if (anim == null || !anim.gameObject.activeInHierarchy)
            anim = GetComponentInChildren<Animator>();

        // Retro (Geriye Gidiş) sınırı: fixed world limit (minX) kullanılmalı.
        bool isAtRetreatLimit = (transform.position.x <= CurrentLeftBoundaryX && hInput < 0);
        
        if (isAtRetreatLimit)
        {
            // On first contact with the left retreat boundary, immediately refresh jump charges.
            if (!wasAtRetreatLimit) ResetJumps();

            GameSpeed.Multiplier = 0f; // Dünyayı dondur
            currentHorizontalVelocity = 0f;
            targetVelocityX = 0f;
            appliedHorizontalVelocity = 0f;
        }
        else
        {
            GameSpeed.Multiplier = 1f; // Dünyayı çöz
        }

        // If we were at the left retreat limit and the player starts moving right,
        // or is pushing right near the wall, immediately and continuously restore jump availability.
        // This ensures the player can always jump out of the corner reliably.
        if (transform.position.x <= (CurrentLeftBoundaryX + 0.1f) && hInput > 0.1f)
        {
            ResetJumps();
        }
        wasAtRetreatLimit = isAtRetreatLimit;

        // Animation state management
        if (anim != null)
        {
            bool isStuck = (rules != null && rules.IsStuck) || isAtRetreatLimit;
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

        // Jump input handling (keyboard + tap/click)
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetMouseButtonDown(0))
        {
            MobileJumpDown();
        }
    }

    void FixedUpdate()
    {
        UpdateViewportBounds();

        // 1. KİLİT MEKANİZMASI: Mesafe hesaplanmadan önce hızları sıfırlıyoruz (Mikro kaymayı önler)
        if (rules != null && rules.IsHorizontalBlocked && targetVelocityX > 0)
        {
            targetVelocityX = 0f;
            currentHorizontalVelocity = 0f;
            appliedHorizontalVelocity = 0f;
        }
        
        if (transform.position.x <= CurrentLeftBoundaryX && targetVelocityX < 0)
        {
            targetVelocityX = 0f;
            currentHorizontalVelocity = 0f;
            appliedHorizontalVelocity = 0f;
        }

        if (!introFinished || (rules != null && rules.IsDead)) return;

        if (portalExitSequenceActive) return;

        // 2. HAREKET HESABI (Sıfırlanmış değerler üzerinden)
        float distanceStep = targetVelocityX * GameSpeed.Multiplier * Time.fixedDeltaTime;
        
        // Use the fixed world left boundary (minX) so retreat limit matches design exactly.
        if (transform.position.x <= CurrentLeftBoundaryX && distanceStep < 0)
        {
            distanceStep = 0; 
        }

        totalDistance += distanceStep;
        if (totalDistance > peakDistance) peakDistance = totalDistance;

        float finalVelocityX = targetVelocityX * groundedSpeedMultiplier;

        // Apply air control while genuinely airborne (including takeoff/landing frames).
        bool isAirborneForControl = !isGrounded || Mathf.Abs(rb.linearVelocity.y) > 0.05f;
        if (isAirborneForControl) finalVelocityX *= airControlMultiplier;

        // Smoothly blend applied horizontal velocity to prevent "rocket" bursts on landing.
        float blendDuration = Mathf.Max(landingVelocityBlendTime, 0.01f);
        
        // Use stoppingDeceleration in physics blend for instant stopping feel
        float currentPhysicsDecel = (Mathf.Abs(finalVelocityX) < 0.01f) ? stoppingDeceleration : deceleration;
        
        float blendRate = (Mathf.Abs(finalVelocityX) > 0.01f)
            ? Mathf.Abs(finalVelocityX - appliedHorizontalVelocity) / blendDuration
            : currentPhysicsDecel;
        appliedHorizontalVelocity = Mathf.MoveTowards(appliedHorizontalVelocity, finalVelocityX, blendRate * Time.fixedDeltaTime);

        // 3. FİNAL HIZ KISITLAMALARI (Viewport ve Limitler)
        if (transform.position.x >= maxX && appliedHorizontalVelocity > 0) appliedHorizontalVelocity = 0f;
        
        rb.linearVelocity = new Vector2(appliedHorizontalVelocity, rb.linearVelocity.y);

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
            appliedHorizontalVelocity = 0f;
        }
    }

    void LateUpdate()
    {
        UpdateViewportBounds();

        if (portalExitSequenceActive) return;
        
        if (!useViewportClamping) return;
        
        // Sadece X ekseninde (Sağ-Sol) kısıtlama uyguluyoruz
        float clampedX = Mathf.Clamp(transform.position.x, minX, maxX);
        
        if (clampedX != transform.position.x)
        {
            transform.position = new Vector3(clampedX, transform.position.y, transform.position.z);
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
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

        // Keep jump height unchanged, but reduce horizontal carry-over at takeoff.
        rb.linearVelocity = new Vector2(rb.linearVelocity.x * jumpTakeoffHorizontalMultiplier, 0f);
        // Doğrudan güncel jumpForce değerini kullanıyoruz
        rb.AddForce(Vector2.up * (jumpForce * currentJumpMultiplier), ForceMode2D.Impulse);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayJump();

        jumpsLeft--;
    }

    public void SetJumpMultiplier(float multiplier) => currentJumpMultiplier = multiplier;
    public void ResetJumpMultiplier() => currentJumpMultiplier = 1f;

    // Mobile input flags management
    public void SetMoveLeft(bool isMoving) { mobileLeft = isMoving; }
    public void SetMoveRight(bool isMoving) { mobileRight = isMoving; }
    
    public bool IsMovingLeft => Input.GetKey(KeyCode.LeftArrow) || mobileLeft;
    public bool IsMovingRight => Input.GetKey(KeyCode.RightArrow) || mobileRight;
    
    public float CurrentVelocityX => (rules != null && (rules.IsHorizontalBlocked || GameSpeed.Multiplier <= 0.01f) && currentHorizontalVelocity > 0) ? 0f : currentHorizontalVelocity;
    
    public void MobileJumpDown()
    {
        if (dead || jumpDisabled || portalExitSequenceActive) return;
        
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
        topRight = cam.ViewportToWorldPoint(new Vector3(1, 1, zDist)); // Genişlik hesaplamaları için güncelliyoruz
        Vector3 trueRight = topRight;
        Vector3 trueCenter = cam.ViewportToWorldPoint(new Vector3(0.5f, 0, zDist)); // Ekranın tam ortası

        FullScreenWidth = trueRight.x - trueLeft.x;
        ScreenMaxX = trueRight.x;

        float effectivePadding = Mathf.Max(viewportPaddingX, 0.7f);
        
        // Final Sınırlar: Sol sabit tuned value, Sağ ekranın tam ortası
        minX = FixedLeftBoundaryX; 
        maxX = trueCenter.x; 

        // Keep left world boundary fixed; only push right boundary if needed.
        if (maxX < minX + 0.5f) maxX = minX + 0.5f;
    }

    public void ResetHorizontalVelocity()
    {
        currentHorizontalVelocity = 0f;
        targetVelocityX = 0f;
        appliedHorizontalVelocity = 0f;
        if (rb != null) rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
    }

    public void PrepareForLevelStart()
    {
        float targetX = (levelStartPoint != null) ? levelStartPoint.position.x : stopX;
        float probeBaseY = (levelStartPoint != null) ? levelStartPoint.position.y : transform.position.y;
        float targetY = probeBaseY;

        Collider2D playerCol = GetComponent<Collider2D>();
        float halfHeight = (playerCol != null) ? playerCol.bounds.extents.y : 0.5f;
        Vector2 probeOrigin = new Vector2(targetX, probeBaseY + Mathf.Max(1f, levelStartProbeHeight));
        RaycastHit2D hit = Physics2D.Raycast(probeOrigin, Vector2.down, Mathf.Max(2f, levelStartProbeDistance), groundLayer);

        if (hit.collider != null)
        {
            targetY = hit.point.y + halfHeight + 0.02f;
        }
        else if (LevelManager.Instance != null && LevelManager.Instance.currentLevel <= 4)
        {
            targetY = -3.7f;
        }

        Vector2 spawnPosition = new Vector2(targetX, targetY);
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.position = spawnPosition;
        }
        else
        {
            transform.position = new Vector3(targetX, targetY, transform.position.z);
        }

        currentHorizontalVelocity = 0f;
        targetVelocityX = 0f;
        appliedHorizontalVelocity = 0f;
        mobileLeft = false;
        mobileRight = false;
        wasAtRetreatLimit = false;
        dead = false;
        isGrounded = true;
        introFinished = true;
        WorldDirection = 1;
        jumpDisabled = false;
        isLevelEnding = false;
        ResetJumps();

        if (anim != null)
        {
            anim.SetBool("walking", false);
            anim.SetBool("Idle", true);
            anim.transform.localRotation = Quaternion.Euler(0, rotationRight, 0);
        }

        transform.localRotation = Quaternion.identity;
        transform.localScale = new Vector3(originalAbsScaleX, transform.localScale.y, transform.localScale.z);
        ResetRetreatLimit();
    }
    public void FreezeForTransition(bool freeze)
    {
        if (freeze)
        {
            ResetHorizontalVelocity();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.linearVelocity = Vector2.zero;
            }
            
            if (anim != null)
            {
                anim.SetBool("walking", false);
                anim.SetBool("Idle", true);
            }
        }
        else
        {
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
            }
        }
    }

    public void DisableJumping() => jumpDisabled = true;
    public void StartLevelEndWalk() => isLevelEnding = true;

    public void SetPortalExitSequenceActive(bool active) => portalExitSequenceActive = active;

    public void SetPortalDrivenWorldPosition(Vector3 worldPos)
    {
        if (rb != null)
            rb.position = worldPos;
        else
            transform.position = worldPos;
    }
}
