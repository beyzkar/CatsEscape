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
        if (dead) return;

        if (!introFinished)
        {
            DoIntroWalk();
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

        bool physicsGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        bool clampGrounded = (LevelManager.Instance != null && LevelManager.Instance.currentLevel <= 4 && transform.position.y <= -3.69f);
        isGrounded = physicsGrounded || clampGrounded;

        if (isGrounded && rb.linearVelocity.y <= 0.1f) ResetJumps();

        float hInput = Input.GetAxisRaw("Horizontal");
        if (isLevelEnding) hInput = 1f;
        else
        {
            if (mobileLeft) hInput = -1f;
            if (mobileRight) hInput = 1f;
        }

        float desiredVelocity = hInput * (hInput > 0 ? moveRightSpeed : moveLeftSpeed);
        bool isStopping = (hInput == 0);
        bool isTurning = (desiredVelocity > 0 && currentHorizontalVelocity < 0) || (desiredVelocity < 0 && currentHorizontalVelocity > 0);
        
        float currentDecelRate = isStopping ? stoppingDeceleration : deceleration;
        float accelRate = isTurning ? (deceleration * 2f) : (Mathf.Abs(desiredVelocity) > 0.01f ? acceleration : currentDecelRate);
        
        if (!isGrounded) accelRate *= airAccelerationMultiplier;
        currentHorizontalVelocity = Mathf.MoveTowards(currentHorizontalVelocity, desiredVelocity, accelRate * Time.deltaTime);
        
        if (isStopping && Mathf.Abs(currentHorizontalVelocity) < 0.01f) currentHorizontalVelocity = 0f;
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

        if (Input.GetKeyDown(KeyCode.UpArrow)) MobileJumpDown();
    }

    void FixedUpdate()
    {
        UpdateViewportBounds();

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

        float distanceStep = targetVelocityX * GameSpeed.Multiplier * Time.fixedDeltaTime;
        if (transform.position.x <= CurrentLeftBoundaryX && distanceStep < 0) distanceStep = 0; 

        totalDistance += distanceStep;
        if (totalDistance > peakDistance) peakDistance = totalDistance;

        float finalVelocityX = targetVelocityX * groundedSpeedMultiplier;
        bool isAirborneForControl = !isGrounded || Mathf.Abs(rb.linearVelocity.y) > 0.05f;
        if (isAirborneForControl) finalVelocityX *= airControlMultiplier;

        float blendDuration = Mathf.Max(landingVelocityBlendTime, 0.01f);
        float currentPhysicsDecel = (Mathf.Abs(finalVelocityX) < 0.01f) ? stoppingDeceleration : deceleration;
        float blendRate = (Mathf.Abs(finalVelocityX) > 0.01f) ? Mathf.Abs(finalVelocityX - appliedHorizontalVelocity) / blendDuration : currentPhysicsDecel;
        appliedHorizontalVelocity = Mathf.MoveTowards(appliedHorizontalVelocity, finalVelocityX, blendRate * Time.fixedDeltaTime);

        if (transform.position.x >= maxX && appliedHorizontalVelocity > 0) appliedHorizontalVelocity = 0f;
        rb.linearVelocity = new Vector2(appliedHorizontalVelocity, rb.linearVelocity.y);

        if (rb.linearVelocity.y < 0)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }
        else if (rb.linearVelocity.y > 0 && !Input.GetKey(KeyCode.UpArrow)) 
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;
        }

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
            anim.transform.localPosition = new Vector3(0f, originalVisualLocalY + externalVisualYOffset, 0f);
        }

        if (boxCol != null && LevelManager.Instance != null && LevelManager.Instance.currentLevel <= 4)
        {
            boxCol.size = new Vector2(1.050137f, 1.623463f);
            boxCol.offset = new Vector2(0.1874768f * WorldDirection, 0.6628802f);
        }
    }

    void DoIntroWalk()
    {
        transform.position = Vector3.MoveTowards(transform.position, new Vector3(stopX, transform.position.y, transform.position.z), introSpeed * Time.deltaTime);
        if (Mathf.Abs(transform.position.x - stopX) < 0.1f || transform.position.x >= maxX - 0.1f) introFinished = true;
    }

    public void ResetJumps() => jumpsLeft = maxJumps;
    public void TryJump()
    {
        if (jumpsLeft <= 0) return;  
        rb.linearVelocity = new Vector2(rb.linearVelocity.x * jumpTakeoffHorizontalMultiplier, 0f);
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
    
    public float CurrentVelocityX => (rules != null && (rules.IsHorizontalBlocked || GameSpeed.Multiplier <= 0.01f) && currentHorizontalVelocity > 0) ? 0f : currentHorizontalVelocity;
    
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
        float targetX = (levelStartPoint != null) ? levelStartPoint.position.x : stopX;
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
            rb.linearVelocity = Vector2.zero;
            rb.position = new Vector2(targetX, targetY);
        }
        else transform.position = new Vector3(targetX, targetY, transform.position.z);

        currentHorizontalVelocity = 0f;
        targetVelocityX = 0f;
        appliedHorizontalVelocity = 0f;
        mobileLeft = mobileRight = wasAtRetreatLimit = dead = false;
        isGrounded = introFinished = true;
        WorldDirection = 1;
        jumpDisabled = isLevelEnding = false;
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
