using UnityEngine;

public class CharacterSelectionUI : MonoBehaviour
{
    public MainMenuManager mainMenuManager; // MainMenuManager referansı
    public UnityEngine.UI.Button ladyMitoButton;
    public UnityEngine.UI.Button sirNoriButton;

    private void Awake()
    {
        // Butonları otomatik bul (isimle) eğer sürüklenmemişse
        if (ladyMitoButton == null)
        {
            GameObject b = GameObject.Find("LadyMitoButton");
            if (b != null) ladyMitoButton = b.GetComponent<UnityEngine.UI.Button>();
        }

        if (sirNoriButton == null)
        {
            GameObject b = GameObject.Find("SirNoriButton");
            if (b != null) sirNoriButton = b.GetComponent<UnityEngine.UI.Button>();
        }

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

    public void ClosePanel()
    {
        gameObject.SetActive(false);
    }
}
