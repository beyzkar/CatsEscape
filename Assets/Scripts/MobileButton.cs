using UnityEngine;
using UnityEngine.EventSystems;

// Handles mobile UI button interactions for character movement and jumping
public class MobileButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    public enum ButtonType { Left, Right, Up }
    
    [Header("Settings")]
    public ButtonType type;
    [SerializeField] private PlayerMovement player;

    private void Start()
    {
        if (player == null)
        {
            GameObject pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null) player = pObj.GetComponent<PlayerMovement>();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        HandleAction(true, true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        HandleAction(false, true);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Directional sliding: Only for Left and Right
        if (type != ButtonType.Up && (eventData.pointerId != -1 || Input.touchCount > 0))
        {
            HandleAction(true, false);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (type != ButtonType.Up)
        {
            HandleAction(false, false);
        }
    }

    private void HandleAction(bool isActive, bool isExplicitTap)
    {
        if (player == null) return;

        switch (type)
        {
            case ButtonType.Left:
                player.SetMoveLeft(isActive);
                break;
            case ButtonType.Right:
                player.SetMoveRight(isActive);
                break;
            case ButtonType.Up:
                // Jump MUST be an explicit tap (OnPointerDown) to prevent double-triggering
                // and to prevent accidental jumps while sliding between left/right.
                if (isActive && isExplicitTap) 
                {
                    player.MobileJumpDown();
                }
                break;
        }
    }
}
