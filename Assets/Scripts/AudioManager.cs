using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource backgroundSource;
    public AudioSource sfxSource;

    [Header("Background & UI Clips")]
    public AudioClip backgroundMusic;
    [Range(0f, 1f)] public float backgroundMusicVolume = 1f;
    public AudioClip gameOverClip;
    [Range(0f, 1f)] public float gameOverVolume = 1f;
    public AudioClip levelWinClip;
    [Range(0f, 1f)] public float levelWinVolume = 1f;
    public AudioClip finalWinClip;
    [Range(0f, 1f)] public float finalWinVolume = 1f;

    [Header("Gameplay SFX Clips")]
    public AudioClip jumpSfx;
    [Range(0f, 1f)] public float jumpVolume = 1f;
    public AudioClip crushSfx;
    [Range(0f, 1f)] public float crushVolume = 1f;
    public AudioClip gameOverSfx;
    [Range(0f, 1f)] public float gameOverSfxVolume = 1f;
    public AudioClip hitWallAudio;
    [Range(0f, 1f)] public float hitWallVolume = 1f;
    public AudioClip heartLostSfx;
    [Range(0f, 1f)] public float heartLostVolume = 1f;
    public AudioClip heartFillSfx;
    [Range(0f, 1f)] public float heartFillVolume = 1f;
    public AudioClip extraXPSfx;
    [Range(0f, 1f)] public float extraXPVolume = 1f;
    public AudioClip potionIncreaseSfx;
    [Range(0f, 1f)] public float potionIncreaseVolume = 1f;
    public AudioClip potionDecreaseSfx;
    [Range(0f, 1f)] public float potionDecreaseVolume = 1f;
    public AudioClip fallingSfx;
    [Range(0f, 1f)] public float fallingVolume = 1f;

    [Header("Global Settings")]
    [Range(0f, 1f)]
    public float backgroundGlobalVolume = 0.5f;
    [Range(0f, 1f)]
    public float sfxGlobalVolume = 0.5f;

    private void Awake()
    {
        //Singleton yapısı sayesinde oyunun herhangi bir yerinde müziğin kesilmeden devam etmesini sağlıyor
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSources();
            Debug.Log("[AudioManager] Singleton initialized and persistent.");
        }
        else
        {
            Debug.Log("[AudioManager] Duplicate detected in " + SceneManager.GetActiveScene().name + ". Destroying.");
            Destroy(gameObject);
            return;
        }
    }

    private void InitializeSources() //akıllı ses ekleme (eğer bir ses dosyasını eklemeyi unutursam kod bunu otomatik olarak ekler)
    {
        // Check for existing sources or add new ones
        AudioSource[] sources = GetComponents<AudioSource>();
        
        if (backgroundSource == null && sources.Length > 0) backgroundSource = sources[0];
        if (sfxSource == null && sources.Length > 1) sfxSource = sources[1];

        if (backgroundSource == null) backgroundSource = gameObject.AddComponent<AudioSource>();
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();

        backgroundSource.loop = true;
        backgroundSource.playOnAwake = false;
        
        Debug.Log("[AudioManager] Sources initialized: BG=" + (backgroundSource != null) + ", SFX=" + (sfxSource != null));
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (Instance == this)
        {
            PlayBackgroundMusic();
        }
    }

    private void Update()
    {
        if (Instance != this) return;

        if (backgroundSource != null)
        {
            backgroundSource.volume = backgroundGlobalVolume * backgroundMusicVolume;
        }
        if (sfxSource != null)
        {
            sfxSource.volume = sfxGlobalVolume;
        }
    }

    private void Start()
    {
        if (Instance == this)
        {
            PlayBackgroundMusic();
        }
    }

    public void PlayBackgroundMusic()
    {
        if (backgroundMusic != null && backgroundSource != null && !backgroundSource.isPlaying)
        {
            backgroundSource.clip = backgroundMusic;
            backgroundSource.Play();
            Debug.Log("[AudioManager] Playing background music.");
        }
    }

    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip, volumeScale);
            // Debug statements can be noisy, but useful for debugging regression
            // Debug.Log("[AudioManager] Playing SFX: " + clip.name + " at scale " + volumeScale);
        }
        else if (clip == null)
        {
            Debug.LogWarning("[AudioManager] Attempted to play a null SFX clip.");
        }
    }

    public void StopBackgroundMusic()
    {
        if (backgroundSource != null && backgroundSource.isPlaying)
        {
            backgroundSource.Stop();
        }
    }

    // --- Specific Play Methods ---

    public void PlayJump() { PlaySFX(jumpSfx, jumpVolume); }
    public void PlayCrush() { PlaySFX(crushSfx, crushVolume); }
    public void PlayGameOverSFX() { PlaySFX(gameOverSfx, gameOverSfxVolume); }
    public void PlayHitWall() { PlaySFX(hitWallAudio, hitWallVolume); }
    public void PlayHeartLost() { PlaySFX(heartLostSfx, heartLostVolume); }
    public void PlayHeartFill() { PlaySFX(heartFillSfx, heartFillVolume); }
    public void PlayExtraXP() { PlaySFX(extraXPSfx, extraXPVolume); }
    public void PlayPotionIncrease() { PlaySFX(potionIncreaseSfx, potionIncreaseVolume); }
    public void PlayPotionDecrease() { PlaySFX(potionDecreaseSfx, potionDecreaseVolume); }
    public void PlayFalling() { PlaySFX(fallingSfx, fallingVolume); }

    public void PlayGameOverSound()
    {
        if (gameOverClip != null) PlaySFX(gameOverClip, gameOverVolume);
    }

    public void PlayLevelWinSound()
    {
        if (levelWinClip != null) PlaySFX(levelWinClip, levelWinVolume);
    }

    public void PlayFinalWinSound()
    {
        if (finalWinClip != null) PlaySFX(finalWinClip, finalWinVolume);
    }
}
