using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonHaptic : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    // Butona basılı tutulduğunda titreşimin tekrarlanmasını engellemek için flag
    private bool isPressed = false;

    public void OnPointerDown(PointerEventData eventData)
    {
        // Eğer zaten basılı değilse (ilk basış anıysa)
        if (!isPressed)
        {
            isPressed = true;
            HapticManager.LightTap();
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Parmak butondan kalktığında state'i sıfırla
        isPressed = false;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Parmak butonun dışına kayarsa state'i sıfırla
        isPressed = false;
    }
}
