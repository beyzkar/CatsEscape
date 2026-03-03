using UnityEngine;

public class PlayerObstacleRules : MonoBehaviour
{
    [Header("Side hit freeze")]
    public float topNormalThreshold = 0.4f;
    public float sideNormalThreshold = 0.6f;

    [Header("Hearts (Wall hits)")]
    public GameObject[] heartUI;
    private int currentHearts = 3;
    private GameObject lastHitWall;

    [Header("SFX")]
    public AudioClip crushSfx;
    public AudioClip deathSfx;
    public AudioClip hitWallAudio;
    private AudioSource audioSrc;

    [Header("Death kick")]
    public float deathKickX = -8f;
    public float deathKickY = 6f;
    public float deathGravity = 6f;

    private bool stuck = false;
    private bool dead = false;

    private Rigidbody2D rb;
    private MonoBehaviour movementScript;
    private UnityEngine.Video.VideoPlayer bgVideo;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        movementScript = GetComponent<PlayerMovement>();

        audioSrc = GetComponent<AudioSource>();
        if (audioSrc == null) audioSrc = gameObject.AddComponent<AudioSource>();
        audioSrc.playOnAwake = false;

        bgVideo = Object.FindFirstObjectByType<UnityEngine.Video.VideoPlayer>();

        if (heartUI != null && heartUI.Length > 0)
        {
            currentHearts = heartUI.Length;
        }
    }

    void Update()
    {
        if (dead) return;

        if (stuck && (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.UpArrow)))
        {
            stuck = false;
            lastHitWall = null; // Clear wall tracking on jump
            GameSpeed.Multiplier = 1f;
            if (bgVideo != null) bgVideo.Play();
        }
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        if (dead) return;

        // Bodyguard or BarbedWire: Instant failure from any direction
        if (col.collider.CompareTag("Bodyguard") || col.collider.CompareTag("BarbedWire"))
        {
            // If we already hit this as a Wall in the same frame, don't die instantly
            if (col.gameObject == lastHitWall) return;

            Die();
            return;
        }

        // Obstacle (Bag) or Wall logic
        if (!col.collider.CompareTag("Obstacle") && !col.collider.CompareTag("Wall")) return;

        bool hitTop = false;
        bool hitSide = false;

        // Check all contact points for better reliability
        for (int i = 0; i < col.contactCount; i++)
        {
            Vector2 n = col.GetContact(i).normal;

            // Player lands on top -> normal points up
            if (n.y > topNormalThreshold)
            {
                hitTop = true;
                break;
            }
            
            // Player hits side -> normal points horizontal
            if (Mathf.Abs(n.x) > sideNormalThreshold)
            {
                hitSide = true;
            }
        }

        // ÜSTTEN temas: ses + kutuyu yok et (Sadece Bag/Obstacle için)
        if (hitTop)
        {
            if (col.collider.CompareTag("Obstacle"))
            {
                if (crushSfx != null && audioSrc != null)
                    audioSrc.PlayOneShot(crushSfx);

                Destroy(col.gameObject);
            }
            return;
        }

        // YANDAN veya ALT'TAN temas: STUCK and STOP (Wall or Obstacle)
        if (!hitTop && !stuck)
        {
            if (col.collider.CompareTag("Wall"))
            {
                // Only count hit if it's a new wall
                if (col.gameObject != lastHitWall)
                {
                    lastHitWall = col.gameObject;
                    LoseHeart();
                }

                if (!dead) // Still alive?
                {
                    stuck = true;
                    GameSpeed.Multiplier = 0f;
                    if (bgVideo != null) bgVideo.Pause();
                }
            }
            else if (col.collider.CompareTag("Obstacle"))
            {
                stuck = true;
                GameSpeed.Multiplier = 0f;
                if (bgVideo != null) bgVideo.Pause();
            }
        }
    }

    private void LoseHeart()
    {
        if (dead) return;

        // If currentHearts is 3, first hit makes it 2 and hides heartUI[2]
        // If currentHearts is 1, third hit makes it 0 and hides heartUI[0]
        // If currentHearts is 0, fourth hit makes it -1 and triggers Die()
        currentHearts--;
        
        Debug.Log("Heart lost! Remaining CurrentHearts value: " + currentHearts);

        // Turn off hearts 3, 2, 1 (uses index from 0 to Length-1)
        if (heartUI != null && currentHearts >= 0 && currentHearts < heartUI.Length)
        {
            if (heartUI[currentHearts] != null)
                heartUI[currentHearts].SetActive(false);
        }

        // Play hit wall sound
        if (hitWallAudio != null && audioSrc != null)
            audioSrc.PlayOneShot(hitWallAudio);

        // ONLY DIE ON THE 4TH HIT (when currentHearts becomes -1)
        if (currentHearts < 0)
        {
            Die();
        }
    }

    private void OnCollisionStay2D(Collision2D col)
    {
        if (dead) return;
        if (col.collider.CompareTag("Bodyguard") || col.collider.CompareTag("BarbedWire"))
        {
            if (col.gameObject != lastHitWall) Die();
        }
    }

    private void Die()
    {
        dead = true;
        stuck = false;

        GameSpeed.Multiplier = 0f;
        if (bgVideo != null) bgVideo.Pause();

        // Stop background music and play game over sound
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopBackgroundMusic();
            AudioManager.Instance.PlayGameOverSound();
        }

        if (deathSfx != null && audioSrc != null)
            audioSrc.PlayOneShot(deathSfx);

        if (movementScript != null)
            movementScript.enabled = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = deathGravity;
            rb.AddForce(new Vector2(deathKickX, deathKickY), ForceMode2D.Impulse);
        }

        if (GameOverManager.Instance != null)
            GameOverManager.Instance.ShowGameOver();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (dead) return;

        // Ensure Bodyguard or BarbedWire causes immediate death even if it's a trigger
        if (other.CompareTag("Bodyguard") || other.CompareTag("BarbedWire"))
        {
            if (other.gameObject != lastHitWall) Die();
        }

        // Wall trigger (if collider is trigger)
        if (other.CompareTag("Wall") && !stuck)
        {
            if (other.gameObject != lastHitWall)
            {
                lastHitWall = other.gameObject;
                LoseHeart();
            }

            if (!dead)
            {
                stuck = true;
                GameSpeed.Multiplier = 0f;
                if (bgVideo != null) bgVideo.Pause();
            }
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (dead) return;
        if (other.CompareTag("Bodyguard") || other.CompareTag("BarbedWire"))
        {
            if (other.gameObject != lastHitWall) Die();
        }
    }
}