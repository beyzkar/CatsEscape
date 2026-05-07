using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("UI References")]
    public TextMeshProUGUI scoreText;

    private float distance = 0f;
    private int totalXP = 0;
    private int bonusXP = 0;
    private bool isTransitioning = false;

    // The XP value at the absolute start of the current level (survives scene reload)
    private static int levelStartXP = 0;
    
    // The base XP from previous levels, excluding current session
    private static int persistentXP = 0;

    [Header("XP Prefabs")]
    public GameObject xp20Prefab;
    public GameObject xp50Prefab;
    public GameObject xp75Prefab;
    public Transform playerTransform;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;

        CalculateXP();
        UpdateUI();
    }

    private void Update()
    {
        if (isTransitioning) return;

        if (PlayerMovement.Instance != null && GameSpeed.Multiplier > 0)
        {
            float currentSpeed = PlayerMovement.Instance.CurrentVelocityX * GameSpeed.Multiplier;
            if (currentSpeed > 0)
            {
                distance += currentSpeed * Time.deltaTime;
                CalculateXP();
                UpdateUI();
            }
        }
    }

    private void CalculateXP()
    {
        if (isTransitioning) return;

        float tempXP = 0;
        if (distance <= 50f) tempXP = distance * 0.2f;
        else if (distance <= 100f) tempXP = 10f + (distance - 50f) * 0.4f;
        else if (distance <= 150f) tempXP = 30f + (distance - 100f) * 0.6f;
        else tempXP = 60f + (distance - 150f) * 1.0f;

        int sessionXP = Mathf.FloorToInt(tempXP);
        int newTotal = persistentXP + sessionXP + bonusXP;

        if (newTotal != totalXP)
        {
            totalXP = newTotal;
        }
    }

    public void CommitSessionXP()
    {
        isTransitioning = true;
        
        // Final total becomes the new base for the next level
        persistentXP = totalXP;
        levelStartXP = totalXP; // For retry logic in the NEXT level
        

        distance = 0f;
        bonusXP = 0;
        
        CalculateXP();
        UpdateUI();
        
        isTransitioning = false;
    }

    public static void ResetAllXP()
    {
        levelStartXP = 0;
        persistentXP = 0;
        if (Instance != null)
        {
            Instance.totalXP = 0;
            Instance.bonusXP = 0;
            Instance.distance = 0f;
            Instance.UpdateUI();
        }
    }

    public static void SetTotalXP(int amount, string reason = "Forced")
    {
        if (amount < 0) amount = 0;
        
        bool isMainMenu = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainMenu";
        if (!isMainMenu && amount > (persistentXP + 20000))
        {
            return;
        }

        persistentXP = amount;
        levelStartXP = amount; 

        if (Instance != null)
        {
            Instance.totalXP = amount;
            Instance.bonusXP = 0;
            Instance.distance = 0f;
            Instance.UpdateUI();
        }
    }

    public static void InitializeForLevel(int level, int startXP, string source)
    {
        if (level <= 1)
        {
            ResetAllXP();
            return;
        }

        levelStartXP = startXP;
        persistentXP = startXP;

        if (Instance != null)
        {
            Instance.totalXP = startXP;
            Instance.bonusXP = 0;
            Instance.distance = 0f;
            Instance.UpdateUI();
        }

    }

    public static void ResetToLevelStartXP()
    {
        int level = (LevelManager.Instance != null) ? LevelManager.Instance.currentLevel : 1;
        persistentXP = levelStartXP;

        if (Instance != null)
        {
            Instance.totalXP = levelStartXP;
            Instance.bonusXP = 0;
            Instance.distance = 0f;
            Instance.UpdateUI();
        }
    }

    public void AddXP(int amount, bool playSound = true)
    {
        bonusXP += amount;
        UpdateUI();
        SpawnXPIcon(amount);

        if (playSound && AudioManager.Instance != null)
            AudioManager.Instance.PlayExtraXP();
    }

    private void SpawnXPIcon(int amount)
    {
        if (playerTransform == null) return;

        GameObject prefab = null;
        if (amount == 20) prefab = xp20Prefab;
        else if (amount == 50) prefab = xp50Prefab;
        else if (amount == 75) prefab = xp75Prefab;

        if (prefab != null)
        {
            Vector3 spawnPos = playerTransform.position + new Vector3(0, 1.5f, 0);
            Instantiate(prefab, spawnPos, Quaternion.identity);
        }
    }

    private void UpdateUI()
    {
        if (scoreText != null) scoreText.text = totalXP.ToString() + " XP";

        if (CatsEscape.Auth.AuthManager.Instance != null)
        {
            // Only update highestXP (stat), NEVER LastSavedXP here!
            CatsEscape.Auth.AuthManager.Instance.HighestXP = totalXP;
        }
    }

    public int GetTotalXP() => totalXP;
}
