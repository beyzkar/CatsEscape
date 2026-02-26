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
    
    [Header("Intro Walk")]
    public float introSpeed = 3f;
    public float stopX = -4f; // Example middle position
    private bool introFinished = false;

    public AudioClip jumpSfx;
    private AudioSource audioSrc;


    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        audioSrc = GetComponent<AudioSource>();
        if (audioSrc == null) audioSrc = gameObject.AddComponent<AudioSource>();
        audioSrc.playOnAwake = false;

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

    void Update()
    {
        if (!introFinished)
        {
            DoIntroWalk();
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

        // zıplama tuşları
        if (Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.UpArrow) ||
            Input.GetMouseButtonDown(0))
        {
            TryJump();
        }
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

    void TryJump()
    {
        if (jumpsLeft <= 0) return;  

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

        if (jumpSfx != null && audioSrc != null)
            audioSrc.PlayOneShot(jumpSfx);

        jumpsLeft--;
        //Debug.Log("After jump | jumpsLeft=" + jumpsLeft); jumpın çalıp çalışmadığını kontrol eder.
    }
}