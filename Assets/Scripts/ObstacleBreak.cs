using UnityEngine;

public class ObstacleBreak : MonoBehaviour
{
    [Header("Break when player lands on top")]
    public float topNormalThreshold = 0.5f; 

    private void OnCollisionEnter2D(Collision2D col)
    {
        if (!col.collider.CompareTag("Player")) return;

        //temas edilen noktayı kontrol et 
        for (int i = 0; i < col.contactCount; i++)
        {
            Vector2 n = col.GetContact(i).normal;

            //kedi kutunun üstüne düştüğü zaman
            if (n.y > topNormalThreshold)
            {
                Destroy(gameObject);
                return;
            }
        }
    }
}