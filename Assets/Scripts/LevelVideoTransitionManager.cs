using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

[RequireComponent(typeof(Canvas), typeof(CanvasGroup), typeof(GraphicRaycaster))]
public class LevelVideoTransitionManager : MonoBehaviour
{
    public static LevelVideoTransitionManager Instance { get; private set; }

    [Header("UI Components")]
    [SerializeField] private RawImage transitionRawImage;
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Transition Text")]
    [SerializeField] private string statusMessage = "Closer to Home";
    [SerializeField] private bool showStatusText = true;

    [Header("Video Settings")]
    [SerializeField] private VideoClip defaultVideoClip;
    [SerializeField] private VideoClip[] levelVideos = new VideoClip[5];
    [SerializeField] private float videoPlaybackStartDelay = 0f;

    [Header("Settings")]
    [SerializeField] private bool useFadeEffect = false;
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.3f;
    [Tooltip("If this object has an Animator that drives CanvasGroup/RawImage alpha, disable it during the transition to avoid mid-video fading.")]
    [SerializeField] private bool disableAnimatorDuringTransition = true;
    [SerializeField] private bool pauseMusicOnTransition = true;
    [SerializeField] private bool showDebugLogs = false;

    [Header("Scene Settings")]
    [SerializeField] private string nextSceneName = "";
    [SerializeField] private int nextSceneIndex = -1;

    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private RenderTexture renderTexture;
    private bool isTransitioning = false;
    private bool doubleTriggerGuard = false;
    private bool videoLoopPointReached;
    private AudioSource backgroundMusicSource;

