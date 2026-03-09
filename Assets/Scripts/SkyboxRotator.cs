using UnityEngine;

/// <summary>
/// Rotates the global skybox to simulate movement in a running game.
/// </summary>
public class SkyboxRotator : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float rotationSpeed = 50.0f; // High speed to simulate running distance
    
    private float currentRotation = 0f;

    void Start()
    {
        if (RenderSettings.skybox == null)
        {
            Debug.LogWarning("SkyboxRotator: Skybox bulunamadı! Lütfen Lighting ayarlarından bir Skybox material atayın.");
        }
        else
        {
            Debug.Log("SkyboxRotator: Çalışıyor. Hedef materyal: " + RenderSettings.skybox.name);
        }
    }

    void Update()
    {
        if (GameSpeed.Multiplier <= 0) return;

        currentRotation += rotationSpeed * GameSpeed.Multiplier * Time.deltaTime;
        if (currentRotation > 360f) currentRotation -= 360f;

        if (RenderSettings.skybox != null)
        {
            // Farland Skies specific: Clouds Rotation
            if (RenderSettings.skybox.HasProperty("_CloudsRotation"))
                RenderSettings.skybox.SetFloat("_CloudsRotation", currentRotation);

            // Try other common rotation property names
            if (RenderSettings.skybox.HasProperty("_Rotation"))
                RenderSettings.skybox.SetFloat("_Rotation", currentRotation);
            else if (RenderSettings.skybox.HasProperty("_RotationSpeed"))
                RenderSettings.skybox.SetFloat("_RotationSpeed", currentRotation);
            else if (RenderSettings.skybox.HasProperty("_SkyRotation"))
                RenderSettings.skybox.SetFloat("_SkyRotation", currentRotation);
        }
    }
}
