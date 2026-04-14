using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections;

public class TransitionController : MonoBehaviour
{
    public static TransitionController Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private RawImage transitionRawImage;
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Settings")]
    [SerializeField] private float transitionDuration = 1.6f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private bool isTransitioning = false;
    private RenderTexture runtimeRT; // Runtime'da oluşturulan RT

    public bool IsTransitioning => isTransitioning;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        SetupComponents();
    }

    private void SetupComponents()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        if (videoPlayer == null) videoPlayer = GetComponent<VideoPlayer>();

        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.skipOnDrop = false;
            // CRITICAL: Inspector'daki static RT'yi temizle
            videoPlayer.targetTexture = null;
        }

        if (transitionRawImage == null)
            transitionRawImage = GetComponentInChildren<RawImage>();
    }

    private void Start()
    {
        HideImmediate();
    }

    public void StartTransition(System.Action onComplete = null)
    {
        if (isTransitioning)
        {
            LogWarning("Transition already in progress, ignoring.");
            return;
        }
        gameObject.SetActive(true); // Ensure object is active BEFORE starting coroutine
        StartCoroutine(TransitionSequence(onComplete));
    }

    private IEnumerator TransitionSequence(System.Action onComplete)
    {
        isTransitioning = true;
        Log("=== TRANSITION START ===");

        // STEP 1: OYUNU DONDUR (Claude bunu silmiş)
        GameSpeed.Multiplier = 0f;

        // Step 2: Panel'i göster ve EN ÖNE AL
        SetupOverlayCanvas();
        gameObject.SetActive(true);
        canvasGroup.alpha = 1f; // Artık direkt açalım
        
        // TEŞHİS: Videodan önce ekranı pembeye boyayalım. 
        // Eğer pembe ekranı görüyorsan panel "burada" demektir.
        if (transitionRawImage != null) 
        {
            transitionRawImage.enabled = true;
            transitionRawImage.color = Color.magenta; 
        }

        // Step 3: RT oluştur ve videoyu hazırla
        CreateRuntimeRenderTexture();
        yield return StartCoroutine(PrepareAndPlay());

        // Step 4: Video hazır, pembeyi beyaza (videoya) çevir
        if (transitionRawImage != null) transitionRawImage.color = Color.white;
        Log($"Video playing: {videoPlayer.isPlaying}, frame: {videoPlayer.frame}");

        // NEW: Get actual duration of the video
        float videoDuration = (float)videoPlayer.length;
        if (videoDuration <= 0) videoDuration = transitionDuration;

        // Step 5: Süre bekle (Videonun gerçek süresi kadar)
        yield return new WaitForSecondsRealtime(videoDuration);

        // Step 6: Tamamla
        HideImmediate();
        onComplete?.Invoke();
        
        // Oyunu devam ettir
        GameSpeed.Multiplier = 1f;
        isTransitioning = false;
        Log("=== TRANSITION COMPLETE ===");
    }

    private void CreateRuntimeRenderTexture()
    {
        // Eski RT'yi temizle
        if (runtimeRT != null)
        {
            runtimeRT.Release();
            Destroy(runtimeRT);
        }

        // Proje renk uzayına göre sRGB ayarla
        bool useSRGB = (QualitySettings.activeColorSpace == ColorSpace.Gamma);

        var descriptor = new RenderTextureDescriptor(Screen.width, Screen.height,
            RenderTextureFormat.ARGB32, 0)
        {
            sRGB = useSRGB
        };

        runtimeRT = new RenderTexture(descriptor);
        runtimeRT.name = "TransitionRT_Runtime";
        runtimeRT.Create();

        // VideoPlayer ve RawImage'e ata
        videoPlayer.targetTexture = runtimeRT;
        if (transitionRawImage != null)
            transitionRawImage.texture = runtimeRT;

        Log($"Runtime RT created: {Screen.width}x{Screen.height}, sRGB={useSRGB}");
    }

    private IEnumerator PrepareAndPlay()
    {
        if (videoPlayer.clip == null)
        {
            LogError("VideoClip is null!");
            yield break;
        }

        // Temiz başlangıç
        videoPlayer.Stop();

        // Prepare
        videoPlayer.Prepare();
        float timeout = Time.realtimeSinceStartup + 5f;
        yield return new WaitUntil(() =>
            videoPlayer.isPrepared || Time.realtimeSinceStartup > timeout);

        if (!videoPlayer.isPrepared)
        {
            LogError("Video prepare TIMED OUT!");
            yield break;
        }
        Log("Video prepared.");

        // Play
        videoPlayer.Play();

        // CRITICAL FIX: frame >= 1 bekle (frame 0 = boş buffer on Mac)
        timeout = Time.realtimeSinceStartup + 3f;
        yield return new WaitUntil(() =>
            videoPlayer.frame >= 1 || Time.realtimeSinceStartup > timeout);

        Log($"First real frame received. frame={videoPlayer.frame}, isPlaying={videoPlayer.isPlaying}");
    }

    private void SetupOverlayCanvas()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; // Camera bağımsız!
        canvas.sortingOrder = 9999;

        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

        // CanvasGroup zincirini kır
        canvasGroup.ignoreParentGroups = true;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;

        // Full screen
        RectTransform rt = GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        if (transitionRawImage != null)
        {
            transitionRawImage.color = Color.white;
            
            // FORCE RawImage to be full screen
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

    public void HideImmediate()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }

        if (transitionRawImage != null)
            transitionRawImage.enabled = false;

        Log("Panel hidden.");
    }

    public void ForceReset()
    {
        isTransitioning = false;
        HideImmediate();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;

        // RT bellek temizliği
        if (runtimeRT != null)
        {
            runtimeRT.Release();
            Destroy(runtimeRT);
        }
    }

    private void Log(string msg) { if (showDebugLogs) Debug.Log($"[TransitionController] {msg}"); }
    private void LogWarning(string msg) { if (showDebugLogs) Debug.LogWarning($"[TransitionController] {msg}"); }
    private void LogError(string msg) { Debug.LogError($"[TransitionController] {msg}"); }
}