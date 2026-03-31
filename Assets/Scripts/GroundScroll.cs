using UnityEngine;

public class GroundScroll : MonoBehaviour
{
    public static GroundScroll Instance { get; private set; }

    public float speed = 6f;
    public float speedIncreasePerSecond = 0.05f; //her saniye oyunun ne kadar hızlanacağını gösterir.
    public float maxSpeed = 12f;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Update()
    {
        if (PlayerMovement.Instance == null || GameSpeed.Multiplier <= 0) return;

        // Move the ground in the opposite direction of player movement
        float scrollAmount = PlayerMovement.Instance.CurrentVelocityX * GameSpeed.Multiplier * Time.deltaTime;
        transform.position += Vector3.left * scrollAmount;
    }
}