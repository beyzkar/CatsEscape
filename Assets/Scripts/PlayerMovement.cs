using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public static PlayerMovement Instance { get; private set; }
    public float jumpForce = 12f;
    private float currentJumpForce;
    public int maxJumps = 2; 

    [Header("Level 5 Fall Settings")]
    public float deathThresholdY = -4.2f; // Height at which jumping is disabled in Level 5

    public Transform groundCheck; //yere değip değmediğini kontrol ettiğimiz yer 
    public float groundCheckRadius = 0.15f;
    public LayerMask groundLayer; //yerin hangi layer olduğunu belirtir (sadece bu layer temas sayılır )
    /*
     Layer
       nesneleri gruplandırır
       fizik ve algılama kontrolü sağlar
       hataları önler
       sperformansı artırır
     */

    private Rigidbody2D rb; //Rigibody2D: nesneye fizik kazandırır.
    /*
     yerçekimi uygular
     düşmesini sağlar
     kuvvet uygulayabilirsin
     çarpışmalara tepki verir
     */
    private int jumpsLeft;
    private bool isGrounded;
    public bool IsGrounded => isGrounded; // Diğer scriptlerin (LevelManager) kedinin yere basıp basmadığını görmesi için
    public int WorldDirection { get; private set; } = 1;
    
    [Header("Movement Settings")]
    public float moveRightSpeed = 5f;
    public float moveLeftSpeed = 5f; 
    public float minX = -5.3f; // Narrower left boundary
    public float maxX = 5f;
    [Header("Return Speed")]
    public float returnSpeed = 2f;

    [Header("Viewport Clamping")]
    public bool useViewportClamping = true;
    [Range(0.1f, 10f)] public float xScrollLimit = 5f; // Screen width multiplier (1.0 = full screen)

    public float ScreenMaxX { get; private set; } // Actual right edge of screen
    public float viewportPaddingX = 3.5f; // Margin to keep cat from clipping edge (increased to match photo)

    [Header("Transition Smoothing")]
    public float flipSpeed = 25f; // Mario-style scale flip speed
    public float acceleration = 20f;
    public float deceleration = 25f;
    private float currentHorizontalVelocity = 0f;
    private float targetScaleX = 1f;
    private float originalAbsScaleX;

    private PlayerObstacleRules rules;
    private Animator anim;

    [Header("Intro Walk")]
    public float introSpeed = 3f;
    public float stopX = -4f;
    private bool introFinished = false;
    private bool dead = false;
    
    // Mobile Input Flags
    private bool mobileLeft = false;
    private bool mobileRight = false;


    void Awake()
    {
        Instance = this;
        rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        
        rules = GetComponent<PlayerObstacleRules>();
        // Initial search, but we will re-search in Update if this one becomes inactive
        anim = GetComponentInChildren<Animator>(); 

        // Force orientation: Face Right (if cat walks Left) or Face Left (if cat walks Right)
        // Adjust this if your cat is backwards
        transform.localRotation = Quaternion.Euler(0, 0, 0); 
    }

    void Start()
    {
        currentJumpForce = jumpForce;
        jumpsLeft = maxJumps;
        originalAbsScaleX = Mathf.Abs(transform.localScale.x);
        targetScaleX = originalAbsScaleX;
        
        // Ensure starting position is off-screen if needed
        // transform.position = new Vector3(-12f, transform.position.y, 0f);
    }

    private float targetVelocityX = 0f;

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
            if (anim != null) 
            {
                anim.SetBool("walking", true);
                anim.SetBool("Idle", false);
            }
            return;
        } 

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

        // --- GROUND CHECK ---
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        // Ensure jumps are only reset when truly grounded and not in the middle of a jump
        if (isGrounded && rb.linearVelocity.y <= 0.1f)
        {
            jumpsLeft = maxJumps;
        }

        // --- HORIZONTAL MOVEMENT (Smoothed) ---
        float hInput = Input.GetAxisRaw("Horizontal"); // Support keyboard
        if (mobileLeft) hInput = -1f;   // Support mobile
        if (mobileRight) hInput = 1f;

        float desiredVelocity = hInput * (hInput > 0 ? moveRightSpeed : moveLeftSpeed);
        float accelRate = (Mathf.Abs(desiredVelocity) > 0.01f) ? acceleration : deceleration;
        currentHorizontalVelocity = Mathf.MoveTowards(currentHorizontalVelocity, desiredVelocity, accelRate * Time.deltaTime);
        targetVelocityX = currentHorizontalVelocity;

        // --- DYNAMIC ANIMATOR FETCHING (For multiple character support) ---
        if (anim == null || !anim.gameObject.activeInHierarchy)
        {
            anim = GetComponentInChildren<Animator>();
        }

        // --- ANIMATION CONTROL (Instant Idle) ---
        if (anim != null)
        {
            bool isMovingInput = (hInput != 0);
            anim.SetBool("walking", isMovingInput);
            anim.SetBool("Idle", !isMovingInput);
        }
        
        // --- NORMAL ROTATION (Instant Flip) ---
        if (hInput > 0) { WorldDirection = 1; transform.localRotation = Quaternion.Euler(0, 0, 0); }
        else if (hInput < 0) { WorldDirection = -1; transform.localRotation = Quaternion.Euler(0, 180f, 0); }

        // --- JUMP INPUT (Keyboard + Mobile Buttons) ---
        // MouseButtonDown(0) removed to prevent conflict with UI button clicks in simulator
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            MobileJumpDown();
        }
    }

    void FixedUpdate()
    {
        if (!introFinished || (rules != null && rules.IsDead)) return;

        // Apply horizontal velocity while preserving vertical velocity from gravity/jumps
        float finalVelocityX = targetVelocityX;

        // --- PREVENT JITTER (Early Velocity Suppression) ---
        // Use boundaries from the previous LateUpdate for solid physics feel
        if (transform.position.x <= minX && finalVelocityX < 0) finalVelocityX = 0;
        if (transform.position.x >= maxX && finalVelocityX > 0) finalVelocityX = 0;

        // If stuck, don't allow horizontal movement
        if (rules != null && rules.IsStuck)
        {
            finalVelocityX = 0f;
        }

        rb.linearVelocity = new Vector2(finalVelocityX, rb.linearVelocity.y);
    }

    void LateUpdate()
    {
        // --- FINAL POSITION CLAMPING (Runs after all movement/physics) ---
        if (useViewportClamping)
        {
            Camera cam = Camera.main;
            if (cam == null) cam = FindObjectOfType<Camera>();
            if (cam == null) return;

            float zDist = Mathf.Abs(cam.transform.position.z);
            Vector3 bottomLeft = cam.ViewportToWorldPoint(new Vector3(0, 0, zDist));
            Vector3 topRight = cam.ViewportToWorldPoint(new Vector3(xScrollLimit, 1, zDist));
            Vector3 screenTopRight = cam.ViewportToWorldPoint(new Vector3(1, 1, zDist));

            // Update bounds for other scripts
            minX = bottomLeft.x + viewportPaddingX;
            maxX = topRight.x;
            ScreenMaxX = screenTopRight.x;

            float minY = bottomLeft.y;
            float maxY = topRight.y;

            // Strict position enforcement
            float cx = Mathf.Clamp(transform.position.x, minX, maxX);
            float cy = Mathf.Clamp(transform.position.y, minY, maxY);

            if (cx != transform.position.x || cy != transform.position.y)
            {
                transform.position = new Vector3(cx, cy, transform.position.z);
                if (cx != transform.position.x) rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            }
        }
        else
        {
            // Legacy clamping fallback
            float cx = Mathf.Clamp(transform.position.x, minX, maxX);
            if (cx != transform.position.x)
            {
                transform.position = new Vector3(cx, transform.position.y, transform.position.z);
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            }
        }
    }

    void DoIntroWalk()
    {
        // Move cat towards the stopping point
        float step = introSpeed * Time.deltaTime;
        Vector3 targetPos = new Vector3(stopX, transform.position.y, transform.position.z);
        transform.position = Vector3.MoveTowards(transform.position, targetPos, step);

        // Check if reached OR clamped (in case clamping stops it earlier)
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
        //Debug.Log("After jump | jumpsLeft=" + jumpsLeft); jumpın çalıp çalışmadığını kontrol eder.
    }

    public void SetJumpMultiplier(float multiplier)
    {
        currentJumpForce = jumpForce * multiplier;
    }

    public void ResetJumpMultiplier()
    {
        currentJumpForce = jumpForce;
    }

    // --- MOBILE INPUT METHODS ---
    public void SetMoveLeft(bool isMoving) { mobileLeft = isMoving; }
    public void SetMoveRight(bool isMoving) { mobileRight = isMoving; }
    
    // Properties for external scripts (like PlayerObstacleRules) to check input state
    public bool IsMovingLeft => Input.GetKey(KeyCode.LeftArrow) || mobileLeft;
    public bool IsMovingRight => Input.GetKey(KeyCode.RightArrow) || mobileRight;
    
    // EXPOSE velocity for World Scrolling
    public float CurrentVelocityX => currentHorizontalVelocity;
    
    public void MobileJumpDown()
    {
        if (dead) return;
        
        // Level 5 specific: Disable jump if too low (falling into pit)
        bool isTooLowInLevel5 = false;
        if (LevelManager.Instance != null && LevelManager.Instance.currentLevel == 5)
        {
            if (transform.position.y < deathThresholdY)
            {
                isTooLowInLevel5 = true;
            }
        }

        if (!isTooLowInLevel5)
        {
            TryJump();
        }
        else
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayFalling();
        }
    }
}