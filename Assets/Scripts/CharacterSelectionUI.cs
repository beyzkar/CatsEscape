using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectionUI : MonoBehaviour
{
    public MainMenuManager mainMenuManager; // MainMenuManager referansı
    public Button ladyMitoButton;
    public Button sirNoriButton;

    private void Awake()
    {
        // Programatik olarak listener ekle (Unity Scene'deki kaybolan OnClick referanslarını garanti altına almak için)
        if (ladyMitoButton != null)
        {
            ladyMitoButton.onClick.RemoveAllListeners();
            ladyMitoButton.onClick.AddListener(() => SelectCharacter(0));
        }

        if (sirNoriButton != null)
        {
            sirNoriButton.onClick.RemoveAllListeners();
            sirNoriButton.onClick.AddListener(() => SelectCharacter(1));
        }
    }

    public void SelectCharacter(int charIndex)
    {
        // Seçimi PlayerPrefs'e kaydet
        PlayerPrefs.SetInt("SelectedCharacter", charIndex);
        PlayerPrefs.Save();

        Debug.Log("Character Selected: " + charIndex);

        if (mainMenuManager != null)
        {
            mainMenuManager.PerformStartGame();
        }
    }
}
