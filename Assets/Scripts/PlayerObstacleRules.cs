using UnityEngine;

public class PlayerObstacleRules : MonoBehaviour
{
    [Header("Side hit freeze")]
    public float topNormalThreshold = 0.4f;
    public float sideNormalThreshold = 0.6f;

    [Header("Lives (side hits)")]
    public int maxSideHits = 3;
    public int sideHits = 0;

    [Header("SFX")]
    public AudioClip crushSfx;
    private AudioSource audioSrc;

    [Header("Death kick")]
    public float deathKickX = -8f;
    public float deathKickY = 6f;
    public float deathGravity = 6f;

    private bool stuck = false;
    private bool dead = false;

    private Rigidbody2D rb;
    private MonoBehaviour movementScript;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        movementScript = GetComponent<PlayerMovement>();

        audioSrc = GetComponent<AudioSource>();
        if (audioSrc == null) audioSrc = gameObject.AddComponent<AudioSource>();
        audioSrc.playOnAwake = false;
    }

    void Update()
    {
        if (dead) return;

        if (stuck && (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)))
        {
            stuck = false;
            GameSpeed.Multiplier = 1f;
        }
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        if (dead) return;
        if (!col.collider.CompareTag("Obstacle")) return;

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

        // ÜSTTEN temas: ses + kutuyu yok et
        if (hitTop)
        {
            if (crushSfx != null && audioSrc != null)
                audioSrc.PlayOneShot(crushSfx);

            Destroy(col.gameObject);
            return;
        }

        // YANDAN temas
        if (hitSide && !stuck)
        {
            sideHits++;

            if (sideHits < maxSideHits)
            {
                stuck = true;
                GameSpeed.Multiplier = 0f;
            }
            else
            {
                Die();
            }
        }
    }

    private void Die()
    {
        dead = true;
        stuck = false;

        GameSpeed.Multiplier = 1f;

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
}