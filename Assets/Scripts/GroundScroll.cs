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
        speed = Mathf.Min(maxSpeed, speed + speedIncreasePerSecond * Time.deltaTime);
        //Mathf.Min: hız max hızı geçmesin diye engel koyar,
        
        transform.position += Vector3.left * speed * GameSpeed.Multiplier * Time.deltaTime;
        //Vector3.left: sola doğru hareket ettirir.
    }
}