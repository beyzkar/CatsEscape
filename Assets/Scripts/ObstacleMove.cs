using UnityEngine;

public class ObstacleMove : MonoBehaviour
{
    public float speed = 7f;
    public float destroyX = -15f;
    [HideInInspector] public bool canRewardCleanJump = true;

    private bool passedPlayer = false;
    private Transform player;

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    void Update()
    {
        // SADECE X ekseninde hareket
        transform.position += Vector3.left * speed * GameSpeed.Multiplier * Time.deltaTime;

        // Check if passed player's X position for "clean jump" reward
        if (!passedPlayer && player != null && transform.position.x < player.position.x)
        {
            passedPlayer = true;
            // Only reward if it's an obstacle and NOT the one we just hit
            if (canRewardCleanJump && (CompareTag("Obstacle") || CompareTag("Wall") || CompareTag("Bodyguard") || CompareTag("BarbedWire")))
            {
                if (ScoreManager.Instance != null)
                {
                    ScoreManager.Instance.RegisterCleanJump();
                }

                // Report to LevelManager for progression
                if (LevelManager.Instance != null)
                {
                    LevelManager.Instance.ObstaclePassed();
                }
            }
        }

        if (transform.position.x < destroyX)
            Destroy(gameObject);
    }
}