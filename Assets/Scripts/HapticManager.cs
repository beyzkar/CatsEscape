using UnityEngine;

/// <summary>
/// Mobil cihazlarda kısa ve hafif dokunsal (haptic) geri bildirim sağlamak için static yardımcı sınıf.
/// Handheld.Vibrate() çok uzun olduğu için Android native VibrationEffect API'sini kullanır.
/// </summary>
public static class HapticManager
{
#if UNITY_ANDROID && !UNITY_EDITOR
    private static AndroidJavaObject vibrator;
    private static AndroidJavaClass vibrationEffectClass;
    private static bool isInitialized = false;

    private static void Initialize()
    {
        if (isInitialized) return;

        try
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");

            // API Level 26+ (Android Oreo ve üstü) VibrationEffect destekler
            using (AndroidJavaClass buildVersionClass = new AndroidJavaClass("android.os.Build$VERSION"))
            {
                int sdkInt = buildVersionClass.GetStatic<int>("SDK_INT");
                if (sdkInt >= 26)
                {
                    vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect");
                }
            }
            isInitialized = true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[HapticManager] Failed to initialize Android vibrator: " + e.Message);
        }
    }
#endif

    /// <summary>
    /// Çok kısa (10-20ms) ve hafif bir titreşim tetikler.
    /// Sadece dokunma hissi (button press feedback) vermek içindir.
    /// </summary>
    public static void LightTap()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        Initialize();
        if (vibrator == null) return;

        try
        {
            long duration = 20L; // Sadece 15 milisaniye (Çok kısa)
            int amplitude = 70;  // Güç: 35 (1-255 arası)

            if (vibrationEffectClass != null)
            {
                // Yeni nesil Android cihazlar için (Özel genlik/güç ayarlı)
                AndroidJavaObject effect = vibrationEffectClass.CallStatic<AndroidJavaObject>("createOneShot", duration, amplitude);
                vibrator.Call("vibrate", effect);
            }
            else
            {
                // Eski nesil Android cihazlar için (Sadece süre ayarlı)
                vibrator.Call("vibrate", duration);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[HapticManager] Failed to trigger LightTap: " + e.Message);
        }
#elif UNITY_IOS && !UNITY_EDITOR
        // iOS'te Handheld.Vibrate() çok uzun sürdüğü için isteğe bağlı boş bırakıldı.
        // İleride iOS için özel Taptic Engine eklenebilir.
#else
        // Editor logu
        // Debug.Log("[HapticManager] LightTap triggered.");
#endif
    }

    /// <summary>
    /// Unity'nin derleme (build) sırasında AndroidManifest.xml dosyasına 
    /// 'android.permission.VIBRATE' iznini otomatik eklemesi için bir hile.
    /// Bu metod asla çağrılmaz.
    /// </summary>
    private static void ForceVibrationPermissionInManifest()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        Handheld.Vibrate();
#endif
    }
}
