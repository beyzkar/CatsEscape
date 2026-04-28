using UnityEngine;

namespace CatsEscape.UI
{
    /// <summary>
    /// Robust Safe Area implementation for mobile notches and home bars.
    /// Apply this to a UI Panel that contains all your gameplay elements.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        private RectTransform _rectTransform;
        private Rect _lastSafeArea = new Rect(0, 0, 0, 0);
        private Vector2 _lastScreenSize = new Vector2(0, 0);
        private ScreenOrientation _lastOrientation = ScreenOrientation.AutoRotation;

        void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            
            if (_rectTransform == null)
            {
                Debug.LogError("SafeArea: RectTransform component missing!");
                enabled = false;
                return;
            }

            ApplySafeArea();
        }

        void Update()
        {
            // Update if screen size, safe area, or orientation changes (mobile rotation support)
            if (_lastSafeArea != Screen.safeArea || 
                _lastScreenSize.x != Screen.width || _lastScreenSize.y != Screen.height || 
                _lastOrientation != Screen.orientation)
            {
                ApplySafeArea();
            }
        }

        void ApplySafeArea()
        {
            _lastSafeArea = Screen.safeArea;
            _lastScreenSize = new Vector2(Screen.width, Screen.height);
            _lastOrientation = Screen.orientation;

            // Convert safe area rectangle from screen space to normalized anchor space
            Vector2 anchorMin = _lastSafeArea.position;
            Vector2 anchorMax = _lastSafeArea.position + _lastSafeArea.size;

            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            // Apply to RectTransform
            _rectTransform.anchorMin = anchorMin;
            _rectTransform.anchorMax = anchorMax;

            Debug.Log($"[SafeArea] Applied: {_lastSafeArea}. Screen: {Screen.width}x{Screen.height}. Anchors: {anchorMin} to {anchorMax}");
        }
    }
}
