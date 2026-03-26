using UnityEngine;

public class ObstacleMove : MonoBehaviour
{
    public float speed = 7f;
    public float destroyX = -15f;
    [HideInInspector] public bool canRewardCleanJump = true;

    private bool passedPlayer = false;
    private Transform player;
    private PlayerMovement playerMove;

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            playerMove = p.GetComponent<PlayerMovement>();
        }
    }

    void Update()
    {
        // SADECE X ekseninde hareket
        int dir = (playerMove != null) ? playerMove.WorldDirection : 1;
        transform.position += Vector3.left * speed * GameSpeed.Multiplier * dir * Time.deltaTime;

        // Check if passed player's X position for "clean jump" reward
        if (!passedPlayer && player != null && transform.position.x < player.position.x)
        {
            passedPlayer = true;
            // Only reward if it's an obstacle and NOT the one we just hit
            if (canRewardCleanJump && (CompareTag("Obstacle") || CompareTag("Wall") || CompareTag("Bodyguard") || CompareTag("BarbedWire") || CompareTag("Bush")))
            {
                if (ScoreManager.Instance != null)
                {
                    // Bush için doğrudan XP verilmiyor artık (Seri/streak içinde değerlendiriliyor)
                    ScoreManager.Instance.RegisterCleanJump();
                }

                // Report to LevelManager for progression (Only for Levels 1-4)
                // Level 5 uses GroundPoint.cs for segment-based progression.
                if (LevelManager.Instance != null && LevelManager.Instance.currentLevel < 5)
                {
                    LevelManager.Instance.ObstaclePassed();
                }
            }
        }

        if (transform.position.x < destroyX && GameSpeed.Multiplier > 0)
            Destroy(gameObject);
    }
}