using System.Collections;
using UnityEngine;

/// <summary>
/// Static world-space portal for Level 5. Spawned by LevelManager when progress reaches the target count.
/// This script is modular and static, not relying on ObstacleMove.
/// </summary>
public class FinalPortal : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("How far ahead of the player the portal should spawn (Fallback only).")]
    [SerializeField] private float forwardOffset = 2.2f;

    [Tooltip("Fine-tuning for the portal spawn center (Used for Bridge Edge anchoring).")]
    [SerializeField] private float anchorOffset = 2.5f;

    [Tooltip("Vertical spawn height relative to default ground Y.")]
    [SerializeField] private float spawnYOffset = 4.0f;

    [Header("Visual Effects (Appear)")]
    [SerializeField] private float appearDuration = 0.6f;
    [SerializeField] private Vector3 startScale = new Vector3(0.1f, 0.1f, 1f);
    [SerializeField] private AnimationCurve appearCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Final Sequence Settings")]
    [Tooltip("How long it takes to pull and shrink the player.")]
    [SerializeField] private float absorptionDuration = 0.8f;
    [Tooltip("Delay after the sequence before opening the score panel.")]
    [SerializeField] private float resultPanelDelay = 0.6f;

    [Header("Spawn Reference (Optional)")]
    [Tooltip("Manually assign an empty GameObject to act as the spawn anchor. If null, the system automatically uses the end of the last bridge.")]
    [SerializeField] private Transform customSpawnAnchor;

    private SpriteRenderer portalRenderer;
    private BoxCollider2D triggerCollider;
    private Vector3 originalScale;
    private bool isSpawned = false;
    private bool isSequenceStarted = false;

    private void Awake()
    {
        portalRenderer = GetComponent<SpriteRenderer>();
        triggerCollider = GetComponent<BoxCollider2D>();
        originalScale = transform.localScale;

        // Ensure the portal starts hidden and non-interactive
        ResetPortalState();
    }

    private void Update()
    {
        // Scroll left with the world if spawned and the sequence hasn't locked the position
        if (isSpawned && !isSequenceStarted)
        {
            if (PlayerMovement.Instance != null && Time.timeScale > 0f)
            {
                // Sync movement with GroundSpawner logic
                float moveStep = PlayerMovement.Instance.CurrentVelocityX * GameSpeed.Multiplier * Time.deltaTime;
                transform.Translate(Vector3.left * moveStep);
            }
        }
    }

    /// <summary>
    /// Ensures the portal is visually hidden and has its collider disabled.
    /// </summary>
    public void ResetPortalState()
    {
        if (portalRenderer != null)
        {
            Color c = portalRenderer.color;
            c.a = 0f;
            portalRenderer.color = c;
        }

        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
            triggerCollider.enabled = false;
        }

        transform.localScale = startScale;
        isSpawned = false;
        isSequenceStarted = false;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Called when Level 5 progress is reset (e.g. hitting a pit).
    /// </summary>
    public void ResetAfterLevelProgressCleared()
    {
        StopAllCoroutines();
        ResetPortalState();
    }

    /// <summary>
    /// Triggered by LevelManager when target obstacle count is reached.
    /// </summary>
    public void TrySpawnFromLevelManager()
    {
        if (isSpawned) return;

        isSpawned = true;
        gameObject.SetActive(true);

        // Position the portal using the bridge-end or custom anchor
        if (PlayerMovement.Instance != null)
        {
            float finalX;

            // 1. Priority: Custom Anchor
            if (customSpawnAnchor != null)
            {
                finalX = customSpawnAnchor.position.x + anchorOffset;
            }
            // 2. Automatic: End edge of the last physical bridge segment (via GroundSpawner)
            else if (GroundSpawner.Instance != null)
            {
                // Stable physical anchor: lastBridgeX + half width = exact right edge
                float lastBridgeX = GroundSpawner.Instance.GetLastBridgeX();
                float bridgeWidth = GroundSpawner.Instance.groundWidth;
                float bridgeEdgeX = lastBridgeX + (bridgeWidth / 2.0f);

                finalX = bridgeEdgeX + anchorOffset;
            }
            // 3. Fallback: Player relative
            else
            {
                finalX = PlayerMovement.Instance.transform.position.x + forwardOffset;
            }

            // Ground-anchored Y: Stay perfectly grounded relative to the bridge level
            float groundBaseY = (GroundSpawner.Instance != null) ? GroundSpawner.Instance.groundY : -2.5f;
            float finalY = groundBaseY + spawnYOffset; 

            transform.position = new Vector3(finalX, finalY, transform.position.z);
        }

        StartCoroutine(AppearSequence());
    }

    private IEnumerator AppearSequence()
    {
        float elapsed = 0f;
        while (elapsed < appearDuration)
        {
            elapsed += Time.deltaTime;
            float t = appearCurve.Evaluate(elapsed / appearDuration);

            // Smooth fade-in
            if (portalRenderer != null)
            {
                Color c = portalRenderer.color;
                c.a = Mathf.Lerp(0f, 1f, t);
                portalRenderer.color = c;
            }

            // Smooth scale-up
            transform.localScale = Vector3.Lerp(startScale, originalScale, t);

            yield return null;
        }

        // Enable interaction once visualization is complete
        if (triggerCollider != null) triggerCollider.enabled = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isSequenceStarted) return;
        if (!other.CompareTag("Player")) return;

        // Prevent multiple triggers
        isSequenceStarted = true;
        if (triggerCollider != null) triggerCollider.enabled = false;

        StartCoroutine(FinalAbsorptionSequence(other.transform));
    }

    private IEnumerator FinalAbsorptionSequence(Transform playerTransform)
    {
        // 1. Disable player input & stop world movement
        PlayerMovement pm = playerTransform.GetComponent<PlayerMovement>();
        if (pm != null)
        {
            pm.SetPortalExitSequenceActive(true);
            pm.FreezeForTransition(true);
        }

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.SetTargetSpeed(0f);
        }
        GameSpeed.Multiplier = 0f;

        // 2. Prepare player visuals for fading
        SpriteRenderer[] playerSprites = playerTransform.GetComponentsInChildren<SpriteRenderer>();
        Vector3 startPos = playerTransform.position;
        Vector3 startScalePlayer = playerTransform.localScale;
        Vector3 targetPos = transform.position;

        float elapsed = 0f;
        while (elapsed < absorptionDuration)
        {
            elapsed += Time.unscaledDeltaTime; // Use unscaled as GameSpeed might be 0
            float t = elapsed / absorptionDuration;
            float smoothT = t * t * (3f - 2f * t); // Smoothstep

            // Pull toward center
            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, smoothT);
            if (pm != null) pm.SetPortalDrivenWorldPosition(currentPos);
            else playerTransform.position = currentPos;

            // Reduce scale
            playerTransform.localScale = Vector3.Lerp(startScalePlayer, Vector3.zero, smoothT);

            // Fade out visuals
            foreach (var sr in playerSprites)
            {
                if (sr == null) continue;
                Color c = sr.color;
                c.a = Mathf.Lerp(1f, 0f, smoothT);
                sr.color = c;
            }

            yield return null;
        }

        // 3. Wait for final impact/delay
        yield return new WaitForSecondsRealtime(resultPanelDelay);

        // 4. Open Final UI
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.OpenFinalVictoryAfterPortalExit();
        }
    }
}

