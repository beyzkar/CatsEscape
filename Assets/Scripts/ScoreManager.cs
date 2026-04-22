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

    // Banked XP from previously completed levels. Static so it survives scene reloads (Retries).
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
        // Cache player for XP icon spawning
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;

        // NEW: Initialize XP and UI immediately on load so persistent XP is displayed at level start
        CalculateXP();
        UpdateUI();
    }

    private void Update()
    {
        // Update distance based on actual player velocity (RIGHTWARD progress only)
        if (PlayerMovement.Instance != null && GameSpeed.Multiplier > 0)
        {
            float currentSpeed = PlayerMovement.Instance.CurrentVelocityX * GameSpeed.Multiplier;
            
            // Only add to distance if moving right (positive velocity)
            // This ensures XP increase is tied to distance covered, not just time spent moving.
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
        // Tiered XP calculation based on distance milestones
        float tempXP = 0;

        if (distance <= 50f) tempXP = distance * 0.2f;
        else if (distance <= 100f) tempXP = 10f + (distance - 50f) * 0.4f; // 10 = 50 * 0.2
        else if (distance <= 150f) tempXP = 30f + (distance - 100f) * 0.6f; // 30 = 10 + 20
        else tempXP = 60f + (distance - 150f) * 1.0f; // 60 = 30 + 30

        // Final XP is the BANKed previous levels XP + current level session XP
        totalXP = persistentXP + Mathf.FloorToInt(tempXP) + bonusXP;
    }

    /// <summary>
    /// Officially "Banks" the current total XP into persistent storage.
    /// Called when a level is successfully completed.
    /// </summary>
    public void CommitSessionXP()
    {
        persistentXP = totalXP;
        
        // Reset session progress so it doesn't carry over as 'earned twice'
        distance = 0f;
        bonusXP = 0;
        
        Debug.Log($"[ScoreManager] XP Committed! New Banked XP: {persistentXP}");
    }

    /// <summary>
    /// Resets all XP to absolute zero. Called for Main Menu or New Game.
    /// </summary>
    public void ResetAllXP()
    {
        persistentXP = 0;
        totalXP = 0;
        bonusXP = 0;
        distance = 0f;
        Debug.Log("[ScoreManager] All XP data wiped (Persistent and Session).");
        UpdateUI();
    }

    public void AddXP(int amount, bool playSound = true)
    {
        bonusXP += amount;
        UpdateUI();
        SpawnXPIcon(amount);

        // Play sound effect
        if (playSound && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayExtraXP();
        }
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
            // Spawn slightly above the player
            Vector3 spawnPos = playerTransform.position + new Vector3(0, 1.5f, 0);
            GameObject instantiated = Instantiate(prefab, spawnPos, Quaternion.identity);
            Debug.Log($"[ScoreManager] Spawned XP icon: {instantiated.name} for amount: {amount}");
        }
        else
        {
            Debug.LogWarning($"[ScoreManager] No prefab assigned for XP amount: {amount}. Check ScoreManager Inspector!");
        }
    }

    private void UpdateUI()
    {
        if (scoreText != null)
        {
            scoreText.text = totalXP.ToString() + " XP";
        }
    }

    public int GetTotalXP()
    {
        return totalXP;
    }
}

