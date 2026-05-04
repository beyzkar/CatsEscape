using UnityEngine;
using CatsEscape.Auth;

public static class ProgressManager
{
    public static int CurrentLevel
    {
        get => AuthManager.Instance != null ? AuthManager.Instance.LastLevelReached : 1;
        set { if (AuthManager.Instance != null) AuthManager.Instance.LastLevelReached = value; }
    }

    public static int XP
    {
        get => AuthManager.Instance != null ? AuthManager.Instance.LastSavedXP : 0;
        set { if (AuthManager.Instance != null) AuthManager.Instance.LastSavedXP = value; }
    }

    public static void ResetRunProgressOnly()
    {
        if (AuthManager.Instance != null)
        {
            AuthManager.Instance.ResetRunProgressOnly();
        }
    }

    public static void Save()
    {
        PlayerPrefs.Save();
        Debug.Log("[PROGRESS] Saved progress after reset");
    }

    // Keep SaveProgress for backward compatibility if needed, but the user asked for Save()
    public static void SaveProgress() => Save();

    public static (int currentLevel, int xp) LoadProgress()
    {
        if (AuthManager.Instance != null)
        {
            return (AuthManager.Instance.LastLevelReached, AuthManager.Instance.LastSavedXP);
        }
        return (1, 0);
    }
}
