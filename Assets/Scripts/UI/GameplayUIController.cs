using UnityEngine;
using TMPro;

namespace CatsEscape.UI
{
    /// <summary>
    /// Central controller for Gameplay HUD (Hearts, Buttons, XP).
    /// Ensures visibility and provides diagnostic info for mobile issues.
    /// </summary>
    public class GameplayUIController : MonoBehaviour
    {
        public static GameplayUIController Instance { get; private set; }

        [Header("UI Roots")]
        public GameObject hudRoot; // All gameplay UI should be under this
        public GameObject heartsParent;
        public GameObject buttonsParent;
        public GameObject xpLabel;

        [Header("Canvas Settings")]
        public Canvas mainCanvas;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            // FORCE visibility on start in GameScene
            SetHUDVisibility(true);
            ValidateCanvas();
        }

        public void SetHUDVisibility(bool visible)
        {
            if (hudRoot != null) 
            {
                hudRoot.SetActive(visible);
                Debug.Log($"[GameplayUI] HUD Root set to: {visible}");
            }
            
            // Secondary check for key children
            if (visible)
            {
                if (heartsParent != null) heartsParent.SetActive(true);
                if (buttonsParent != null) buttonsParent.SetActive(true);
                if (xpLabel != null) xpLabel.SetActive(true);
            }
        }

        private void ValidateCanvas()
        {
            if (mainCanvas == null) mainCanvas = GetComponentInParent<Canvas>();
            
            if (mainCanvas != null)
            {
                // Root cause prevention: Ensure Canvas is Overlay and has high sorting
                // but lower than critical systems like Transition
                // mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                // mainCanvas.sortingOrder = 10;
                
                Debug.Log($"[GameplayUI] Canvas: {mainCanvas.name}, RenderMode: {mainCanvas.renderMode}, Sorting: {mainCanvas.sortingOrder}");
            }
        }

        /// <summary>
        /// Call this to log UI state from a mobile device (connected to ADB)
        /// </summary>
        [ContextMenu("Log UI Diagnostic")]
        public void LogDiagnostic()
        {
            Debug.Log("=== GAMEPLAY UI DIAGNOSTIC ===");
            LogState("HUD Root", hudRoot);
            LogState("Hearts", heartsParent);
            LogState("Buttons", buttonsParent);
            LogState("XP Label", xpLabel);
            
            if (mainCanvas != null)
            {
                Debug.Log($"CanvasEnabled: {mainCanvas.enabled}, Alpha: {mainCanvas.GetComponent<CanvasGroup>()?.alpha ?? 1f}");
            }
        }

        private void LogState(string name, GameObject obj)
        {
            if (obj == null) Debug.Log($"{name}: NULL");
            else Debug.Log($"{name}: ActiveInHierarchy={obj.activeInHierarchy}, LocalScale={obj.transform.localScale}");
        }
    }
}
