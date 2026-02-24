using UnityEngine;

public class ObstacleMove : MonoBehaviour
{
    public float speed = 6f;
    public float destroyX = -15f;

    void Update()
    {
        transform.Translate(Vector2.left * speed * GameSpeed.Multiplier * Time.deltaTime);
        
        if (transform.position.x < destroyX)
            Destroy(gameObject);
    }
}
