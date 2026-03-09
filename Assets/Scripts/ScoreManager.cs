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

    [Header("XP Prefabs")]
    public GameObject xp5Prefab;
    public GameObject xp20Prefab;
    public GameObject xp50Prefab;
    public Transform playerTransform;

    [Header("Combo Settings")]
    private int cleanJumpsCount = 0;
    private const int COMBO_THRESHOLD = 5;
    private const int COMBO_XP_BONUS = 5;

    // local extraXPSfx removed
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        if (GameSpeed.Multiplier > 0)
        {
            // Update distance based on the ground scroll speed
            if (GroundScroll.Instance != null)
            {
                float currentSpeed = GroundScroll.Instance.speed * GameSpeed.Multiplier;
                distance += currentSpeed * Time.deltaTime;
                
                CalculateXP();
                UpdateUI();
            }
        }
    }

    private void CalculateXP()
    {
        // 0-50 metre: her metre 0.2 xp (önceden 1)
        // 50-100 metre: her metre 0.4 xp (önceden 2)
        // 100-150 metre: her metre 0.6 xp (önceden 3)
        
        float tempXP = 0;

        if (distance <= 50f)
        {
            tempXP = distance * 0.2f;
        }
        else if (distance <= 100f)
        {
            tempXP = (50f * 0.2f) + (distance - 50f) * 0.4f;
        }
        else if (distance <= 150f)
        {
            tempXP = (50f * 0.2f) + (50f * 0.4f) + (distance - 100f) * 0.6f;
        }
        else
        {
            // 150+ metre (varsayılan artış oranı olarak 1.0 xp/m kullanalım)
            tempXP = (50f * 0.2f) + (50f * 0.4f) + (50f * 0.6f) + (distance - 150f) * 1.0f;
        }

        totalXP = Mathf.FloorToInt(tempXP) + bonusXP;
    }

    public void AddXP(int amount)
    {
        bonusXP += amount;
        UpdateUI();
        SpawnXPIcon(amount);

        // Play sound effect
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayExtraXP();
        }
    }

    private void SpawnXPIcon(int amount)
    {
        // Auto-find player if not assigned
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }

        if (playerTransform == null)
        {
            Debug.LogWarning("ScoreManager: Player Transform is null and couldn't be found by tag 'Player'!");
            return;
        }

        GameObject prefab = null;
        if (amount == 5) prefab = xp5Prefab;
        else if (amount == 20) prefab = xp20Prefab;
        else if (amount == 50) prefab = xp50Prefab;

        if (prefab != null)
        {
            // Spawn slightly above the player
            Vector3 spawnPos = playerTransform.position + new Vector3(0, 1.5f, 0);
            Instantiate(prefab, spawnPos, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning("ScoreManager: No prefab assigned for XP amount: " + amount);
        }
    }

    public void RegisterCleanJump()
    {
        cleanJumpsCount++;
        if (cleanJumpsCount >= COMBO_THRESHOLD)
        {
            AddXP(COMBO_XP_BONUS);
            Debug.Log("Combo! +5 XP added. Total clean jumps: " + cleanJumpsCount);
        }
    }

    public void ResetCleanJumps()
    {
        cleanJumpsCount = 0;
        Debug.Log("Combo reset (hit obstacle).");
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
