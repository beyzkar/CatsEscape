using UnityEngine;

public class CharacterSelector : MonoBehaviour
{
    [Header("Character Models")]
    public GameObject[] characterModels; // Player objesinin altındaki kedi modellerini buraya ekleyin

    private void Awake()
    {
        // PlayerPrefs'ten seçilen karakteri oku (0 veya 1)
        int selectedIndex = PlayerPrefs.GetInt("SelectedCharacter", 0);

        // Güvenlik: Index sınırlar içinde mi?
        if (selectedIndex < 0 || selectedIndex >= characterModels.Length)
            selectedIndex = 0;

        // Belirtilen karakteri aktif yap, diğerlerini kapat
        for (int i = 0; i < characterModels.Length; i++)
        {
            if (characterModels[i] != null)
            {
                characterModels[i].SetActive(i == selectedIndex);
            }
        }

        Debug.Log("In-Game: Character " + selectedIndex + " activated.");
    }
}
