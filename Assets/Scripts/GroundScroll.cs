using UnityEngine;

public class GroundScroll : MonoBehaviour
{
    public float speed = 5f;
    public float speedIncreasePerSecond = 0.05f; //her saniye oyunun ne kadar hızlanacağını gösterir.
    public float maxSpeed = 12f;

    void Update()
    {
        speed = Mathf.Min(maxSpeed, speed + speedIncreasePerSecond * Time.deltaTime);
        //Mathf.Min: hız max hızı geçmesin diye engel koyar
        transform.position += Vector3.left * speed * Time.deltaTime;
        //Vector3.left: sola doğru hareket ettirir.
    }
}