    public bool IsTransitioning => isTransitioning;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        SetupComponents();
    }

    private void SetupComponents()
    {
        canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        // Nested under another Canvas: without this, sorting order is ignored and the panel can draw behind game/UI.
        if (transform.parent != null)
            canvas.overrideSorting = true;

        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        // Parent CanvasGroup alpha / blocksRaycasts must not hide or block this overlay.
        canvasGroup.ignoreParentGroups = true;
        
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();
        if (videoPlayer == null)
            videoPlayer = gameObject.AddComponent<VideoPlayer>();

        videoPlayer.playOnAwake = false;
        videoPlayer.skipOnDrop = false;
        videoPlayer.waitForFirstFrame = true;
        // Avoid stale RenderTexture / Camera references from the Inspector when using runtime-created RTs.
        videoPlayer.targetTexture = null;
        videoPlayer.targetCamera = null;
        videoPlayer.loopPointReached += OnVideoFinished;

        if (transitionRawImage == null)
        {
            transitionRawImage = GetComponentInChildren<RawImage>();
        }

        SetupRectTransform();
    }

    private void SetupRectTransform()
    {
        RectTransform rt = GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        // FORCE RawImage to be full screen as well
        if (transitionRawImage != null)
        {
            RectTransform rawRT = transitionRawImage.GetComponent<RectTransform>();
            if (rawRT != null)
            {
                rawRT.anchorMin = Vector2.zero;
                rawRT.anchorMax = Vector2.one;
                rawRT.offsetMin = Vector2.zero;
                rawRT.offsetMax = Vector2.zero;
                rawRT.localScale = Vector3.one;
            }
        }
    }

    private void Start()
    {
        HideImmediately();
        FindBackgroundMusic();
    }

    private void FindBackgroundMusic()
    {
        AudioManager audioManager = AudioManager.Instance;
        if (audioManager != null)
        {
            backgroundMusicSource = audioManager.GetComponent<AudioSource>();
        }
    }

    public void StartTransition()
    {
        if (isTransitioning || doubleTriggerGuard)
        {
            Log("Transition already in progress, ignoring.");
            return;
        }

        gameObject.SetActive(true); // Ensure object is active BEFORE starting coroutine
        StartCoroutine(TransitionSequence());
    }

    public void StartTransition(System.Action onComplete)
    {
        if (isTransitioning || doubleTriggerGuard)
        {
            Log("Transition already in progress, ignoring.");
            return;
        }

        gameObject.SetActive(true); // Ensure object is active BEFORE starting coroutine
        StartCoroutine(TransitionSequenceWithCallback(onComplete));
    }

    private IEnumerator TransitionSequence()
    {
        yield return StartCoroutine(TransitionSequenceInternal(null));
    }

    private IEnumerator TransitionSequenceWithCallback(System.Action onComplete)
    {
        yield return StartCoroutine(TransitionSequenceInternal(onComplete));
    }

    private IEnumerator TransitionSequenceInternal(System.Action onComplete)
    {
        if (isTransitioning) yield break;
        isTransitioning = true;
        doubleTriggerGuard = true;

        Animator transitionAnimator = GetComponent<Animator>();
        bool animatorRestore = false;
        if (transitionAnimator != null && disableAnimatorDuringTransition)
        {
            animatorRestore = transitionAnimator.enabled;
            transitionAnimator.enabled = false;
        }

        Log("=== TRANSITION START ===");

        try
        {
            // Step 1: FULL FREEZE
            FreezeGame();
            
            // Ensure player is stopped (Redundancy)
            if (PlayerMovement.Instance != null)
                PlayerMovement.Instance.FreezeForTransition(true);

            if (pauseMusicOnTransition && backgroundMusicSource != null && backgroundMusicSource.isPlaying)
            {
                backgroundMusicSource.Pause();
            }

            // Step 2: PREPARE
            SetupVideoForCurrentLevel();
            CreateRenderTexture();

            // Step 3: SHOW PANEL (optional fade)
            ShowPanel();
            if (useFadeEffect)
            {
                yield return StartCoroutine(FadeIn());
            }
            else
            {
                canvasGroup.alpha = 1f;
            }

            // Step 4: PLAY VIDEO
            yield return StartCoroutine(PlayVideo());

            // Step 5: FADE OUT (optional; keep off for instant handoff after clip)
            if (useFadeEffect)
            {
                yield return StartCoroutine(FadeOut());
            }

            HidePanel();

            Log("=== VIDEO FINISHED ===");

            // Step 6: CALLBACK & LEVEL CHANGE
            onComplete?.Invoke();

            LoadNextScene();
        }
        finally
        {
            if (transitionAnimator != null && disableAnimatorDuringTransition)
                transitionAnimator.enabled = animatorRestore;

            isTransitioning = false;
            doubleTriggerGuard = false;
        }
    }

    private void FreezeGame()
    {
        GameSpeed.Multiplier = 0f;
        Log("Game frozen.");
    }

    private void UnfreezeGame()
    {
        GameSpeed.Multiplier = 1f;
        if (pauseMusicOnTransition && backgroundMusicSource != null)
        {
            backgroundMusicSource.UnPause();
        }
        Log("Game unfrozen.");
    }

    private void SetupVideoForCurrentLevel()
    {
        int currentLevel = LevelManager.Instance != null ? LevelManager.Instance.currentLevel : 1;
        int levelIndex = Mathf.Clamp(currentLevel - 1, 0, levelVideos.Length - 1);

        VideoClip clipToUse = defaultVideoClip;
        if (levelVideos != null && levelVideos.Length > levelIndex && levelVideos[levelIndex] != null)
        {
            clipToUse = levelVideos[levelIndex];
        }

        if (clipToUse == null)
        {
            LogWarning("No video clip assigned! Using default or none.");
            return;
        }

        videoPlayer.clip = clipToUse;
        videoPlayer.Prepare();

        Log($"Video prepared: {clipToUse.name}");
    }

    private void CreateRenderTexture()
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }

        int w = Mathf.Max(1, Screen.width);
        int h = Mathf.Max(1, Screen.height);
        bool srgb = QualitySettings.activeColorSpace == ColorSpace.Gamma;
        var desc = new RenderTextureDescriptor(w, h, RenderTextureFormat.ARGB32, 0)
        {
            sRGB = srgb
        };
        renderTexture = new RenderTexture(desc);
        renderTexture.name = "TransitionVideoRT";
        renderTexture.Create();

        videoPlayer.targetTexture = renderTexture;
        
        if (transitionRawImage != null)
        {
            transitionRawImage.texture = renderTexture;
        }

        Log($"RenderTexture created: {renderTexture.width}x{renderTexture.height}");
    }

    private void ShowPanel()
    {
        gameObject.SetActive(true);
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;

        if (transitionRawImage != null)
        {
            transitionRawImage.enabled = true;
            transitionRawImage.color = Color.white;
        }

        if (showStatusText && statusText != null)
        {
            statusText.text = statusMessage;
            statusText.gameObject.SetActive(true);
        }

        Log("Panel shown.");
    }

    private void HidePanel()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        if (transitionRawImage != null)
        {
            transitionRawImage.enabled = false;
        }

        if (statusText != null)
        {
            statusText.gameObject.SetActive(false);
        }

        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }

        Log("Panel hidden.");
    }

    private IEnumerator FadeIn()
    {
        float elapsed = 0f;
        canvasGroup.alpha = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    private IEnumerator FadeOut()
    {
        float elapsed = 0f;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeOutDuration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
    }

    private IEnumerator PlayVideo()
    {
        Log("Starting video playback...");

        float startTimeout = Time.realtimeSinceStartup + 5f;
        yield return new WaitUntil(() => videoPlayer.isPrepared || Time.realtimeSinceStartup > startTimeout);

        if (!videoPlayer.isPrepared)
        {
            LogError("Video failed to prepare!");
            yield break;
        }

        videoPlayer.isLooping = false;
        videoLoopPointReached = false;

        videoPlayer.Play();

        if (videoPlaybackStartDelay > 0f)
            yield return new WaitForSecondsRealtime(videoPlaybackStartDelay);

        // First frame can be empty on some platforms
        float frameTimeout = Time.realtimeSinceStartup + 3f;
        yield return new WaitUntil(() => videoPlayer.frame >= 1 || Time.realtimeSinceStartup > frameTimeout);

        float clipLen = (float)videoPlayer.length;
        if (clipLen <= 0f) clipLen = 5f;
        float failSafeEnd = Time.realtimeSinceStartup + clipLen + 2f;

        yield return new WaitUntil(() =>
            videoLoopPointReached ||
            Time.realtimeSinceStartup > failSafeEnd);

        Log("Video playback complete.");
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        videoLoopPointReached = true;
        Log("Video finished event received.");
    }

    private void LoadNextScene()
    {
        UnfreezeGame();

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            Log($"Loading scene: {nextSceneName}");
            SceneManager.LoadScene(nextSceneName);
        }
        else if (nextSceneIndex >= 0)
        {
            Log($"Loading scene index: {nextSceneIndex}");
            SceneManager.LoadScene(nextSceneIndex);
        }
        else
        {
            LogWarning("No scene configured. Use nextSceneName or nextSceneIndex.");
        }
    }

    public void SetNextScene(string sceneName)
    {
        nextSceneName = sceneName;
        nextSceneIndex = -1;
    }

    public void SetNextSceneIndex(int sceneIndex)
    {
        nextSceneIndex = sceneIndex;
        nextSceneName = "";
    }

    public void SetVideoClip(VideoClip clip, int levelIndex = -1)
    {
        if (levelIndex >= 0 && levelIndex < levelVideos.Length)
        {
            levelVideos[levelIndex] = clip;
        }
        else
        {
            defaultVideoClip = clip;
        }
    }

    private void HideImmediately()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        if (statusText != null)
        {
            statusText.gameObject.SetActive(false);
        }

        gameObject.SetActive(false);
    }

    public void ForceReset()
    {
        isTransitioning = false;
        doubleTriggerGuard = false;
        HideImmediately();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;

        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
        }

        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }
    }

    private void Log(string msg)
    {
        if (showDebugLogs) Debug.Log($"[LevelVideoTransition] {msg}");
    }

    private void LogWarning(string msg)
    {
        if (showDebugLogs) Debug.LogWarning($"[LevelVideoTransition] {msg}");
    }

    private void LogError(string msg)
    {
        Debug.LogError($"[LevelVideoTransition] {msg}");
    }
}