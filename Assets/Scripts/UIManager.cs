using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("Popup")]
    public GameObject settingsPopup;
    public GameObject pialaPopup;

    [Header("Sound Toggle")]
    [Tooltip("Optional. Isi dengan Button sound di popup settings kalau ingin fallback warna dari image tombol.")]
    public Button soundToggleButton;
    [Tooltip("Text ON/OFF pada tombol sound.")]
    public TMP_Text soundToggleText;
    [Tooltip("Image/background tombol sound yang berubah warna.")]
    public Image soundToggleImage;
    public string soundOnText = "ON";
    public string soundOffText = "OFF";
    public Color soundOnColor = new Color(0.18f, 0.72f, 0.32f);
    public Color soundOffColor = new Color(0.86f, 0.18f, 0.18f);

    private void Start()
    {
        UpdateSoundToggleVisual();
    }

    public void OpenSettings()
    {
        settingsPopup.SetActive(true);
        UpdateSoundToggleVisual();
    }

    public void CloseSettings()
    {
        settingsPopup.SetActive(false);
    }

    public void Logout()
    {
        PlayerPrefs.DeleteKey("Nama");
        PlayerPrefs.DeleteKey("Kelas");
        PlayerPrefs.Save();
        SceneManager.LoadScene("LoginScene");
    }

    public void ExitGame()
    {
        Application.Quit();
        Debug.Log("Keluar aplikasi");
    }

    public void ToggleSound()
    {
        if (ButtonSfxManager.Instance == null) return;

        ButtonSfxManager.Instance.ToggleSound();
        BackgroundMusicManager.Instance?.RefreshPlayback();
        UpdateSoundToggleVisual();
    }

    public void UpdateSoundToggleVisual()
    {
        bool isSoundOn = ButtonSfxManager.Instance != null
            ? ButtonSfxManager.Instance.IsSoundOn()
            : PlayerPrefs.GetInt("SoundOn", 1) == 1;

        if (soundToggleText != null)
            soundToggleText.text = isSoundOn ? soundOnText : soundOffText;

        Image targetImage = soundToggleImage;
        if (targetImage == null && soundToggleButton != null)
            targetImage = soundToggleButton.targetGraphic as Image;

        if (targetImage != null)
            targetImage.color = isSoundOn ? soundOnColor : soundOffColor;
    }

    public void GoToLearning()
    {
        SceneManager.LoadScene("LearningScene");
    }
    public void OpenPiala()
    {
        pialaPopup.SetActive(true);
        pialaPopup.SendMessage("RefreshReport", SendMessageOptions.DontRequireReceiver);
    }

    public void ClosePiala()
    {
        pialaPopup.SetActive(false);
    }

}
