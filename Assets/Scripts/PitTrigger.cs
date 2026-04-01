using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

// Handles player death and scene restart when falling into a pit
public class PitTrigger : MonoBehaviour
{
    [Header("Settings")]
    public float restartDelay = 1.0f; // Time to wait before restarting the scene
    public Color gizmoColor = new Color(1, 0, 0, 0.5f); // Visualization color in the Inspector

    private bool isRestarting = false;

    // 2D Collision Detection
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !isRestarting)
        {
            HandlePlayerFall(collision.gameObject);
        }
    }

    private void HandlePlayerFall(GameObject player)
    {
        isRestarting = true;
        Debug.Log("[PitTrigger] Player fell into the pit!");

        // 1. Freeze player controls and physics
        FreezePlayer(player);

        // 2. Trigger death logic (Universal message for compatibility)
        player.SendMessage("Die", SendMessageOptions.DontRequireReceiver);

        // 3. Start the restart sequence
        StartCoroutine(RestartSequence());
    }

    private void FreezePlayer(GameObject player)
    {
        // Physics freeze
        Rigidbody2D rb2d = player.GetComponent<Rigidbody2D>();
        if (rb2d != null)
        {
            rb2d.linearVelocity = Vector2.zero;
            rb2d.simulated = false; // Disable physics simulation
        }

        // Stop movement logic
        player.SendMessage("SetDead", true, SendMessageOptions.DontRequireReceiver);
    }

    private IEnumerator RestartSequence()
    {
        yield return new WaitForSeconds(restartDelay);
        
        // Reload current active scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    [ContextMenu("Fit Visual To Collider")]
    public void FitVisualToCollider()
    {
        BoxCollider2D box2d = GetComponent<BoxCollider2D>();
        if (box2d == null || transform.childCount == 0) return;

        foreach (Transform child in transform)
        {
            child.localPosition = (Vector3)box2d.offset;
            child.localScale = new Vector3(box2d.size.x, box2d.size.y, 1f);
        }
    }

    // Visualize the pit in the Editor
    private void OnDrawGizmos()
    {
        BoxCollider2D box2d = GetComponent<BoxCollider2D>();
        if (box2d == null) return;

        Gizmos.color = gizmoColor;
        Gizmos.matrix = transform.localToWorldMatrix; // Apply local scale/rotation
        Gizmos.DrawCube((Vector3)box2d.offset, (Vector3)box2d.size);
    }
}
