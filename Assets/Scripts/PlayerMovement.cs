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
    public float lowJumpMultiplier = 2f;
    private float currentJumpMultiplier = 1f;
    public int maxJumps = 2; 
    private int jumpsLeft;

    [Header("Level 5 Fall Settings")]
    public float deathThresholdY = -4.2f; // Height at which jumping is disabled in Level 5

    [Header("Ground Detection")]
    public Transform groundCheck; // Point used to detect if the player is on the ground
    public float groundCheckRadius = 0.15f;
    public LayerMask groundLayer; // Specifies which layer is ground

    private Rigidbody2D rb; // Rigidbody2D: Adds physics to the object
    private bool isGrounded;
    public bool IsGrounded => isGrounded;

    [Header("Horizontal Movement")]
    public float moveRightSpeed = 9f; // Increased from 7.5f
    public float moveLeftSpeed = 6.5f; // Increased from 5f
    public float minX = FixedLeftBoundaryX; // Fixed world left boundary
    public float maxX = 5f;    // Initial right boundary
    public float acceleration = 5f;
    public float deceleration = 10f;
    public float stoppingDeceleration = 60f; // High rate for instant stops when input is released
    [Range(0.1f, 1f)] public float groundedSpeedMultiplier = 0.5f; // Set to 0.5 so Cat(0.5) + World(0.5) = 1.0 (Max Speed)
    [Range(0.1f, 1f)] public float airAccelerationMultiplier = 1.0f; // REMOVED acceleration delay in air for consistency
    [Range(0.01f, 0.5f)] public float landingVelocityBlendTime = 0.12f; // Smooths airborne -> grounded transition
    private float currentHorizontalVelocity = 0f;
    private float targetVelocityX = 0f;
    private float appliedHorizontalVelocity = 0f;

    [Header("Viewport Clamping")]
    public bool useViewportClamping = true;
    public float airControlMultiplier = 0.5f; // Set to 0.5 so Cat(0.5) + World(0.5) = 1.0 (Max Speed)
    public float jumpTakeoffHorizontalMultiplier = 1.0f; // REMOVED jump speed dampening
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
    
    private Camera cam;
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
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
        
        rules = GetComponent<PlayerObstacleRules>();
        anim = GetComponentInChildren<Animator>(); 
        boxCol = GetComponent<BoxCollider2D>();
    }

    void Start()
    {
        jumpsLeft = maxJumps;
        originalAbsScaleX = Mathf.Abs(transform.localScale.x);
        
        if (Camera.main != null) cam = Camera.main;

        UpdateViewportBounds();
        FullScreenWidth = topRight.x - bottomLeft.x;
        peakCameraX = bottomLeft.x;
        
        ResetRetreatLimit();

        if (anim != null) originalVisualLocalY = anim.transform.localPosition.y;
        totalDistance = 0f;
        peakDistance = 0f;
        transform.localRotation = Quaternion.identity; 
        
        // Ensure player starts at X = -6 and Idle on every scene load (Retry, Level 1, etc.)
        PrepareForLevelStart();
    }

    public void ResetRetreatLimit()
    {
        levelAnchorX = transform.position.x;
        UpdateViewportBounds(); 
        peakCameraX = bottomLeft.x;
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
        UpdateViewportBounds();
        if (dead) return;

        if (portalExitSequenceActive) return;

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

        // --- INTRO & MOVEMENT LOGIC ---
        float hInput = 0f;
        bool jumpInput = Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space);

        if (!introFinished)
        {
            // Auto-move right during intro
            hInput = 1f;
            
            // Check if we reached stop position
            if (transform.position.x >= stopX)
            {
                introFinished = true;
            }
        }
        else
        {
            // Normal Input
            hInput = Input.GetAxisRaw("Horizontal");
            if (isLevelEnding) hInput = 1f;
            else
            {
                if (mobileLeft) hInput = -1f;
                if (mobileRight) hInput = 1f;
            }
        }

        bool physicsGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        bool clampGrounded = (LevelManager.Instance != null && LevelManager.Instance.currentLevel <= 4 && transform.position.y <= -3.69f);
        isGrounded = physicsGrounded || clampGrounded;

        if (isGrounded && rb.linearVelocity.y <= 0.1f) ResetJumps();

        // Speed during intro is forced to introSpeed, otherwise use moveRight/LeftSpeed
        float speedMultiplier = !introFinished ? (introSpeed / moveRightSpeed) : 1f;
        float desiredVelocity = hInput * (hInput > 0 ? moveRightSpeed : moveLeftSpeed) * speedMultiplier;

        targetVelocityX = desiredVelocity;
        currentHorizontalVelocity = targetVelocityX; 

        // Dynamic animator fetch for multiple character skins
        if (anim == null || !anim.gameObject.activeInHierarchy)
            anim = GetComponentInChildren<Animator>();

        bool isAtRetreatLimit = (transform.position.x <= CurrentLeftBoundaryX && hInput < 0);
        
        if (isAtRetreatLimit)
        {
            if (!wasAtRetreatLimit) ResetJumps();

            GameSpeed.Multiplier = 0f; // Dünyayı dondur
            currentHorizontalVelocity = 0f;
            targetVelocityX = 0f;
            appliedHorizontalVelocity = 0f;
        }
        else
        {
            // Reset to base speed only if we were frozen at the retreat limit and are now moving away
            // Start gameplay movement ONLY when player provides input (Keyboard or Mobile)
            bool hasMovementInput = (hInput != 0) || jumpInput || IsMovingLeft || IsMovingRight || (rb.linearVelocity.y > 0.1f);
            if (GameSpeed.Multiplier < 0.01f && !stuckByRules() && introFinished && hasMovementInput) 
            {
                float baseSpeed = (LevelManager.Instance != null) ? LevelManager.Instance.GetCurrentBaseSpeed() : 1f;
                GameSpeed.Multiplier = baseSpeed;
                if (LevelManager.Instance != null) LevelManager.Instance.SetTargetSpeed(baseSpeed);
            }
        }

        if (transform.position.x <= (CurrentLeftBoundaryX + 0.1f) && hInput > 0.1f)
        {
            ResetJumps();
        }
        wasAtRetreatLimit = isAtRetreatLimit;
        if (anim != null)
        {
            bool isStuck = (rules != null && rules.IsStuck) || isAtRetreatLimit;
            bool isMovingInput = (hInput != 0) && !isStuck;
            anim.SetBool("walking", isMovingInput);
            anim.SetBool("Idle", !isMovingInput || isStuck);
        }
        
        if (hInput > 0 && currentHorizontalVelocity > -0.5f) WorldDirection = 1;
        else if (hInput < 0 && currentHorizontalVelocity < 0.5f) WorldDirection = -1;

        transform.localScale = new Vector3(originalAbsScaleX, transform.localScale.y, transform.localScale.z);
        transform.localRotation = Quaternion.identity;

        if (anim != null)
        {
            float targetRY = (WorldDirection == 1) ? rotationRight : rotationLeft;
            anim.transform.localRotation = Quaternion.RotateTowards(anim.transform.localRotation, Quaternion.Euler(0, targetRY, 0), rotationSpeed * Time.deltaTime);
        }

        if (jumpInput && introFinished) MobileJumpDown(); // No jumping during intro
    }

    private bool stuckByRules() => rules != null && rules.IsStuck;

    void FixedUpdate()
    {
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

        if (rules != null && rules.IsDead) return;
        if (portalExitSequenceActive) return;

        float distanceStep = (CurrentVelocityX + Mathf.Max(0, appliedHorizontalVelocity)) * GameSpeed.Multiplier * Time.fixedDeltaTime;
        if (transform.position.x <= CurrentLeftBoundaryX && distanceStep < 0) distanceStep = 0; 

        totalDistance += distanceStep;
        if (totalDistance > peakDistance) peakDistance = totalDistance;

        float finalVelocityX = targetVelocityX;
        bool isAirborneForControl = !isGrounded || Mathf.Abs(rb.linearVelocity.y) > 0.05f;
        
        if (isAirborneForControl) 
        {
            finalVelocityX *= airControlMultiplier;
        }
        else 
        {
            finalVelocityX *= groundedSpeedMultiplier;
        }

        // --- SMOOTH MOVEMENT CALCULATION ---
        bool isStopping = (targetVelocityX == 0);
        bool isTurning = (targetVelocityX > 0 && appliedHorizontalVelocity < 0) || (targetVelocityX < 0 && appliedHorizontalVelocity > 0);
        float currentDecelRate = isStopping ? stoppingDeceleration : deceleration;
        float accelRate = isTurning ? (deceleration * 2f) : (Mathf.Abs(targetVelocityX) > 0.01f ? acceleration : currentDecelRate);
        
        appliedHorizontalVelocity = Mathf.MoveTowards(appliedHorizontalVelocity, finalVelocityX, accelRate * Time.fixedDeltaTime);

        // Strict capping to prevent any buildup
        appliedHorizontalVelocity = Mathf.Clamp(appliedHorizontalVelocity, -moveLeftSpeed * 0.5f, moveRightSpeed * 0.5f);

        float physicsVelocityX = appliedHorizontalVelocity;
        
        // --- RIGHT BOUNDARY (maxX) VELOCITY CLAMPING ---
        if (transform.position.x >= maxX && physicsVelocityX > 0)
        {
            physicsVelocityX = 0f;
        }

        rb.linearVelocity = new Vector2(physicsVelocityX, rb.linearVelocity.y);

        if (rb.linearVelocity.y < 0)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }
        else if (rb.linearVelocity.y > 0 && !(Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.Space))) 
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;
        }

        if (transform.position.x > maxX + 0.05f)
        {
            rb.position = new Vector2(maxX, rb.position.y);
        }
    }

    void LateUpdate()
    {
        if (portalExitSequenceActive) return;
        if (!useViewportClamping) return;
        
        if (transform.position.x > maxX + 0.05f || transform.position.x < minX - 0.05f)
        {
            float clampedX = Mathf.Clamp(transform.position.x, minX, maxX);
            transform.position = new Vector3(clampedX, transform.position.y, transform.position.z);
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
            anim.transform.localPosition = new Vector3(0f, originalVisualLocalY + externalVisualYOffset, 0f);
        }

        if (boxCol != null && LevelManager.Instance != null && LevelManager.Instance.currentLevel <= 4)
        {
            boxCol.size = new Vector2(1.050137f, 1.623463f);
            boxCol.offset = new Vector2(0.1874768f * WorldDirection, 0.6628802f);
        }
    }

    public void ResetJumps() => jumpsLeft = maxJumps;
    public void TryJump()
    {
        if (jumpsLeft <= 0) return;  
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f); // Reset Y only, preserve X for constant speed
        rb.AddForce(Vector2.up * (jumpForce * currentJumpMultiplier), ForceMode2D.Impulse);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayJump();
        jumpsLeft--;
    }

    public void SetJumpMultiplier(float multiplier) => currentJumpMultiplier = multiplier;
    public void ResetJumpMultiplier() => currentJumpMultiplier = 1f;

    public void SetMoveLeft(bool isMoving) { mobileLeft = isMoving; }
    public void SetMoveRight(bool isMoving) { mobileRight = isMoving; }
    
    public bool IsMovingLeft => Input.GetKey(KeyCode.LeftArrow) || mobileLeft;
    public bool IsMovingRight => Input.GetKey(KeyCode.RightArrow) || mobileRight;
    
    public float CurrentVelocityX
    {
        get
        {
            if (rules != null && (rules.IsHorizontalBlocked || GameSpeed.Multiplier <= 0.01f)) return 0f;
            
            float baseV = appliedHorizontalVelocity;

            // --- RELATIVE VELOCITY COMPENSATION (Surgical Fix) ---
            // Formula: TotalRelativeSpeed = CatVelocityOnScreen + WorldScrollSpeed
            // WorldScrollSpeed = CurrentVelocityX * GameSpeed.Multiplier
            // Goal: Relative speed before limit (V + V*M) must equal relative speed at limit (0 + V_Compensated * M).
            // Solving (V + VM) = V_Comp * M  =>  V_Comp = V * (1 + 1/M)
            if (transform.position.x >= maxX && baseV > 0)
            {
                float compensatedV = baseV * (1f + (1f / GameSpeed.Multiplier));
                
                // Debug log only at the limit (guarded)
                if (transform.position.x >= maxX + 0.01f && Time.frameCount % 100 == 0)
                {
                    Debug.Log($"[SPEED_CLAMP] Level: {LevelManager.Instance?.currentLevel}, " +
                              $"CatV: {baseV:F2}, M: {GameSpeed.Multiplier:F2}, " +
                              $"CompV: {compensatedV:F2}, FinalWorldV: {compensatedV * GameSpeed.Multiplier:F2}");
                }
                
                return compensatedV;
            }

            return baseV;
        }
    }
    
    public void MobileJumpDown()
    {
        if (dead || jumpDisabled || portalExitSequenceActive) return;
        bool canJump = true;
        if (LevelManager.Instance != null && LevelManager.Instance.currentLevel == 5 && transform.position.y < deathThresholdY) canJump = false;

        if (canJump) TryJump();
        else if (AudioManager.Instance != null) AudioManager.Instance.PlayFalling();
    }

    private void UpdateViewportBounds()
    {
        Camera cam = Camera.main;
        if (cam == null) cam = Object.FindFirstObjectByType<Camera>();
        if (cam == null) return;

        float zDist = Mathf.Abs(cam.transform.position.z);
        Vector3 trueLeft = cam.ViewportToWorldPoint(new Vector3(0, 0, zDist));
        bottomLeft = trueLeft; 
        topRight = cam.ViewportToWorldPoint(new Vector3(1, 1, zDist));
        Vector3 trueCenter = cam.ViewportToWorldPoint(new Vector3(0.5f, 0, zDist));

        FullScreenWidth = topRight.x - trueLeft.x;
        ScreenMaxX = topRight.x;

        minX = FixedLeftBoundaryX; 
        maxX = trueCenter.x; 

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
        int levelNum = (LevelManager.Instance != null) ? LevelManager.Instance.currentLevel : 0;
        Debug.Log($"[SPAWN_START] Level: {levelNum}, Position Before: {transform.position}");

        float targetX = -4f; 
        float probeBaseY = (levelStartPoint != null) ? levelStartPoint.position.y : transform.position.y;
        float targetY = probeBaseY;

        Collider2D playerCol = GetComponent<Collider2D>();
        float halfHeight = (playerCol != null) ? playerCol.bounds.extents.y : 0.5f;
        RaycastHit2D hit = Physics2D.Raycast(new Vector2(targetX, probeBaseY + Mathf.Max(1f, levelStartProbeHeight)), Vector2.down, Mathf.Max(2f, levelStartProbeDistance), groundLayer);

        if (hit.collider != null) targetY = hit.point.y + halfHeight + 0.02f;
        else if (LevelManager.Instance != null && LevelManager.Instance.currentLevel <= 4) targetY = -3.7f;

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.linearVelocity = Vector2.zero; // RB Velocity Reset
            rb.position = new Vector2(targetX, targetY);
        }
        else transform.position = new Vector3(targetX, targetY, transform.position.z);

        currentHorizontalVelocity = 0f;
        targetVelocityX = 0f;
        appliedHorizontalVelocity = 0f;
        mobileLeft = mobileRight = wasAtRetreatLimit = dead = false;
        
        // --- Intro Reset ---
        // Spawn exactly at -6f with no offset or intro walk
        introFinished = true; 
        isGrounded = true;
        
        WorldDirection = 1;
        jumpDisabled = isLevelEnding = false;
        ResetJumps();

        if (anim != null)
        {
            anim.SetBool("walking", false); 
            anim.SetBool("Idle", true);
            anim.Play("Idle", 0, 0f); // Force immediate transition to Idle state
            anim.transform.localRotation = Quaternion.Euler(0, rotationRight, 0);
        }

        transform.localRotation = Quaternion.identity;
        transform.localScale = new Vector3(originalAbsScaleX, transform.localScale.y, transform.localScale.z);
        ResetRetreatLimit();

        Debug.Log($"[SPAWN_FINISH] Level: {levelNum}, Position After: {transform.position}, Intro Disabled: {introFinished}");
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
        else if (rb != null) rb.bodyType = RigidbodyType2D.Dynamic;
    }

    public void DisableJumping() => jumpDisabled = true;
    public void StartLevelEndWalk() => isLevelEnding = true;
    public void SetPortalExitSequenceActive(bool active) => portalExitSequenceActive = active;

    public void SetPortalDrivenWorldPosition(Vector3 worldPos)
    {
        if (rb != null) rb.position = worldPos;
        else transform.position = worldPos;
    }
}
