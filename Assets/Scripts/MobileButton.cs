using UnityEngine;
using UnityEngine.EventSystems;

// Handles mobile UI button interactions for character movement and jumping
public class MobileButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public enum ButtonType { Left, Right, Up }
    
    [Header("Settings")]
    public ButtonType type;
    [SerializeField] private PlayerMovement player;

    private void Start()
    {
        // Only search for player if not assigned in Inspector
        if (player == null)
        {
            GameObject pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null) player = pObj.GetComponent<PlayerMovement>();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (player == null) 
        {
            Debug.LogError($"[MOBILE_INPUT] {gameObject.name} (Type: {type}) has NO Player assigned!");
            return;
        }

        Debug.Log($"[MOBILE_INPUT] {type} button down at {eventData.position}");

        switch (type)
        {
            case ButtonType.Left:
                player.SetMoveLeft(true);
                break;
            case ButtonType.Right:
                player.SetMoveRight(true);
                break;
            case ButtonType.Up:
                player.MobileJumpDown();
                break;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (player == null) return;

        Debug.Log($"[MOBILE_INPUT] {type} button up");

        // Reset movement flags on button release (except for Up/Jump)
        switch (type)
        {
            case ButtonType.Left:
                player.SetMoveLeft(false);
                break;
            case ButtonType.Right:
                player.SetMoveRight(false);
                break;
        }
    }
}
