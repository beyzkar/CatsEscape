using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float jumpForce = 12f;
    public int maxJumps = 2; //double jump yaptığımız yer 

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
    public float moveLeftSpeed = 7f; // Hızlı kaçış manevrası için
    public float minX = -8.5f;
    public float maxX = 2f;
    [Header("Return Speed")]
    public float returnSpeed = 2f;

    private PlayerObstacleRules rules;

    [Header("Intro Walk")]
    public float introSpeed = 3f;
    public float stopX = -4f;
    private bool introFinished = false;

    [Header("Camera Control")]
    public float maxCameraShift = 2f;
    public float cameraShiftSpeed = 5f;
    private Camera mainCam;
    private float targetCameraShift = 0f;
    private float currentCameraShift = 0f;
    private Vector3 initialCamPos;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        
        rules = GetComponent<PlayerObstacleRules>();
        mainCam = Camera.main;
        if (mainCam != null) initialCamPos = mainCam.transform.position;

        // Force orientation: Face Right (if cat walks Left) or Face Left (if cat walks Right)
        // Adjust this if your cat is backwards
        transform.localRotation = Quaternion.Euler(0, 0, 0); 
    }

    void Start()
    {
        jumpsLeft = maxJumps;
        
        // Ensure starting position is off-screen if needed
        // transform.position = new Vector3(-12f, transform.position.y, 0f);
    }

    private float targetVelocityX = 0f;

    void Update()
    {
        if (!introFinished)
        {
            DoIntroWalk();
            return;
        } 

        if (rules != null && rules.IsDead) 
        {
            targetVelocityX = 0f;
            return;
        }

        // yere değiyor mu kontrol
        bool groundedNow = Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );

        // sadece yere değdiği anda zıplama hakkını resetle
        if (groundedNow && !isGrounded)
        {
            jumpsLeft = maxJumps;
        }

        isGrounded = groundedNow;

        // --- HORIZONTAL MOVEMENT INPUT ---
        float horizontalInput = 0f;
        targetVelocityX = 0f;

        // Move Left (Always available if not dead)
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            targetVelocityX = -moveLeftSpeed;
            horizontalInput = -1f;
            WorldDirection = -1;
        }
        else
        {
            WorldDirection = 1;
        }

        // Move Right (Movement is allowed, but World remains stopped by GameSpeed.Multiplier)
        if (Input.GetKey(KeyCode.RightArrow))
        {
            targetVelocityX = moveRightSpeed;
            horizontalInput = 1f;
        }

        // --- CAMERA SHIFT LOGIC ---
        if (mainCam != null)
        {
            // If moving left, shift camera target left
            targetCameraShift = (horizontalInput < 0) ? -maxCameraShift : 0f;
            currentCameraShift = Mathf.Lerp(currentCameraShift, targetCameraShift, Time.deltaTime * cameraShiftSpeed);
            mainCam.transform.position = new Vector3(initialCamPos.x + currentCameraShift, initialCamPos.y, initialCamPos.z);
        }

        // --- FLIP LOGIC ---
        if (horizontalInput > 0) FaceDirection(true);
        else if (horizontalInput < 0) FaceDirection(false);

        // zıplama tuşları
        if (Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.UpArrow) ||
            Input.GetMouseButtonDown(0))
        {
            TryJump();
        }
    }

    void FixedUpdate()
    {
        if (!introFinished || (rules != null && rules.IsDead)) return;

        // Apply horizontal velocity while preserving vertical velocity from gravity/jumps
        float finalVelocityX = targetVelocityX;

        // RETURN TO HOME logic (if not dead/stuck and not pressing Left)
        if (introFinished && (rules == null || (!rules.IsDead && !rules.IsStuck)))
        {
            // If we are to the left of our home (stopX) and not actively moving further left
            if (transform.position.x < stopX && targetVelocityX >= 0)
            {
                // Add return speed to the right
                finalVelocityX += returnSpeed;
            }
        }

        rb.linearVelocity = new Vector2(finalVelocityX, rb.linearVelocity.y);

        // Clamping position is better done by forcing velocity to 0 at boundaries
        // but for now, we'll let Physics handle colliders. 
        // If we really need a hard clamp, we should check if we're past maxX/minX and set velocity to 0.
        if (transform.position.x > maxX && rb.linearVelocity.x > 0)
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        if (transform.position.x < minX && rb.linearVelocity.x < 0)
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
    }

    void FaceDirection(bool faceRight)
    {
        // 0 degrees for right, 180 degrees for left
        float yRotation = faceRight ? 0f : 180f;
        transform.localRotation = Quaternion.Euler(0, yRotation, 0);
    }

    void DoIntroWalk()
    {
        // Move cat towards the stopping point
        float step = introSpeed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, new Vector3(stopX, transform.position.y, 0), step);

        // Check if reached
        if (Mathf.Abs(transform.position.x - stopX) < 0.01f)
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
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayJump();

        jumpsLeft--;
        //Debug.Log("After jump | jumpsLeft=" + jumpsLeft); jumpın çalıp çalışmadığını kontrol eder.
    }
}