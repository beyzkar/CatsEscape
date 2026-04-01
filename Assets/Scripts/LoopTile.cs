//Ground'uun loopta kalıp sürekli olarak hareket etmesini sağlar
using UnityEngine;

public class LoopTile : MonoBehaviour
{
    public float resetX = -40f;    
    public float startX = 40f;    
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