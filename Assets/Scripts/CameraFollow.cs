using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 0.125f;
    public Vector3 offset = new Vector3(0, 0, -10f);

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;
        // 2D Runner'larda genellikle sadece sağa (X) takip etmek ve Y'yi sabit tutmak istenir
        // Ama istersen Y'yi de serbest bırakabiliriz. Şimdilik sadece X'i takip edelim:
        desiredPosition.y = transform.position.y; 

        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;
    }
}
