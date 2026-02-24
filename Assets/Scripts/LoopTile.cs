//Ground'uun loopta kalıp sürekli olarak hareket etmesini sağlar
using UnityEngine;

public class LoopTile : MonoBehaviour
{
    public float tileWidth = 40f;  // Ground'un dünya genişliği
    public float resetX = -40f;    // Bu X’in altına inince resetle
    public float startX = 40f;     // Buraya geri koy

    void Update()
    {
        if (transform.position.x <= resetX)
        {
            var p = transform.position;
            p.x = startX;
            transform.position = p;
        }
    }
}