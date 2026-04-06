using UnityEngine;

// Core player movement controller: handles horizontal movement, jumping, and rotation/flipping
public class PlayerMovement : MonoBehaviour
{
    public static PlayerMovement Instance { get; private set; }

    [Header("Jump Settings")]
    public float jumpForce = 12f;
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
    [Range(0.1f, 10f)] public float xScrollLimit = 0.35f; // Boundary multiplier
    public float ScreenMaxX { get; private set; } // Right edge of screen for other scripts
    public float viewportPaddingX = 1.8f; // Reduced from 4.5f to allow closer approach to edges
    private Vector3 bottomLeft;
    private Vector3 topRight;

    [Header("Visual Orientation")]
    public float rotationRight = 120f;
    public float rotationLeft = 300f; 
    public float rotationSpeed = 720f; // Speed of the turn-around pivot
    public int WorldDirection { get; private set; } = 1;
    private BoxCollider2D boxCol;

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
        
        // Capture original visual height to apply offsets additively
        if (anim != null) originalVisualLocalY = anim.transform.localPosition.y;
        
        // Ensure parent rotation is locked at identity
        transform.localRotation = Quaternion.identity; 
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

        // Smoothed velocity calculation with turn-brake logic
        float desiredVelocity = hInput * (hInput > 0 ? moveRightSpeed : moveLeftSpeed);
        
        // Detect if we are changing direction (Turning)
        bool isTurning = (desiredVelocity > 0 && currentHorizontalVelocity < 0) || 
                         (desiredVelocity < 0 && currentHorizontalVelocity > 0);
        
        // If turning, use deceleration to stop quickly first. Otherwise use acceleration/deceleration normally.
        float accelRate = isTurning ? deceleration : (Mathf.Abs(desiredVelocity) > 0.01f ? acceleration : deceleration);
        
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
        if (!introFinished || (rules != null && rules.IsDead)) return;

        float finalVelocityX = targetVelocityX;

        // Strict Velocity Lock: No movement allowed if strictly stuck or in recovery
        if (rules != null && rules.IsHorizontalBlocked)
        {
            // Block Rightward movement to prevent sinking in
            // But ALLOW Leftward movement (negative velocity) to escape instantly
            if (finalVelocityX > 0) finalVelocityX = 0f;
            
            // If truly stuck (not in recovery timer), strictly ensure we don't move right
            if (rules.IsStuck && finalVelocityX > 0) finalVelocityX = 0f;
        }

        if (transform.position.x <= minX && finalVelocityX < 0) finalVelocityX = 0;
        if (transform.position.x >= maxX && finalVelocityX > 0) finalVelocityX = 0;

        rb.linearVelocity = new Vector2(finalVelocityX, rb.linearVelocity.y);

        // Strict physical position clamp to prevent penetration through boundaries
        // Only applied when NOT dead and NOT in intro walk
        if (rb.position.x < minX)
        {
            rb.position = new Vector2(minX, rb.position.y);
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
        else if (rb.position.x > maxX)
        {
            rb.position = new Vector2(maxX, rb.position.y);
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }

    void LateUpdate()
    {
        UpdateViewportBounds();
        if (!useViewportClamping) return;

        // Enforce strict clamping
        float clampedX = Mathf.Clamp(transform.position.x, minX, maxX);
        
        // Level 5 has pits, so we allow falling below the floor!
        float effectiveMinY = bottomLeft.y;
        if (LevelManager.Instance != null && LevelManager.Instance.currentLevel == 5)
        {
            effectiveMinY = -20f; // Allow falling far down
        }
        
        float clampedY = Mathf.Clamp(transform.position.y, effectiveMinY, topRight.y);

        if (clampedX != transform.position.x || clampedY != transform.position.y)
        {
            transform.position = new Vector3(clampedX, clampedY, transform.position.z);
            if (clampedX != transform.position.x) rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }

        // Forced Base Height for Levels 1-4
        // This ensures the cat stays at exactly -3.7 regardless of gravity or ground placement.
        if (LevelManager.Instance != null && LevelManager.Instance.currentLevel <= 4)
        {
            if (transform.position.y < -3.7f)
            {
                transform.position = new Vector3(transform.position.x, -3.7f, transform.position.z);
                if (rb.linearVelocity.y < 0) rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
            }
        }

        // Animation Overwrite Prevention:
        // Set the animator's local Y position AFTER the animator itself has calculated its frame position.
        // This ensures the visual cat offset works regardless of which animation is playing.
        if (anim != null)
        {
            Vector3 visualPos = anim.transform.localPosition;
            visualPos.y = originalVisualLocalY + externalVisualYOffset;
            anim.transform.localPosition = visualPos;
        }

        // Strict Collider Enforcement for Levels 1-4 (User Request)
        if (boxCol != null && LevelManager.Instance != null && LevelManager.Instance.currentLevel <= 4)
        {
            // Use the exact values provided by the user in Turn 218
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
        bottomLeft = cam.ViewportToWorldPoint(new Vector3(0, 0, zDist));
        topRight = cam.ViewportToWorldPoint(new Vector3(xScrollLimit, 1, zDist));
        Vector3 screenTopRight = cam.ViewportToWorldPoint(new Vector3(1, 1, zDist));

        // Enforce a minimum safe padding to stay away from the notch/edge
        float effectivePadding = Mathf.Max(viewportPaddingX, 1.2f);
        minX = bottomLeft.x + effectivePadding;
        maxX = topRight.x - effectivePadding; 
        ScreenMaxX = screenTopRight.x;
    }

    public void ResetHorizontalVelocity()
    {
        currentHorizontalVelocity = 0f;
        targetVelocityX = 0f;
        if (rb != null) rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
    }
}
