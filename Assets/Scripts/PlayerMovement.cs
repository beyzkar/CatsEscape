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
    public float moveRightSpeed = 5f;
    public float moveLeftSpeed = 5f; 
    public float minX = -5.3f; // Initial left boundary
    public float maxX = 5f;    // Initial right boundary
    public float acceleration = 20f;
    public float deceleration = 25f;
    private float currentHorizontalVelocity = 0f;
    private float targetVelocityX = 0f;

    [Header("Viewport Clamping")]
    public bool useViewportClamping = true;
    [Range(0.1f, 10f)] public float xScrollLimit = 5f; // Boundary multiplier
    public float ScreenMaxX { get; private set; } // Right edge of screen for other scripts
    public float viewportPaddingX = 3.5f; // Safety margin for character bounds

    [Header("Visual Orientation")]
    public float rotationRight = 120f;
    public float rotationLeft = 300f; 
    public int WorldDirection { get; private set; } = 1;

    [Header("Intro Settings")]
    public float introSpeed = 3f;
    public float stopX = -4f;
    private bool introFinished = false;
    private bool dead = false;
    
    // Internal references and state
    private PlayerObstacleRules rules;
    private Animator anim;
    private float originalAbsScaleX;
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
    }

    void Start()
    {
        currentJumpForce = jumpForce;
        jumpsLeft = maxJumps;
        originalAbsScaleX = Mathf.Abs(transform.localScale.x);
        
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

        // Smoothed velocity calculation
        float desiredVelocity = hInput * (hInput > 0 ? moveRightSpeed : moveLeftSpeed);
        float accelRate = (Mathf.Abs(desiredVelocity) > 0.01f) ? acceleration : deceleration;
        currentHorizontalVelocity = Mathf.MoveTowards(currentHorizontalVelocity, desiredVelocity, accelRate * Time.deltaTime);
        targetVelocityX = currentHorizontalVelocity;

        // Dynamic animator fetch for multiple character skins
        if (anim == null || !anim.gameObject.activeInHierarchy)
            anim = GetComponentInChildren<Animator>();

        // Animation state management
        if (anim != null)
        {
            bool isMovingInput = (hInput != 0);
            anim.SetBool("walking", isMovingInput);
            anim.SetBool("Idle", !isMovingInput);
        }
        
        // Direction and Rotation management
        if (hInput > 0) WorldDirection = 1;
        else if (hInput < 0) WorldDirection = -1;

        // Explicit transform and child rotation enforcement
        transform.localScale = new Vector3(originalAbsScaleX, transform.localScale.y, transform.localScale.z);
        transform.localRotation = Quaternion.identity;

        if (anim != null)
        {
            float targetRY = (WorldDirection == 1) ? rotationRight : rotationLeft;
            anim.transform.localRotation = Quaternion.Euler(0, targetRY, 0);
        }

        // Jump input handling
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            MobileJumpDown();
        }
    }

    void FixedUpdate()
    {
        if (!introFinished || (rules != null && rules.IsDead)) return;

        float finalVelocityX = targetVelocityX;

        // Boundary safety check for physics velocity
        if (transform.position.x <= minX && finalVelocityX < 0) finalVelocityX = 0;
        if (transform.position.x >= maxX && finalVelocityX > 0) finalVelocityX = 0;

        // Stop movement if stuck against an obstacle
        if (rules != null && rules.IsStuck) finalVelocityX = 0f;

        rb.linearVelocity = new Vector2(finalVelocityX, rb.linearVelocity.y);
    }

    void LateUpdate()
    {
        if (!useViewportClamping)
        {
            // Simple clamping fallback
            float clampedX = Mathf.Clamp(transform.position.x, minX, maxX);
            if (clampedX != transform.position.x)
            {
                transform.position = new Vector3(clampedX, transform.position.y, transform.position.z);
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            }
            return;
        }

        Camera cam = Camera.main;
        if (cam == null) return;

        float zDist = Mathf.Abs(cam.transform.position.z);
        Vector3 bottomLeft = cam.ViewportToWorldPoint(new Vector3(0, 0, zDist));
        Vector3 topRight = cam.ViewportToWorldPoint(new Vector3(xScrollLimit, 1, zDist));
        Vector3 screenTopRight = cam.ViewportToWorldPoint(new Vector3(1, 1, zDist));

        // Update bounds for current frame
        minX = bottomLeft.x + viewportPaddingX;
        maxX = topRight.x;
        ScreenMaxX = screenTopRight.x;

        // Enforce strict clamping
        float cx = Mathf.Clamp(transform.position.x, minX, maxX);
        float cy = Mathf.Clamp(transform.position.y, bottomLeft.y, topRight.y);

        if (cx != transform.position.x || cy != transform.position.y)
        {
            transform.position = new Vector3(cx, cy, transform.position.z);
            if (cx != transform.position.x) rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
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
}