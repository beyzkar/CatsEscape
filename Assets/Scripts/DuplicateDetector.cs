using UnityEngine;

public class DuplicateDetector : MonoBehaviour
{
    void Update()
    {
        // ACTIVE ASSASSIN MODE: Kill clones every frame!
        PlayerMovement[] players = Object.FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        
        if (players.Length > 1)
        {
            Debug.LogWarning("<color=orange><b>[ASSASSIN] Extra cat detected! Terminating clone...</b></color>");
            
            // Keep ONLY the first instance, destroy all others
            // Note: We check PlayerMovement.Instance to decide who stays
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i] != PlayerMovement.Instance && players[i].gameObject != null)
                {
                    Destroy(players[i].gameObject);
                    Debug.Log("<color=green><b>[SUCCESS] Clone deleted.</b></color>");
                }
            }
        }
    }
}
